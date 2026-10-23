namespace SetshabeloAdmissionSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
