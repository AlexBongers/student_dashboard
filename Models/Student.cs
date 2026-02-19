using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StageManagementSystem.Models
{
    public class Student
    {
        public int Id { get; set; }
        
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        // Computed property for backward compatibility and display
        public string Name => $"{LastName}, {FirstName}"; 

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? StudentNumber { get; set; }
        
        [Required]
        public string Type { get; set; } = "stage"; // "stage" or "scriptie"

        public string MyRole { get; set; } = "docentbegeleider"; // "docentbegeleider" or "1e examinator"
        
        [Required]
        public string Company { get; set; } = string.Empty;
        
        public string? Location { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string Status { get; set; } = "opstart";
        
        public string? Notes { get; set; }
        
        public bool Archived { get; set; }
        
        public DateTime? ArchivedAt { get; set; }
        
        public bool HasUrgentDeadline => Deadlines?.Any(d => !d.IsCompleted && (d.DueDate.Date - DateTime.Today).TotalDays <= 7) ?? false;
        
        public string? ProfilePicturePath { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
        public virtual ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
