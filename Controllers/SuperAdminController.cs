using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetshabeloAdmissionSystem.Services;
using SetshabeloAdmissionSystem.ViewModels;

namespace SetshabeloAdmissionSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin")] // 👈 Only SuperAdmin can access
    public class SuperAdminController : Controller
    {
        private readonly SupabaseService _supabase;

        public SuperAdminController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public IActionResult RegisterAdmin()
        {
            return View(new RegisterAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(RegisterAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Create the Auth user (bypasses email confirmation using Service Role)
            var auth = await _supabase.CreateAdminUserAsync(model.Email, model.Password);
            if (auth == null)
            {
                ModelState.AddModelError("",
                    "Could not create admin account. The email may already be in use.");
                return View(model);
            }

            // 2. Insert into profiles (role='Admin') and admins tables
            var ok = await _supabase.CreateAdminProfileAsync(
                auth.UserId, model.Email, model.FirstName, model.LastName);

            if (!ok)
            {
                ModelState.AddModelError("",
                    "Account created in Auth, but database setup failed. " +
                    "Please check Supabase logs.");
                return View(model);
            }

            TempData["Success"] =
                $"Admin {model.FirstName} {model.LastName} created successfully!";

            return RedirectToAction("Dashboard", "Admin");
        }
    }
}
