
using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
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

        // GET: /User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                TempData["Success"] = "Registration successful!";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // GET: /User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                // store user info in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role);

                // redirect based on role
                if (user.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else if (user.Role == "Requester")
                    return RedirectToAction("Dashboard", "Requester");
                else
                {
                    // Volunteer: if they don't have a VolunteerProfile yet, send them to create/edit it
                    var profile = _context.VolunteerProfiles.FirstOrDefault(v => v.UserId == user.Id);
                    if (profile == null)
                    {
                        TempData["Success"] = "Please complete your volunteer profile.";
                        return RedirectToAction("Dashboard", "Volunteer");
                    }

                    return RedirectToAction("Dashboard", "Volunteer");
                }
            }

            TempData["Error"] = "Invalid email or password";
            return View();
        }
    }
}