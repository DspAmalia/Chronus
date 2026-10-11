using Chronus.Models.MainModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Chronus.Data
{
    public class ChronusDbContext : IdentityDbContext<Employee, Role, Guid>
    {
        public ChronusDbContext(DbContextOptions<ChronusDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<RawAuditEvent> RawAuditEvents => Set<RawAuditEvent>();
        public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurări pentru Employee (o singură dată, corect integrat cu Identity)
            modelBuilder.Entity<Employee>(b =>
            {
                b.HasIndex(e => e.Email).IsUnique();

                // Relație cu Department (opțională, SetNull la ștergere)
                b.HasOne(e => e.Department)
                   .WithMany(d => d.Employees)
                   .HasForeignKey(e => e.DepartmentId)
                   .OnDelete(DeleteBehavior.SetNull);

                // Relație cu Role (opțională, SetNull la ștergere)
                b.HasOne(e => e.Role)
                   .WithMany(r => r.Employees)
                   .HasForeignKey(e => e.RoleId)
                   .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RawAuditEvent>()
                .HasOne(r => r.Device)
                .WithMany(d => d.RawEvents)
                .HasForeignKey(r => r.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimeEntry>(b =>
            {
                b.HasKey(t => t.Id);
                b.HasIndex(t => new { t.EmployeeId, t.Date }).IsUnique();
                b.Property(t => t.ConfidenceScore).HasPrecision(3, 2);
            });
        }
    }
}