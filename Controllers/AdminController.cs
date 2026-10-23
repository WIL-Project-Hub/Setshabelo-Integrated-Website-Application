using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetshabeloAdmissionSystem.Services;
using SetshabeloAdmissionSystem.ViewModels;
using System.Security.Claims;

namespace SetshabeloAdmissionSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SupabaseService _supabase;

        public AdminController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Dashboard()
        {
            var counts = await _supabase.GetStatusCountsAsync();
            var vm = new DashboardViewModel
            {
                TotalApplications = counts["Total"],
                PendingCount = counts["Pending"],
                UnderReviewCount = counts["Under Review"],
                AcceptedCount = counts["Accepted"],
                RejectedCount = counts["Rejected"]
            };
            return View(vm);
        }

        public async Task<IActionResult> ApplicationList(string? status, string? search)
        {
            var apps = await _supabase.GetAllApplicationsAsync();

            if (!string.IsNullOrEmpty(status))
                apps = apps.Where(a => a.ApplicationStatus == status).ToList();

            if (!string.IsNullOrEmpty(search))
                apps = apps.Where(a =>
                    a.ChildFullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.ParentLastName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return View(new ApplicationListViewModel
            {
                Applications = apps,
                StatusFilter = status,
                SearchTerm = search
            });
        }

        public async Task<IActionResult> ApplicationDetail(Guid id)
        {
            var app = await _supabase.GetApplicationByIdAsync(id);
            if (app == null) return NotFound();

            var docs = await _supabase.GetDocumentsForApplicationAsync(id);
            var comments = await _supabase.GetCommentsForApplicationAsync(id);

            return View(new ApplicationDetailViewModel
            {
                Application = app,
                Documents = docs,
                Comments = comments
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, string status)
        {
            var admin = await _supabase.GetAdminByUserIdAsync(UserId);
            if (admin == null || admin.AdminId == Guid.Empty) return Forbid();

            await _supabase.UpdateApplicationStatusAsync(id, status, admin.AdminId);
            TempData["Success"] = $"Application status updated to {status}.";
            return RedirectToAction("ApplicationDetail", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid id, string newComment)
        {
            if (string.IsNullOrWhiteSpace(newComment))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction("ApplicationDetail", new { id });
            }

            var admin = await _supabase.GetAdminByUserIdAsync(UserId);
            if (admin == null || admin.AdminId == Guid.Empty) return Forbid();

            await _supabase.CreateCommentAsync(id, admin.AdminId, newComment);
            TempData["Success"] = "Comment added.";
            return RedirectToAction("ApplicationDetail", new { id });
        }
    }
}
