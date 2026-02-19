using System;
using System.ComponentModel.DataAnnotations;

namespace StageManagementSystem.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; }
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
        public string Content { get; set; } = string.Empty;

        public virtual Student Student { get; set; } = null!;
    }
}
