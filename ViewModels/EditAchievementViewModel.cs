using System.ComponentModel.DataAnnotations;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class EditAchievementViewModel
    {
        public Guid AchievementId { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Keeps track of the existing image if no new one is uploaded
        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
