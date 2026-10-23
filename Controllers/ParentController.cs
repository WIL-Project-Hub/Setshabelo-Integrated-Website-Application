using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetshabeloAdmissionSystem.Models;
using SetshabeloAdmissionSystem.Services;
using SetshabeloAdmissionSystem.ViewModels;
using System.Security.Claims;

namespace SetshabeloAdmissionSystem.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        private readonly SupabaseService _supabase;

        public ParentController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // 
        // HOME
        // 
        public async Task<IActionResult> ParentHome()
        {
            var parent = await _supabase.GetParentByUserIdAsync(UserId);
            if (parent == null || parent.ParentId == Guid.Empty)
            {
                Console.WriteLine($"[PARENT] No parent row for user {UserId}");
                return RedirectToAction("AccessDenied", "Account");
            }

            var apps = await _supabase.GetApplicationsByParentIdAsync(parent.ParentId);
            var updates = await _supabase.GetSchoolUpdatesAsync();
            var achievements = await _supabase.GetAchievementsAsync();

            var vm = new ParentHomeViewModel
            {
                ParentFirstName = parent.FirstName,
                TotalApplications = apps.Count,
                PendingCount = apps.Count(a => a.ApplicationStatus == "Pending"),
                AcceptedCount = apps.Count(a => a.ApplicationStatus == "Accepted"),
                RejectedCount = apps.Count(a => a.ApplicationStatus == "Rejected"),
                RecentUpdates = updates.Take(3).ToList(),
                RecentAchievements = achievements.Take(3).ToList()
            };
            return View(vm);
        }

        // 
        // PROFILE
        // 

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var parent = await _supabase.GetParentByUserIdAsync(UserId);
            if (parent == null || parent.ParentId == Guid.Empty)
                return RedirectToAction("AccessDenied", "Account");

            var email = User.FindFirstValue(ClaimTypes.Email) ?? "";

            return View(new ParentProfileViewModel
            {
                Email = email,
                FirstName = parent.FirstName,
                LastName = parent.LastName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ParentProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var parent = await _supabase.GetParentByUserIdAsync(UserId);
            if (parent == null || parent.ParentId == Guid.Empty)
                return RedirectToAction("AccessDenied", "Account");

            var ok = await _supabase.UpdateParentNameAsync(
                parent.ParentId, model.FirstName, model.LastName);

            if (!ok)
            {
                ModelState.AddModelError("", "Could not update profile. Please try again.");
                return View(model);
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // 
        // NEW APPLICATION — 6-STEP WIZARD
        // 
        [HttpGet]
        public IActionResult NewApplication()
        {
            return View(new ApplicationWizardViewModel { CurrentStep = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> NewApplication(ApplicationWizardViewModel model, string action)
        {
            ModelState.Clear();

            // PREVIOUS STEP
            if (action == "prev")
            {
                model.CurrentStep = Math.Max(1, model.CurrentStep - 1);
                return View(model);
            }

            // NEXT STEP
            if (action == "next")
            {
                if (!ValidateStep(model))
                    return View(model);

                model.CurrentStep = Math.Min(6, model.CurrentStep + 1);
                return View(model);
            }

            // SUBMIT
            if (action == "submit")
            {
                // Full validation across steps 1 to 5
                for (int step = 1; step <= 5; step++)
                {
                    model.CurrentStep = step;
                    if (!ValidateStep(model))
                    {
                        return View(model);
                    }
                }

                model.CurrentStep = 6;

                var parent = await _supabase.GetParentByUserIdAsync(UserId);
                if (parent == null || parent.ParentId == Guid.Empty)
                {
                    ModelState.AddModelError("", "Parent profile not found.");
                    return View(model);
                }

                var app = new Application
                {
                    ParentId = parent.ParentId,

                    // Parent info
                    ParentTitle = model.ParentTitle,
                    ParentInitials = model.ParentInitials,
                    ParentFirstName = model.ParentFirstName,
                    ParentLastName = model.ParentLastName,
                    ParentIdNumber = model.ParentIdNumber,
                    ParentPhone = model.ParentPhone,
                    ParentEmail = model.ParentEmail,
                    ParentAddress = model.ParentAddress,
                    ParentRelationship = model.ParentRelationship,
                    ParentRace = model.ParentRace,
                    ParentHomeLanguage = model.ParentHomeLanguage,
                    ParentOccupation = model.ParentOccupation,
                    ParentEmployer = model.ParentEmployer,

                    // Child
                    ChildFullName = model.ChildFullName,
                    ChildDateOfBirth = model.ChildDateOfBirth,
                    ChildGender = model.ChildGender,
                    ChildRace = model.ChildRace,
                    ChildIdNumber = model.ChildIdNumber,
                    ChildCitizenship = model.ChildCitizenship,
                    CountryOfResidence = model.CountryOfResidence,
                    Province = model.Province,
                    ChildHomeLanguage = model.ChildHomeLanguage,
                    PreferredLanguage = model.PreferredLanguage,
                    GradeApplyingFor = model.GradeApplyingFor,
                    PrePrimaryEducation = model.PrePrimaryEducation,

                    // Education
                    HighestGradePassed = model.HighestGradePassed,
                    YearGradePassed = model.YearGradePassed,
                    AccessionNo = model.AccessionNo,
                    PreviousSchool = model.PreviousSchool,
                    PreviousSchoolAddress = model.PreviousSchoolAddress,

                    // Address
                    PhysicalAddress = model.PhysicalAddress,
                    CityOrSuburb = model.CityOrSuburb,
                    PostalCode = model.PostalCode,

                    // Contact
                    EmergencyPhone = model.EmergencyPhone,
                    ChildPhone = model.ChildPhone,
                    ChildEmail = model.ChildEmail,
                    ModeOfTransport = model.ModeOfTransport,

                    // Medical
                    MedicalAidNumber = model.MedicalAidNumber,
                    MedicalAid = model.MedicalAid,
                    MedicalAidMainMember = model.MedicalAidMainMember,
                    DoctorName = model.DoctorName,
                    DoctorAddress = model.DoctorAddress,
                    DoctorPhone = model.DoctorPhone,
                    MedicalCondition = model.MedicalCondition,
                    SpecialProblems = model.SpecialProblems,
                    ChildDexterity = model.ChildDexterity
                };

                var appId = await _supabase.CreateApplicationAsync(app);
                if (appId == null)
                {
                    ModelState.AddModelError("", "Could not submit application. Please try again.");
                    return View(model);
                }

                // Upload documents
                if (model.Documents != null && model.Documents.Count > 0)
                {
                    for (int i = 0; i < model.Documents.Count; i++)
                    {
                        var file = model.Documents[i];
                        if (file.Length == 0) continue;

                        var safeName = $"{appId}/{Guid.NewGuid()}_{file.FileName}";
                        var url = await _supabase.UploadFileToStorageAsync(file, safeName);
                        if (url != null)
                        {
                            var type = (model.DocumentTypes != null &&
                                        i < model.DocumentTypes.Count)
                                ? model.DocumentTypes[i] : "Supporting Document";

                            await _supabase.CreateDocumentAsync(appId.Value, url, type);
                        }
                    }
                }

                TempData["Success"] = "Application submitted successfully!";
                return RedirectToAction("MyApplications");
            }

            return View(model);
        }

        // 
        // EACH-STEP VALIDATION
        //
        private bool ValidateStep(ApplicationWizardViewModel model)
        {
            switch (model.CurrentStep)
            {
                case 1: // Child
                    if (string.IsNullOrWhiteSpace(model.ChildFullName))
                        ModelState.AddModelError(nameof(model.ChildFullName),
                            "Child's full name is required.");
                    if (model.ChildDateOfBirth == null)
                        ModelState.AddModelError(nameof(model.ChildDateOfBirth),
                            "Date of birth is required.");
                    if (string.IsNullOrWhiteSpace(model.ChildGender))
                        ModelState.AddModelError(nameof(model.ChildGender),
                            "Please select a gender.");
                    if (string.IsNullOrWhiteSpace(model.GradeApplyingFor))
                        ModelState.AddModelError(nameof(model.GradeApplyingFor),
                            "Please select a grade.");
                    break;

                case 2: // Education
                    // Optional
                    break;

                case 3: // Parent
                    if (string.IsNullOrWhiteSpace(model.ParentFirstName))
                        ModelState.AddModelError(nameof(model.ParentFirstName),
                            "First name is required.");
                    if (string.IsNullOrWhiteSpace(model.ParentLastName))
                        ModelState.AddModelError(nameof(model.ParentLastName),
                            "Last name is required.");
                    if (string.IsNullOrWhiteSpace(model.ParentPhone))
                        ModelState.AddModelError(nameof(model.ParentPhone),
                            "Phone is required.");
                    if (string.IsNullOrWhiteSpace(model.ParentEmail))
                        ModelState.AddModelError(nameof(model.ParentEmail),
                            "Email is required.");
                    if (string.IsNullOrWhiteSpace(model.ParentRelationship))
                        ModelState.AddModelError(nameof(model.ParentRelationship),
                            "Please select a relationship.");
                    break;

                case 4: // Contact
                    if (string.IsNullOrWhiteSpace(model.PhysicalAddress))
                        ModelState.AddModelError(nameof(model.PhysicalAddress),
                            "Physical address is required.");
                    break;

                case 5: // Medical
                    // Optional
                    break;

                case 6: // Documents
                    // Optional
                    break;
            }

            return ModelState.IsValid;
        }

        // 
        // MY APPLICATIONS
        // 
        public async Task<IActionResult> MyApplications()
        {
            var parent = await _supabase.GetParentByUserIdAsync(UserId);
            if (parent == null || parent.ParentId == Guid.Empty)
                return RedirectToAction("AccessDenied", "Account");

            var apps = await _supabase.GetApplicationsByParentIdAsync(parent.ParentId);
            return View(new MyApplicationsViewModel { Applications = apps });
        }

        // 
        // APPLICATION STATUS
        // 
        public async Task<IActionResult> ApplicationStatus(Guid id)
        {
            var parent = await _supabase.GetParentByUserIdAsync(UserId);
            if (parent == null || parent.ParentId == Guid.Empty)
                return RedirectToAction("AccessDenied", "Account");

            var app = await _supabase.GetApplicationByIdAsync(id);
            if (app == null || app.ParentId != parent.ParentId)
                return Forbid();

            var docs = await _supabase.GetDocumentsForApplicationAsync(id);
            var comments = await _supabase.GetCommentsForApplicationAsync(id);

            return View(new ParentApplicationStatusViewModel
            {
                Application = app,
                Documents = docs,
                Comments = comments
            });
        }

        // 
        // UPDATES & ACHIEVEMENTS
        // 
        public async Task<IActionResult> Updates()
        {
            var updates = await _supabase.GetSchoolUpdatesAsync();
            var achievements = await _supabase.GetAchievementsAsync();
            return View(new UpdatesListViewModel
            {
                Updates = updates,
                Achievements = achievements
            });
        }

        public async Task<IActionResult> Achievements()
        {
            var achievements = await _supabase.GetAchievementsAsync();
            return View(achievements);
        }
    }
}
