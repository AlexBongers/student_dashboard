using System;

namespace StageManagementSystem.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? ContactId { get; set; } // Optional link to a specific contact moment
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;

        public virtual Student Student { get; set; } = null!;
    }
}
