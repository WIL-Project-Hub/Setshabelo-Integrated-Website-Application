namespace SetshabeloAdmissionSystem.Models
{
    public class SchoolUpdate
    {
        public Guid UpdateId { get; set; }
        public Guid AdminId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
