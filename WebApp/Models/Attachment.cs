using System;

namespace WebApp.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? ContactId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public virtual Student Student { get; set; } = null!;
    }
}
