using SetshabeloAdmissionSystem.Models;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ApplicationDetailViewModel
    {
        public Application Application { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public string NewComment { get; set; } = string.Empty;
    }
}
