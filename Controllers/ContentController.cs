using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetshabeloAdmissionSystem.Services;
using SetshabeloAdmissionSystem.ViewModels;
using System.Security.Claims;

namespace SetshabeloAdmissionSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContentController : Controller
    {
        private readonly SupabaseService _supabase;

        public ContentController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> ManageContent()
        {
            var updates = await _supabase.GetSchoolUpdatesAsync();
            var achievements = await _supabase.GetAchievementsAsync();
            return View(new ManageContentViewModel
            {
                Updates = updates,
                Achievements = achievements
            });
        }

        // 
        // SCHOOL UPDATES
        // 

        [HttpGet]
        public IActionResult PublishUpdate()
        {
            return View(new PublishUpdateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishUpdate(PublishUpdateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _supabase.GetAdminByUserIdAsync(UserId);
            if (admin == null || admin.AdminId == Guid.Empty) return Forbid();

            var ok = await _supabase.CreateSchoolUpdateAsync(
                admin.AdminId, model.Title, model.Content);

            if (!ok)
            {
                ModelState.AddModelError("", "Could not publish update.");
                return View(model);
            }

            TempData["Success"] = "Update published.";
            return RedirectToAction("ManageContent");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateDetails(Guid id)
        {
            var updates = await _supabase.GetSchoolUpdatesAsync();
            var update = updates.FirstOrDefault(u => u.UpdateId == id);
            if (update == null) return NotFound();

            return View(new UpdateDetailsViewModel { Update = update });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUpdate(Guid id)
        {
            await _supabase.DeleteSchoolUpdateAsync(id);
            TempData["Success"] = "Update deleted.";
            return RedirectToAction("ManageContent");
        }

        // 
        // ACHIEVEMENTS
        // 

        [HttpGet]
        public IActionResult PublishAchievement()
        {
            return View(new PublishAchievementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishAchievement(PublishAchievementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _supabase.GetAdminByUserIdAsync(UserId);
            if (admin == null || admin.AdminId == Guid.Empty) return Forbid();

            // Handle Image Upload
            string? imageUrl = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Create a unique file name to avoid collision errors
                var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                imageUrl = await _supabase.UploadAchievementImageAsync(model.ImageFile, fileName);

                if (imageUrl == null)
                {
                    ModelState.AddModelError("", "Failed to upload image. Check Supabase bucket settings.");
                    return View(model);
                }
            }

            var ok = await _supabase.CreateAchievementAsync(
                admin.AdminId, model.Title, model.Description, imageUrl);

            if (!ok)
            {
                ModelState.AddModelError("", "Could not publish achievement.");
                return View(model);
            }

            TempData["Success"] = "Achievement published.";
            return RedirectToAction("ManageContent");
        }

        [HttpGet]
        public async Task<IActionResult> AchievementDetails(Guid id)
        {
            var achievement = await _supabase.GetAchievementByIdAsync(id);
            if (achievement == null) return NotFound();

            return View(new AchievementDetailsViewModel { Achievement = achievement });
        }

        [HttpGet]
        public async Task<IActionResult> EditAchievement(Guid id)
        {
            var achievement = await _supabase.GetAchievementByIdAsync(id);
            if (achievement == null) return NotFound();

            var vm = new EditAchievementViewModel
            {
                AchievementId = achievement.AchievementId,
                Title = achievement.Title,
                Description = achievement.Description,
                ExistingImageUrl = achievement.ImageUrl // Store current URL to keep if no new upload
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAchievement(EditAchievementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? finalImageUrl = model.ExistingImageUrl;

            // If a new image is uploaded, replace the old one
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                var uploadedUrl = await _supabase.UploadAchievementImageAsync(model.ImageFile, fileName);

                if (uploadedUrl == null)
                {
                    ModelState.AddModelError("", "Failed to upload new image. Check bucket permissions.");
                    return View(model);
                }

                finalImageUrl = uploadedUrl;
            }

            var ok = await _supabase.UpdateAchievementAsync(
                model.AchievementId, model.Title, model.Description, finalImageUrl);

            if (!ok)
            {
                ModelState.AddModelError("", "Could not update achievement.");
                return View(model);
            }

            TempData["Success"] = "Achievement updated.";
            return RedirectToAction("ManageContent");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAchievement(Guid id)
        {
            await _supabase.DeleteAchievementAsync(id);
            TempData["Success"] = "Achievement deleted.";
            return RedirectToAction("ManageContent");
        }
    }
}
