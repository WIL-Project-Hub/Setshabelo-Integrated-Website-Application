using System.ComponentModel.DataAnnotations;

namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ParentProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
    }
}
