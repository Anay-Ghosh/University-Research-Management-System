using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            int? volunteerId = HttpContext.Session.GetInt32("UserId");
            if (volunteerId == null)
            {
                TempData["Error"] = "Please log in as a volunteer.";
                return RedirectToAction("Login", "User");
            }

            // Optional profile
            var profile = _context.VolunteerProfiles
                .FirstOrDefault(v => v.UserId == volunteerId.Value);

            // Assigned requests
            var assignedRequests = _context.HelpRequests
                .Where(r => r.AssignedVolunteerId == volunteerId.Value)
                .ToList();

            // Build dashboard view model
            var model = new VolunteerDashboardViewModel
            {
                Location = profile?.Location ?? "Not set",
                TotalAssignedRequests = assignedRequests.Count,
                PendingRequests = assignedRequests.Count(r => r.Status == "Pending"),
                CompletedRequests = assignedRequests.Count(r => r.Status == "Completed"),
                ProfileExists = profile != null
            };

            return View(model);
        }

        // =========================
        // VIEW ASSIGNED TASKS
        // =========================
        public IActionResult MyTasks(string? status)
        {
            int? volunteerId = HttpContext.Session.GetInt32("UserId");
            if (volunteerId == null)
            {
                TempData["Error"] = "Please log in as a volunteer.";
                return RedirectToAction("Login", "User");
            }

            var tasksQuery = _context.HelpRequests
                .Include(r => r.Requester)
                .Where(r => r.AssignedVolunteerId == volunteerId.Value)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                tasksQuery = tasksQuery.Where(r => r.Status == status);
                ViewBag.Filter = status;
            }

            var tasks = tasksQuery
                .OrderByDescending(r => r.Id)
                .ToList();

            return View(tasks);
        }

        // =========================
        // GET: Edit or Create Profile
        // =========================
        public IActionResult EditProfile()
        {
            int? volunteerId = HttpContext.Session.GetInt32("UserId");
            if (volunteerId == null)
            {
                TempData["Error"] = "Please log in as a volunteer.";
                return RedirectToAction("Login", "User");
            }

            // Try to get existing profile
            var profile = _context.VolunteerProfiles
                .FirstOrDefault(v => v.UserId == volunteerId.Value);

            if (profile == null)
            {
                // Create a new profile object with safe defaults
                profile = new VolunteerProfile
                {
                    UserId = volunteerId.Value,
                    Location = "Not set",
                    Phone = "Not set",
                    Address = "",
                    TaskTypes = "",
                    Latitude = 0,
                    Longitude = 0
                };
            }

            return View(profile);
        }

        // =========================
        // POST: Save Profile (Create or Update)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(VolunteerProfile model)
        {
            int? volunteerId = HttpContext.Session.GetInt32("UserId");
            if (volunteerId == null)
            {
                TempData["Error"] = "Please log in as a volunteer.";
                return RedirectToAction("Login", "User");
            }

            // Make sure UserId is correct
            model.UserId = volunteerId.Value;

            // Check if profile already exists
            var profile = _context.VolunteerProfiles
                .FirstOrDefault(v => v.UserId == volunteerId.Value);

            try
            {
                if (profile == null)
                {
                    // Creation: set safe defaults for required fields
                    model.Location ??= "Not set";
                    model.Phone ??= "Not set";
                    model.Address ??= "";
                    model.TaskTypes ??= "";
                    model.Latitude = model.Latitude;
                    model.Longitude = model.Longitude;

                    _context.VolunteerProfiles.Add(model);
                    TempData["Success"] = "Profile created successfully.";
                }
                else
                {
                    // Update existing profile safely
                    profile.Location = model.Location ?? "Not set";
                    profile.Phone = model.Phone ?? "Not set";
                    profile.Address = model.Address ?? "";
                    profile.TaskTypes = model.TaskTypes ?? "";
                    profile.Latitude = model.Latitude;
                    profile.Longitude = model.Longitude;

                    TempData["Success"] = "Profile updated successfully.";
                }

                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // Show the exact error to help debugging
                TempData["Error"] = $"Failed to save profile. {ex.InnerException?.Message}";
                return View(model);
            }

            return RedirectToAction("Dashboard");
        }


        // =========================
        // MARK TASK AS COMPLETED
        // =========================
        [HttpPost]
        public IActionResult CompleteTask(int id)
        {
            int? volunteerId = HttpContext.Session.GetInt32("UserId");
            if (volunteerId == null)
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToAction("Login", "User");
            }

            var task = _context.HelpRequests
                .FirstOrDefault(r => r.Id == id && r.AssignedVolunteerId == volunteerId.Value);

            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction("MyTasks");
            }

            if (task.Status != "Assigned")
            {
                TempData["Info"] = "This task is already completed.";
                return RedirectToAction("MyTasks");
            }

            task.Status = "Completed";
            _context.SaveChanges();

            TempData["Success"] = "Task marked as completed.";
            return RedirectToAction("MyTasks");
        }
    }
}
