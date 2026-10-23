namespace SetshabeloAdmissionSystem.ViewModels
{
    public class ApplicationWizardViewModel
    {
        public int CurrentStep { get; set; } = 1;

        // STEP 1 — CHILD
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

        // STEP 2 — EDUCATION
        public string? HighestGradePassed { get; set; }
        public string? YearGradePassed { get; set; }
        public string? AccessionNo { get; set; }
        public string? PreviousSchool { get; set; }
        public string? PreviousSchoolAddress { get; set; }

        // STEP 3 — PARENT 
        public string? ParentTitle { get; set; }
        public string? ParentInitials { get; set; }
        public string ParentFirstName { get; set; } = string.Empty;
        public string ParentLastName { get; set; } = string.Empty;
        public string? ParentIdNumber { get; set; }
        public string? ParentRace { get; set; }
        public string? ParentHomeLanguage { get; set; }
        public string? ParentOccupation { get; set; }
        public string? ParentEmployer { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string? ParentAddress { get; set; }
        public string? ParentRelationship { get; set; }

        // STEP 4 — CONTACT
        public string? PhysicalAddress { get; set; }
        public string? CityOrSuburb { get; set; }
        public string? PostalCode { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? ChildPhone { get; set; }
        public string? ChildEmail { get; set; }
        public string? ModeOfTransport { get; set; }

        // STEP 5 — MEDICAL
        public string? MedicalAidNumber { get; set; }
        public string? MedicalAid { get; set; }
        public string? MedicalAidMainMember { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorAddress { get; set; }
        public string? DoctorPhone { get; set; }
        public string? MedicalCondition { get; set; }
        public string? SpecialProblems { get; set; }
        public string? ChildDexterity { get; set; }

        // STEP 6 — DOCUMENTS 
        public List<IFormFile>? Documents { get; set; }
        public List<string> DocumentTypes { get; set; } = new();
    }
}
