namespace SetshabeloAdmissionSystem.Models
{
    public class Document
    {
        public Guid DocumentId { get; set; }
        public Guid ApplicationId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
