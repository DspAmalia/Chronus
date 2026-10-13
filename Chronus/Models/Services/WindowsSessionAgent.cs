using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Chronus.Services;

public class WindowsSessionAgent : BackgroundService
{
    private readonly ILogger<WindowsSessionAgent> _logger;
    private readonly HttpClient _httpClient;

    // Preluăm dinamic datele calculatorului curent
    private static readonly string MachineName = Environment.MachineName;
    private static readonly string WindowsUserName = Environment.UserName;

    private const string ApiUrl = "https://localhost:7166/api/events";

    public WindowsSessionAgent(ILogger<WindowsSessionAgent> logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🖥️ Agentul Windows pentru {Machine} a pornit...", MachineName);

        SystemEvents.SessionSwitch += OnSessionSwitch;

        stoppingToken.Register(() =>
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _logger.LogInformation("🖥️ Agentul Windows s-a oprit.");
        });

        return Task.CompletedTask;
    }

    private async void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
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
            _logger.LogInformation("🔔 Eveniment Windows detectat: {Reason}", e.Reason);
            await SendEventToApiAsync(eventType.Value);
        }
    }

    private async Task SendEventToApiAsync(int eventType)
    {
        try
        {
            var payload = new
            {
                WindowsUserName = WindowsUserName,
                MachineName = MachineName,
                Type = eventType
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Eveniment trimis dinamic cu succes pentru mașina: {Machine}", MachineName);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ Serverul a respins evenimentul ({Code}): {Error}", response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Nu s-a putut trimite evenimentul către API.");
        }
    }
}