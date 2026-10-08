using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Chronus.Models.MainModels
{
    public enum EntryStatus
    {
        InProgress = 0,
        AutoCalculated = 1,
        NeedsReview = 2,
        Approved = 3
    }

    public class TimeEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Employee")]
        public Guid EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public DateTime ClockInUtc { get; set; }

        public DateTime? ClockOutUtc { get; set; }

        public int TotalBreakMinutes { get; set; } = 0;

        public int NetWorkMinutes { get; set; } = 0;

        [Column(TypeName = "decimal(3, 2)")]
        public decimal ConfidenceScore { get; set; } = 1.00m;

        public EntryStatus Status { get; set; } = EntryStatus.InProgress;
    }
}
