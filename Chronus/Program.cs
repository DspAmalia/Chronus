using Chronus.Components;
using Chronus.Data;
using Chronus.Models.MainModels;
using Microsoft.EntityFrameworkCore;
using Chronus.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ChronusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Employee, Role>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ChronusDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        options.Events.OnCreatingTicket = async context =>
        {
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var givenName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "Google";
            var surname = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value ?? "User";
            var providerKey = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey)) return;

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Employee>>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new Employee
                {
                    UserName = email,
                    Email = email,
                    FirstName = givenName,
                    LastName = surname,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    DailyTargetMinutes = 480
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded) return;
            }

            // Folosim direct "Google" în loc de context.Provider
            var info = new UserLoginInfo("Google", providerKey, "Google");
            var existingLogin = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingLogin == null)
            {
                await userManager.AddLoginAsync(user, info);
            }
        };
    });

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddCascadingAuthenticationState();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adăugăm agentul de Windows și procesatorul în fundal
builder.Services.AddHostedService<Chronus.Services.ClockingProcessorService>();
builder.Services.AddHostedService<Chronus.Services.WindowsSessionAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseMigrationsEndPoint();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chronus API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// 1. Pune middleware-urile de securitate AICI (înainte de rute și endpoint-uri)
app.UseAuthentication();
app.UseAuthorization();

// 2. Endpoint pentru primirea evenimentelor de la Agentul de PC
app.MapPost("/api/events", async (RawAuditEventDto dto, IDbContextFactory<ChronusDbContext> dbFactory) =>
{
    using var db = await dbFactory.CreateDbContextAsync();

    var auditEvent = new RawAuditEvent
    {
        EmployeeId = dto.EmployeeId,
        DeviceId = dto.DeviceId,
        Type = dto.Type,
        TimestampUtc = DateTime.UtcNow
    };

    db.RawAuditEvents.Add(auditEvent);
    await db.SaveChangesAsync();

    return Results.Ok(new { status = "Event Recorded" });
}).DisableAntiforgery();

// 3. Endpoint-ul de Login clasic prin formular (acesta lipsea și dădea 404!)
app.MapPost("/api/auth/login", async (HttpContext httpContext, SignInManager<Employee> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        return Results.Redirect("/", preserveMethod: false);
    }

    return Results.Redirect("/employees/login?error=Invalid+credentials", preserveMethod: false);
})
.DisableAntiforgery();

// 4. Endpoint pentru Google Login
app.MapGet("/api/auth/external-login", (string provider, HttpContext httpContext) =>
{
    var redirectUrl = "/";
    var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = redirectUrl };
    return Results.Challenge(properties, new[] { provider });
});

// Solicităm Factory-ul pentru DbContext și populăm datele la startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ChronusDbContext>>();
    await DbInitializer.SeedAsync(dbFactory);
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// DTO folosit pentru Endpoint
public record RawAuditEventDto(Guid EmployeeId, Guid DeviceId, EventType Type);