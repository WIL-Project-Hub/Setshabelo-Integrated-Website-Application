using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ParentHomeViewModel
    {
        public string ParentFirstName { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<SchoolUpdate> RecentUpdates { get; set; } = new();
        public List<Achievement> RecentAchievements { get; set; } = new();
    }
}
