using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chronus.Models.MainModels
{
    public class MeetingAttendance
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("CalendarEvent")]
        public Guid EventId { get; set; }
        public CalendarEvent? CalendarEvent { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required]
        public DateTime CheckedInAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "OnTime"; // "OnTime" sau "Late"
    }
}