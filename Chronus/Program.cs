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
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// ⬇️ Adaugă acest bloc pentru a prelungi durata cookie-ului și a-l face persistent
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/employees/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//builder.Services.AddAuthentication()
//    .AddGoogle(options =>
//    {
//        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
//        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
//        options.SaveTokens = true;

//        options.Events.OnCreatingTicket = async context =>
//        {
//            // Codul tău existent pentru crearea/găsirea angajatului...
//            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
//            var givenName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "Google";
//            var surname = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value ?? "User";
//            var providerKey = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

//            if (string.IsNullOrEmpty(email)) return;

//            var dbFactory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<ChronusDbContext>>();
//            using var db = await dbFactory.CreateDbContextAsync();

//            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Employee>>();

//            var user = await userManager.FindByEmailAsync(email);
//            if (user == null)
//            {
//                user = new Employee
//                {
//                    UserName = email,
//                    Email = email,
//                    FirstName = givenName,
//                    LastName = surname,
//                    EmailConfirmed = true,
//                    IsActive = true,
//                    CreatedAtUtc = DateTime.UtcNow,
//                    DailyTargetMinutes = 480
//                };

//                var createResult = await userManager.CreateAsync(user);
//                if (!createResult.Succeeded) return;
//            }

//            if (!string.IsNullOrEmpty(providerKey))
//            {
//                var info = new UserLoginInfo("Google", providerKey, "Google");
//                var existingLogin = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
//                if (existingLogin == null)
//                {
//                    await userManager.AddLoginAsync(user, info);
//                }
//            }
//        };
//    });

builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔄 Reintroducem serviciile de fundal hardware (ClockingProcessorService și WindowsSessionAgent)
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

app.UseAuthentication();
app.UseAuthorization();

// 🔄 Reintroducem Endpoint-ul /api/events pentru preluarea evenimentelor de la Agentul de PC
app.MapPost("/api/events", async (RawAuditEventDto dto, IDbContextFactory<ChronusDbContext> dbFactory) =>
{
    using var db = await dbFactory.CreateDbContextAsync();

    // Căutăm dispozitivul și angajatul după numele mașinii
    var device = await db.UserDevices
        .Include(d => d.Employee)
        .FirstOrDefaultAsync(d => d.MachineName == dto.MachineName);

    if (device == null || device.Employee == null)
    {
        return Results.BadRequest(new { error = "Device or Employee not linked in database." });
    }

    var auditEvent = new RawAuditEvent
    {
        EmployeeId = device.EmployeeId,
        DeviceId = device.Id,
        Type = dto.Type,
        TimestampUtc = DateTime.UtcNow
    };

    db.RawAuditEvents.Add(auditEvent);
    await db.SaveChangesAsync();

    return Results.Ok(new { status = "Event Recorded for " + device.Employee.Email });
}).DisableAntiforgery();

// Endpoint-ul de Login clasic prin formular
app.MapPost("/api/auth/login", async (HttpContext httpContext, SignInManager<Employee> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        return Results.Redirect("/clocking", preserveMethod: false);
    }

    return Results.Redirect("/employees/login?error=Invalid+credentials", preserveMethod: false);
})
.DisableAntiforgery();

// Endpoint-ul de Logout
app.MapPost("/api/auth/logout", async (SignInManager<Employee> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/employees/login", preserveMethod: false);
})
.DisableAntiforgery();

// Endpoint pentru Google Login
//app.MapGet("/api/auth/external-login", (string provider, HttpContext httpContext) =>
//{
//    var redirectUrl = "/clocking";
//    var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
//    {
//        RedirectUri = redirectUrl,
//        IsPersistent = true 
//    };
//    return Results.Challenge(properties, new[] { provider });
//});

// Populăm datele la startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ChronusDbContext>>();
    await DbInitializer.SeedAsync(dbFactory, scope.ServiceProvider);
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// DTO pentru a accepta contextul de Windows
public record RawAuditEventDto(string WindowsUserName, string MachineName, EventType Type);