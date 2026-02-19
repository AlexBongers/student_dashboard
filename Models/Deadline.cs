using System;

namespace StageManagementSystem.Models
{
    public class Deadline
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }

        public virtual Student Student { get; set; } = null!;
    }
}
