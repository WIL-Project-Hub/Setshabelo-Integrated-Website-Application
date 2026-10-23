namespace SetshabeloAdmissionSystem.Models
{
    public class Achievement
    {
        public Guid AchievementId { get; set; }
        public Guid AdminId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool IsPublic { get; set; } = true;
    }
}
