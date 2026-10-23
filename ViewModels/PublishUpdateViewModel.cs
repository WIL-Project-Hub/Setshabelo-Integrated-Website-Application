using System.ComponentModel.DataAnnotations;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class PublishUpdateViewModel
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
