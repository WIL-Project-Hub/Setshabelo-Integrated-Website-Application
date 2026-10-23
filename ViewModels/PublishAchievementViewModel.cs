using System.ComponentModel.DataAnnotations;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class PublishAchievementViewModel
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
