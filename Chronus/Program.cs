using Chronus.Components;
using Chronus.Data;
using Chronus.Models.MainModels;
using Microsoft.EntityFrameworkCore;
using Chronus.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ChronusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


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

// ⚠️ AICI ERA GREȘEALA: Am scos Swagger-ul în afara blocului "!app.Environment.IsDevelopment()"
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chronus API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Endpoint scurt Minimal API pentru primirea evenimentelor de la Agentul de PC
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