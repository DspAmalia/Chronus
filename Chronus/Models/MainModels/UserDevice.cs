using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Chronus.Models.MainModels
{
    public class UserDevice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public ICollection<RawAuditEvent> RawEvents { get; set; } = new List<RawAuditEvent>();
    }
}
