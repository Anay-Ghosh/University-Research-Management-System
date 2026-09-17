using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace LifeNetAssist.MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                TempData["Error"] = "This email is already registered.";
                return View(user);
            }

            if (ModelState.IsValid)
            {
                // never store the raw password
                user.Password = PasswordHasher.Hash(user.Password);

                _context.Users.Add(user);
                _context.SaveChanges();
                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null && PasswordHasher.Verify(password, user.Password))
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.Name);

                if (user.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                if (user.Role == "Student")
                    return RedirectToAction("Dashboard", "Student");

                var profile = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == user.Id);
                if (profile == null)
                    TempData["Success"] = "Please complete your supervisor profile.";

                return RedirectToAction("Dashboard", "Supervisor");
            }

            TempData["Error"] = "Invalid email or password";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
