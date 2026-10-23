using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ManageContentViewModel
    {
        public List<SchoolUpdate> Updates { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = new();
    }
}
