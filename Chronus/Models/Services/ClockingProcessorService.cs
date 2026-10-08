using Chronus.Data;
using Chronus.Models.MainModels;
using Microsoft.EntityFrameworkCore;

namespace Chronus.Services;

public class ClockingProcessorService : BackgroundService
{
    private readonly IDbContextFactory<ChronusDbContext> _dbFactory;
    private readonly ILogger<ClockingProcessorService> _logger;

    public ClockingProcessorService(
        IDbContextFactory<ChronusDbContext> dbFactory,
        ILogger<ClockingProcessorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Procesatorul automat de pontaj Chronus a pornit...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnprocessedEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la procesarea pontajului.");
            }

            // Rulează o dată la 10 secunde
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessUnprocessedEventsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        // Extragere evenimente neprocesate, ordonate după timp
        var unprocessedEvents = await db.RawAuditEvents
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.TimestampUtc)
            .ToListAsync();

        if (!unprocessedEvents.Any()) return;

        foreach (var rawEvent in unprocessedEvents)
        {
            var eventDate = DateOnly.FromDateTime(rawEvent.TimestampUtc.ToLocalTime());

            // Căutăm sau creăm fisa de pontaj pentru ziua respectivei înregistrări
            var timeEntry = await db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == rawEvent.EmployeeId && t.Date == eventDate);

            if (timeEntry == null)
            {
                timeEntry = new TimeEntry
                {
                    EmployeeId = rawEvent.EmployeeId,
                    Date = eventDate,
                    ClockInUtc = rawEvent.TimestampUtc,
                    Status = EntryStatus.InProgress,
                    ConfidenceScore = 1.00m
                };
                db.TimeEntries.Add(timeEntry);
            }

            // Dacă e SessionUnlocked (1) -> Actualizează ClockIn sau revenire
            if (rawEvent.Type == EventType.SessionUnlocked)
            {
                if (timeEntry.ClockOutUtc.HasValue)
                {
                    // Calculează pauza dintre ultimul Lock și acest Unlock
                    var breakDuration = (int)(rawEvent.TimestampUtc - timeEntry.ClockOutUtc.Value).TotalMinutes;
                    if (breakDuration > 0)
                    {
                        timeEntry.TotalBreakMinutes += breakDuration;
                    }
                    timeEntry.ClockOutUtc = null; // Re-devine activ
                }
            }
            // Dacă e SessionLocked (2) -> Marchează ca temporar oprit (ClockOut provizoriu)
            else if (rawEvent.Type == EventType.SessionLocked)
            {
                timeEntry.ClockOutUtc = rawEvent.TimestampUtc;

                // Calculează timpul net lucrat în minute
                var totalMinutesWorked = (int)(rawEvent.TimestampUtc - timeEntry.ClockInUtc).TotalMinutes;
                timeEntry.NetWorkMinutes = Math.Max(0, totalMinutesWorked - timeEntry.TotalBreakMinutes);
            }

            // Marcăm evenimentul ca fiind procesat
            rawEvent.IsProcessed = true;
        }

        await db.SaveChangesAsync();
    }
}