using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Chronus.Models.MainModels
{
    public enum EventType
    {
        SessionUnlocked = 1, // Start zi / Revenire din pauză
        SessionLocked = 2,   // Pauză / Plecare de la PC
        SystemShutdown = 3   // Închidere PC / Final zi
    }

    public class RawAuditEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        [ForeignKey("Device")]
        public Guid DeviceId { get; set; }

        public UserDevice? Device { get; set; }

        [Required]
        public EventType Type { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public bool IsProcessed { get; set; } = false;
    }
}
