namespace SetshabeloAdmissionSystem.Models
{
    public class Comment
    {
        public Guid CommentId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid AdminId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
