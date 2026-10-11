using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Chronus.Models.MainModels
{
    public class Employee : IdentityUser<Guid>
    {
        // Notă: Id-ul și Email-ul sunt moștenite automat din IdentityUser<Guid>

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public int DailyTargetMinutes { get; set; } = 480; // 8 ore de muncă

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
        public ICollection<RawAuditEvent> RawEvents { get; set; } = new List<RawAuditEvent>();
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

        [ForeignKey("Department")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [ForeignKey("Role")]
        public Guid? RoleId { get; set; }
        public Role? Role { get; set; }
    }
}