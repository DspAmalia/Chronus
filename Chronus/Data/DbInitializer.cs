using Chronus.Models.MainModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronus.Data
{
    public class DbInitializer
    {
        public static async Task SeedAsync(IDbContextFactory<ChronusDbContext> dbFactory, IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Employee>>();

            using var db = await dbFactory.CreateDbContextAsync();

            // Ne asigurăm că baza de date este creată
            await db.Database.EnsureCreatedAsync();

            // Dacă avem deja angajați, nu mai adăugăm date de test
            if (await db.Employees.AnyAsync())
            {
                return;
            }

            // 1. Creăm un angajat de test folosind UserManager (pentru a avea parolă validă)
            var employee = new Employee
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "Popescu",
                LastName = "Ion",
                Email = "ion.popescu@companie.ro",
                UserName = "ion.popescu@companie.ro",
                EmailConfirmed = true,
                DailyTargetMinutes = 480, // 8 ore
                IsActive = true
            };

            // Creăm utilizatorul cu parola "Parola123!" (se generează automat hash-ul de securitate)
            var createResult = await userManager.CreateAsync(employee, "Parola123!");
            if (!createResult.Succeeded)
            {
                return;
            }

            // 2. Creăm un PC asignat acestui angajat
            var device = new UserDevice
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                EmployeeId = employee.Id,
                MachineName = "DESKTOP-ION-WORK",
                LastSeenUtc = DateTime.UtcNow
            };

            // 3. Adăugăm o fișă de pontaj pe ziua de azi pentru acest angajat
            var today = DateOnly.FromDateTime(DateTime.Today);
            var timeEntry = new TimeEntry
            {
                EmployeeId = employee.Id,
                Date = today,
                ClockInUtc = DateTime.UtcNow.AddHours(-4), // A început acum 4 ore
                NetWorkMinutes = 240,
                TotalBreakMinutes = 0,
                Status = EntryStatus.InProgress,
                ConfidenceScore = 1.00m
            };

            db.UserDevices.Add(device);
            db.TimeEntries.Add(timeEntry);

            await db.SaveChangesAsync();
        }
    }
}