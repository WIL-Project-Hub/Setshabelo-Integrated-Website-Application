namespace SetshabeloAdmissionSystem.Models
{
    public class Parent
    {
        public Guid ParentId { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
