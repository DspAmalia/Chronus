using Chronus.Models.MainModels;
using Microsoft.EntityFrameworkCore;

namespace Chronus.Data
{
    public class ChronusDbContext : DbContext
    {
        public ChronusDbContext(DbContextOptions<ChronusDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<RawAuditEvent> RawAuditEvents => Set<RawAuditEvent>();
        public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<RawAuditEvent>()
                .HasOne(r => r.Device)
                .WithMany(d => d.RawEvents)
                .HasForeignKey(r => r.DeviceId)
                .OnDelete(DeleteBehavior.Restrict); // Interzice ștergerea dublă în cascadă

            modelBuilder.Entity<TimeEntry>(b =>
            {
                b.HasKey(t => t.Id);
                b.HasIndex(t => new { t.EmployeeId, t.Date }).IsUnique();
                b.Property(t => t.ConfidenceScore).HasPrecision(3, 2);
            });
        }
    }
}
