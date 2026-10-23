using System;

namespace SetshabeloAdmissionSystem.Models
{
    public class Application
    {
        public Guid ApplicationId { get; set; }
        public Guid ParentId { get; set; }

        // Parent info
        public string? ParentTitle { get; set; }
        public string? ParentInitials { get; set; }
        public string ParentFirstName { get; set; } = string.Empty;
        public string ParentLastName { get; set; } = string.Empty;
        public string? ParentIdNumber { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string? ParentAddress { get; set; }
        public string? ParentRelationship { get; set; }
        public string? ParentRace { get; set; }
        public string? ParentHomeLanguage { get; set; }
        public string? ParentOccupation { get; set; }
        public string? ParentEmployer { get; set; }

        // Child info
        public string ChildFullName { get; set; } = string.Empty;
        public DateTime? ChildDateOfBirth { get; set; }
        public string? ChildGender { get; set; }
        public string? ChildRace { get; set; }
        public string? ChildIdNumber { get; set; }
        public string? ChildCitizenship { get; set; }
        public string? CountryOfResidence { get; set; }
        public string? Province { get; set; }
        public string? ChildHomeLanguage { get; set; }
        public string? PreferredLanguage { get; set; }
        public string GradeApplyingFor { get; set; } = string.Empty;
        public string? PrePrimaryEducation { get; set; }

        // Education history
        public string? HighestGradePassed { get; set; }
        public string? YearGradePassed { get; set; }
        public string? AccessionNo { get; set; }
        public string? PreviousSchool { get; set; }
        public string? PreviousSchoolAddress { get; set; }

        // Address
        public string? PhysicalAddress { get; set; }
        public string? CityOrSuburb { get; set; }
        public string? PostalCode { get; set; }

        // Contact
        public string? EmergencyPhone { get; set; }
        public string? ChildPhone { get; set; }
        public string? ChildEmail { get; set; }
        public string? ModeOfTransport { get; set; }

        // Medical
        public string? MedicalAidNumber { get; set; }
        public string? MedicalAid { get; set; }
        public string? MedicalAidMainMember { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorAddress { get; set; }
        public string? DoctorPhone { get; set; }
        public string? MedicalCondition { get; set; }
        public string? SpecialProblems { get; set; }
        public string? ChildDexterity { get; set; }

        // Application details
        public string ApplicationStatus { get; set; } = "Pending";
        public DateTime SubmissionDate { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }
    }
}
