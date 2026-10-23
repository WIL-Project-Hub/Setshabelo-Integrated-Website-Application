using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ParentApplicationStatusViewModel
    {
        public Application Application { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }
}
