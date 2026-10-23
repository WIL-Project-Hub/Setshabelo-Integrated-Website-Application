using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SetshabeloAdmissionSystem.Services;
using SetshabeloAdmissionSystem.ViewModels;
using System.Security.Claims;

namespace SetshabeloAdmissionSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SupabaseService _supabase;

        public AccountController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // 
        // LOGIN
        // 

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var auth = await _supabase.SignInAsync(model.Email, model.Password);
            if (auth == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var profile = await _supabase.GetProfileAsync(auth.UserId, auth.AccessToken);
            if (profile == null)
            {
                ModelState.AddModelError("", "Profile not found.");
                return View(model);
            }

            // Get First Name for the navbar
            string firstName = profile.Email.Split('@')[0];
            if (profile.Role == "Parent")
            {
                var parent = await _supabase.GetParentByUserIdAsync(auth.UserId);
                if (parent != null) firstName = parent.FirstName;
            }
            else if (profile.Role == "Admin" || profile.Role == "SuperAdmin")
            {
                var admin = await _supabase.GetAdminByUserIdAsync(auth.UserId);
                if (admin != null) firstName = admin.FirstName;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, auth.UserId),
                new Claim(ClaimTypes.Email, auth.Email),
                new Claim(ClaimTypes.GivenName, firstName),
                new Claim("access_token", auth.AccessToken)
            };

            // SuperAdmin gets both roles so they can access all admin features
            if (profile.Role == "SuperAdmin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, profile.Role));
            }

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Redirect based on role
            return profile.Role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "SuperAdmin" => RedirectToAction("Dashboard", "Admin"), // 👈 SuperAdmin goes to Admin Dashboard
                "Parent" => RedirectToAction("ParentHome", "Parent"),
                _ => RedirectToAction("AccessDenied", "Account")
            };
        }

        // 
        // REGISTER (Parent self-registration)
        // 

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var auth = await _supabase.SignUpAsync(model.Email, model.Password);
            if (auth == null)
            {
                ModelState.AddModelError("",
                    "Could not create account. The email may already be in use.");
                return View(model);
            }

            var ok = await _supabase.CreateProfileAndParentAsync(
                auth.UserId, model.Email, model.FirstName, model.LastName);

            if (!ok)
            {
                ModelState.AddModelError("",
                    "Account created, but profile setup failed. " +
                    "Please contact the school administrator.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(auth.AccessToken))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, auth.UserId),
                    new Claim(ClaimTypes.Email, auth.Email),
                    new Claim(ClaimTypes.Role, "Parent"),
                    new Claim(ClaimTypes.GivenName, model.FirstName),
                    new Claim("access_token", auth.AccessToken)
                };
                var identity = new ClaimsIdentity(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return RedirectToAction("ParentHome", "Parent");
            }

            TempData["Success"] =
                "Account created! Please check your email to confirm, then log in.";
            return RedirectToAction("Login");
        }

        // 
        // LOGOUT + ACCESS DENIED
        // 

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
