using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Chronus.Services;

public class WindowsSessionAgent : BackgroundService
{
    private readonly ILogger<WindowsSessionAgent> _logger;
    private readonly HttpClient _httpClient;

    // Folosim ID-urile de test create în DbInitializer
    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DeviceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Endpoint-ul local din aplicație
    private const string ApiUrl = "https://localhost:7166/api/events"; // ⚠️ Asigură-te că portul se potrivește cu cel din proiectul tău!

    public WindowsSessionAgent(ILogger<WindowsSessionAgent> logger)
    {
        _logger = logger;

        // Ignorăm verificările de certificat SSL pentru localhost în dezvoltare
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🖥️ Agentul Windows Chronus a pornit și ascultă deblocarea/blocarea ecranului...");

        // Ne abonăm la evenimentele de sesiune Windows
        SystemEvents.SessionSwitch += OnSessionSwitch;

        stoppingToken.Register(() =>
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _logger.LogInformation("🖥️ Agentul Windows Chronus s-a oprit.");
        });

        return Task.CompletedTask;
    }

    private async void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        // Determinăm tipul de eveniment
        // Type 1 = SessionUnlocked (La lucru)
        // Type 2 = SessionLocked (În pauză)
        int? eventType = e.Reason switch
        {
            SessionSwitchReason.SessionUnlock => 1,
            SessionSwitchReason.SessionLock => 2,
            SessionSwitchReason.ConsoleConnect => 1,
            SessionSwitchReason.ConsoleDisconnect => 2,
            _ => null
        };

        if (eventType.HasValue)
        {
            _logger.LogInformation("🔔 Eveniment detectat în Windows: {Reason} (EventType: {Type})", e.Reason, eventType);
            await SendEventToApiAsync(eventType.Value);
        }
    }

    private async Task SendEventToApiAsync(int eventType)
    {
        try
        {
            var payload = new
            {
                EmployeeId = EmployeeId,
                DeviceId = DeviceId,
                Type = eventType
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Eveniment trimis cu succes la API-ul Chronus.");
            }
            else
            {
                _logger.LogWarning("⚠️ Serverul a răspuns cu codul: {Code}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Nu s-a putut trimite evenimentul către API.");
        }
    }
}