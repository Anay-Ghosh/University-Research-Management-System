using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;

        public AdminController(ApplicationDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalStudents = _context.Users.Count(u => u.Role == "Student");
            ViewBag.TotalSupervisors = _context.Users.Count(u => u.Role == "Supervisor");
            ViewBag.TotalProposals = _context.ResearchProposals.Count();
            ViewBag.PendingProposals = _context.ResearchProposals.Count(p => p.Status == "Pending");
            ViewBag.OngoingProposals = _context.ResearchProposals.Count(p => p.Status == "Ongoing");
            ViewBag.CompletedProposals = _context.ResearchProposals.Count(p => p.Status == "Completed");
            ViewBag.TotalPublications = _context.Publications.Count();

            return View();
        }

        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var users = _context.Users.OrderBy(u => u.Role).ThenBy(u => u.Name).ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            bool hasWork = _context.ResearchProposals.Any(p => p.StudentId == id || p.SupervisorId == id);
            if (hasWork)
            {
                TempData["Error"] = "Cannot delete: this user is linked to existing proposals.";
                return RedirectToAction("Users");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();
            TempData["Success"] = "User deleted.";
            return RedirectToAction("Users");
        }

        public IActionResult Proposals(string? q, string? status, string? department)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var supervisors = _context.Users
                .Where(u => u.Role == "Supervisor")
                .OrderBy(u => u.Name)
                .ToList();

            var profiles = _context.SupervisorProfiles.ToList();

            var loads = _context.ResearchProposals
                .Where(p => p.SupervisorId != null && p.Status != "Completed" && p.Status != "Rejected")
                .Select(p => new { p.SupervisorId, p.StudentId })
                .Distinct()
                .ToList()
                .GroupBy(x => x.SupervisorId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.Supervisors = supervisors;
            ViewBag.SupervisorLoads = loads;
            ViewBag.SupervisorMax = profiles.ToDictionary(p => p.UserId, p => p.MaxStudents);

            var query = _context.ResearchProposals
                .Include(p => p.Student)
                .Include(p => p.Supervisor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p =>
                    p.Title.Contains(q) ||
                    (p.Keywords != null && p.Keywords.Contains(q)) ||
                    (p.Student != null && p.Student.Name.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(p => p.Student != null && p.Student.Department == department);

            var proposals = query
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.Department = department;

            ViewBag.Departments = _context.Users
                .Where(u => u.Department != null && u.Department != "")
                .Select(u => u.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            ViewBag.ResultCount = proposals.Count;

            return View(proposals);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignSupervisor(int proposalId, int supervisorId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals.Find(proposalId);
            if (proposal == null) return NotFound();

            var supervisor = _context.Users.FirstOrDefault(u => u.Id == supervisorId && u.Role == "Supervisor");
            if (supervisor == null)
            {
                TempData["Error"] = "Selected user is not a supervisor.";
                return RedirectToAction("Proposals");
            }

            var profile = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == supervisorId);

            if (profile != null)
            {
                int currentLoad = _context.ResearchProposals
                    .Where(p => p.SupervisorId == supervisorId
                             && p.Id != proposalId
                             && p.Status != "Completed"
                             && p.Status != "Rejected")
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count();

                if (currentLoad >= profile.MaxStudents)
                {
                    TempData["Error"] = supervisor.Name + " is already at capacity ("
                        + currentLoad + "/" + profile.MaxStudents + " students).";
                    return RedirectToAction("Proposals");
                }

                if (!profile.IsAvailable)
                {
                    TempData["Error"] = supervisor.Name + " is currently marked as unavailable.";
                    return RedirectToAction("Proposals");
                }
            }

            proposal.SupervisorId = supervisorId;
            if (proposal.Status == "Pending") proposal.Status = "Ongoing";
            _context.SaveChanges();

            _notify.Notify(proposal.StudentId,
                supervisor.Name + " has been assigned as your supervisor for \"" + proposal.Title + "\".",
                "/Student/Details/" + proposal.Id,
                "Assignment");

            _notify.Notify(supervisorId,
                "You have been assigned to supervise \"" + proposal.Title + "\".",
                "/Supervisor/Review/" + proposal.Id,
                "Assignment");

            TempData["Success"] = "Supervisor assigned.";
            return RedirectToAction("Proposals");
        }

        public IActionResult Publications()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "User");

            var pubs = _context.Publications
                .Include(p => p.Student)
                .OrderByDescending(p => p.Year)
                .ToList();

            return View(pubs);
        }
    }
}




