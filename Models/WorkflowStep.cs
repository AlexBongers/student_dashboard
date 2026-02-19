using System;
using System.ComponentModel.DataAnnotations;

namespace StageManagementSystem.Models
{
    public class WorkflowStep
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        
        [Required]
        public string StepKey { get; set; } = string.Empty;
        
        public bool Completed { get; set; }
        public DateTime? CompletedDate { get; set; }

        public virtual Student Student { get; set; } = null!;
    }
}
