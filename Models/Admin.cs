namespace SetshabeloAdmissionSystem.Models
{
    public class Admin
    {
        public Guid AdminId { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
