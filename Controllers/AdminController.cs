using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // ADMIN DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            // Build dashboard stats
            var model = new AdminDashboardViewModel
            {
                TotalRequests = _context.HelpRequests.Count(),
                PendingRequests = _context.HelpRequests.Count(r => r.Status == "Pending"),
                AssignedRequests = _context.HelpRequests.Count(r => r.Status == "Assigned"),
                CompletedRequests = _context.HelpRequests.Count(r => r.Status == "Completed"),
                TotalVolunteers = _context.Users.Count(u => u.Role == "Volunteer"),
                ClosestVolunteers = new List<ClosestVolunteerViewModel>()
            };

            // Compute closest volunteer for pending requests
            var pendingRequests = _context.HelpRequests
                .Where(r => r.Status == "Pending")
                .ToList();

            var volunteers = _context.Users
                .Where(u => u.Role == "Volunteer")
                .Include(u => u.VolunteerProfile)
                .Where(u => u.VolunteerProfile != null)
                .ToList();

            foreach (var req in pendingRequests)
            {
                User? closestVolunteer = null;
                double minDistance = double.MaxValue;

                foreach (var vol in volunteers)
                {
                    if (vol.VolunteerProfile == null) continue;

                    double distance = GetDistance(
                        req.Latitude,
                        req.Longitude,
                        vol.VolunteerProfile.Latitude,
                        vol.VolunteerProfile.Longitude
                    );

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestVolunteer = vol;
                    }
                }

                if (closestVolunteer != null)
                {
                    model.ClosestVolunteers.Add(new ClosestVolunteerViewModel
                    {
                        RequesterName = req.RequesterName,
                        VolunteerName = closestVolunteer.Name,
                        DistanceKm = Math.Round(minDistance, 2)
                    });
                }
            }

            return View(model);
        }

        // =========================
        // VIEW REQUESTS (All / Pending / Assigned / Completed)
        // =========================
        public IActionResult Requests(string? status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var query = _context.HelpRequests
                .Include(r => r.Requester)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var requestList = query
                .OrderByDescending(r => r.Id)
                .ToList()
                .Select(r =>
                {
                    User? assignedVolunteer = null;
                    double? distanceKm = null;

                    if (r.AssignedVolunteerId != null)
                    {
                        assignedVolunteer = _context.Users
                            .Include(u => u.VolunteerProfile)
                            .FirstOrDefault(u => u.Id == r.AssignedVolunteerId);

                        if (assignedVolunteer?.VolunteerProfile != null)
                        {
                            distanceKm = GetDistance(
                                r.Latitude,
                                r.Longitude,
                                assignedVolunteer.VolunteerProfile.Latitude,
                                assignedVolunteer.VolunteerProfile.Longitude
                            );
                        }
                    }

                    return new RequestWithVolunteerViewModel
                    {
                        Request = r,
                        AssignedVolunteer = assignedVolunteer,
                        DistanceKm = distanceKm
                    };
                })
                .ToList();

            ViewBag.Filter = string.IsNullOrEmpty(status) ? "All Requests" : status + " Requests";

            // Explicitly return your existing AllRequest.cshtml
            return View("AllRequests", requestList);
        }

        // =========================
        // VOLUNTEERS LIST
        // =========================
        public IActionResult Volunteers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var volunteers = _context.Users
                .Where(u => u.Role == "Volunteer")
                .Include(u => u.VolunteerProfile)
                .ToList();

            return View(volunteers);
        }

        // =========================
        // DELETE VOLUNTEER
        // =========================
        public IActionResult DeleteVolunteer(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var volunteer = _context.Users
                .Include(u => u.VolunteerProfile)
                .FirstOrDefault(u => u.Id == id && u.Role == "Volunteer");

            if (volunteer == null)
            {
                TempData["Error"] = "Volunteer not found.";
                return RedirectToAction("Volunteers");
            }

            if (volunteer.VolunteerProfile != null)
                _context.VolunteerProfiles.Remove(volunteer.VolunteerProfile);

            _context.Users.Remove(volunteer);
            _context.SaveChanges();

            TempData["Success"] = "Volunteer deleted successfully.";
            return RedirectToAction("Volunteers");
        }

        // =========================
        // ASSIGN VOLUNTEER TO REQUEST (closest distance)
        // =========================
        public IActionResult AssignVolunteer(int requestId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "User");

            var request = _context.HelpRequests.FirstOrDefault(r => r.Id == requestId);

            if (request == null)
            {
                TempData["Error"] = "Help request not found.";
                return RedirectToAction("Requests");
            }

            if (request.Status != "Pending")
            {
                TempData["Info"] = "Request already assigned.";
                return RedirectToAction("Requests");
            }

            var volunteers = _context.Users
                .Where(u => u.Role == "Volunteer")
                .Include(u => u.VolunteerProfile)
                .Where(u => u.VolunteerProfile != null)
                .ToList();

            if (!volunteers.Any())
            {
                TempData["Error"] = "No volunteers available.";
                return RedirectToAction("Requests");
            }

            User? closestVolunteer = null;
            double minDistance = double.MaxValue;

            foreach (var v in volunteers)
            {
                double distance = GetDistance(
                    request.Latitude,
                    request.Longitude,
                    v.VolunteerProfile!.Latitude,
                    v.VolunteerProfile!.Longitude
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestVolunteer = v;
                }
            }

            if (closestVolunteer != null)
            {
                request.AssignedVolunteerId = closestVolunteer.Id;
                request.Status = "Assigned";
                _context.SaveChanges();

                TempData["Success"] = $"Volunteer {closestVolunteer.Name} assigned (Distance: {minDistance:F2} km).";
            }

            return RedirectToAction("Requests");
        }

        // =========================
        // HELPER METHODS
        // =========================
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of Earth in km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private double ToRad(double angle) => angle * (Math.PI / 180);
    }
}
