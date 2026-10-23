using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ApplicationListViewModel
    {
        public List<Application> Applications { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
    }
}
