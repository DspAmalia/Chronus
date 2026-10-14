using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chronus.Models.MainModels
{
    public enum AgendaPriority
    {
        [Display(Name = "Important")]
        High = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "Not so important")]
        Low = 3
    }

    public class CalendarEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EmployeeId { get; set; } 

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public AgendaPriority Priority { get; set; } = AgendaPriority.Normal;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public string? Participants { get; set; } // Stochează email-urile sau ID-urile separate prin virgulă

        [Required]
        public Guid CreatedById { get; set; }
    }
}