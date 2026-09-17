using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;

        public SupervisorController(ApplicationDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
        }

        private int? CurrentSupervisorId()
        {
            if (HttpContext.Session.GetString("UserRole") != "Supervisor")
                return null;
            return HttpContext.Session.GetInt32("UserId");
        }

        public IActionResult Dashboard()
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var myProposals = _context.ResearchProposals
                .Include(p => p.Student)
                .Where(p => p.SupervisorId == supId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            var profile = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == supId);

            ViewBag.CurrentLoad = myProposals
                .Where(p => p.Status != "Completed" && p.Status != "Rejected")
                .Select(p => p.StudentId)
                .Distinct()
                .Count();

            ViewBag.MaxStudents = profile != null ? profile.MaxStudents : 0;
            ViewBag.HasProfile = profile != null;
            ViewBag.IsAvailable = profile != null && profile.IsAvailable;

            return View(myProposals);
        }

        public IActionResult Available()
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var open = _context.ResearchProposals
                .Include(p => p.Student)
                .Where(p => p.SupervisorId == null)
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            return View(open);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(int id)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals.FirstOrDefault(p => p.Id == id);
            if (proposal == null) return NotFound();

            if (proposal.SupervisorId != null)
            {
                TempData["Error"] = "This proposal has already been taken by another supervisor.";
                return RedirectToAction("Available");
            }

            var profile = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == supId);

            if (profile == null)
            {
                TempData["Error"] = "Please complete your supervisor profile before accepting students.";
                return RedirectToAction("Profile");
            }

            if (!profile.IsAvailable)
            {
                TempData["Error"] = "Your profile is marked as unavailable. Update it before accepting students.";
                return RedirectToAction("Profile");
            }

            int currentLoad = _context.ResearchProposals
                .Where(p => p.SupervisorId == supId && p.Status != "Completed" && p.Status != "Rejected")
                .Select(p => p.StudentId)
                .Distinct()
                .Count();

            if (currentLoad >= profile.MaxStudents)
            {
                TempData["Error"] = "You have reached your maximum of " + profile.MaxStudents + " students.";
                return RedirectToAction("Available");
            }

            proposal.SupervisorId = supId;
            proposal.Status = "Ongoing";
            _context.SaveChanges();

            var supName = HttpContext.Session.GetString("UserName") ?? "Your supervisor";
            _notify.Notify(proposal.StudentId,
                supName + " is now supervising your research \"" + proposal.Title + "\".",
                "/Student/Details/" + proposal.Id,
                "Assignment");

            TempData["Success"] = "You are now supervising this research.";
            return RedirectToAction("Dashboard");
        }

        public IActionResult Review(int id)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .Include(p => p.Student)
                .Include(p => p.ProgressUpdates)
                .FirstOrDefault(p => p.Id == id && p.SupervisorId == supId);

            if (proposal == null) return NotFound();

            ViewBag.Documents = _context.ResearchDocuments
                .Where(d => d.ProposalId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToList();

            ViewBag.Reviews = _context.Reviews
                .Where(r => r.ProposalId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(proposal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Review(int id, string status, string feedback, DateTime? reviewDeadline, string? deadlineTask)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .FirstOrDefault(p => p.Id == id && p.SupervisorId == supId);
            if (proposal == null) return NotFound();

            proposal.Status = status;
            proposal.Feedback = feedback;

            var hadDeadline = proposal.ReviewDeadline;

            proposal.ReviewDeadline = reviewDeadline;
            proposal.DeadlineTask = deadlineTask;

            // a new or changed deadline reopens the task
            if (reviewDeadline != null && reviewDeadline != hadDeadline)
                proposal.DeadlineResolved = false;

            // keep a permanent record of this review
            if (!string.IsNullOrWhiteSpace(feedback))
            {
                _context.Reviews.Add(new Review
                {
                    ProposalId = proposal.Id,
                    SupervisorId = supId.Value,
                    Feedback = feedback,
                    Status = status,
                    Deadline = reviewDeadline,
                    Task = deadlineTask,
                    Resolved = false,
                    CreatedAt = DateTime.Now
                });
            }

            _context.SaveChanges();

            var msg = "Your supervisor reviewed \"" + proposal.Title + "\" - status: " + status + ".";
            if (reviewDeadline != null)
                msg += " Deadline: " + reviewDeadline.Value.ToString("dd MMM yyyy") + ".";

            _notify.Notify(proposal.StudentId, msg, "/Student/Details/" + proposal.Id, "Feedback");

            TempData["Success"] = "Review saved.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearDeadline(int id)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .FirstOrDefault(p => p.Id == id && p.SupervisorId == supId);
            if (proposal == null) return NotFound();

            proposal.ReviewDeadline = null;
            proposal.DeadlineTask = null;
            proposal.DeadlineResolved = false;
            _context.SaveChanges();

            TempData["Success"] = "Deadline removed.";
            return RedirectToAction("Review", new { id });
        }

        public IActionResult Profile()
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var profile = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == supId)
                          ?? new SupervisorProfile { UserId = supId.Value };

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(SupervisorProfile model)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var existing = _context.SupervisorProfiles.FirstOrDefault(s => s.UserId == supId);

            if (existing == null)
            {
                model.UserId = supId.Value;
                _context.SupervisorProfiles.Add(model);
            }
            else
            {
                existing.ResearchAreas = model.ResearchAreas;
                existing.Designation = model.Designation;
                existing.OfficeRoom = model.OfficeRoom;
                existing.MaxStudents = model.MaxStudents;
                existing.IsAvailable = model.IsAvailable;
            }

            _context.SaveChanges();
            TempData["Success"] = "Profile saved.";
            return RedirectToAction("Dashboard");
        }

        // GET: /Supervisor/Publications
        public IActionResult Publications()
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            // students supervised by this supervisor
            var myStudentIds = _context.ResearchProposals
                .Where(p => p.SupervisorId == supId)
                .Select(p => p.StudentId)
                .Distinct()
                .ToList();

            var pubs = _context.Publications
                .Include(p => p.Student)
                .Include(p => p.Proposal)
                .Where(p => myStudentIds.Contains(p.StudentId))
                .OrderBy(p => p.IsVerified)
                .ThenByDescending(p => p.Year)
                .ToList();

            return View(pubs);
        }

        // POST: /Supervisor/VerifyPublication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyPublication(int id, string remarks)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var pub = _context.Publications.FirstOrDefault(p => p.Id == id);
            if (pub == null) return NotFound();

            bool supervises = _context.ResearchProposals
                .Any(p => p.SupervisorId == supId && p.StudentId == pub.StudentId);
            if (!supervises) return NotFound();

            pub.IsVerified = true;
            pub.VerifiedAt = DateTime.Now;
            pub.VerifierRemarks = remarks;
            _context.SaveChanges();

            _notify.Notify(pub.StudentId,
                "Your publication \"" + pub.Title + "\" was verified by your supervisor.",
                "/Student/Publications",
                "Publication");

            TempData["Success"] = "Publication verified.";
            return RedirectToAction("Publications");
        }

        // POST: /Supervisor/UnverifyPublication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnverifyPublication(int id)
        {
            var supId = CurrentSupervisorId();
            if (supId == null) return RedirectToAction("Login", "User");

            var pub = _context.Publications.FirstOrDefault(p => p.Id == id);
            if (pub == null) return NotFound();

            bool supervises = _context.ResearchProposals
                .Any(p => p.SupervisorId == supId && p.StudentId == pub.StudentId);
            if (!supervises) return NotFound();

            pub.IsVerified = false;
            pub.VerifiedAt = null;
            _context.SaveChanges();

            TempData["Success"] = "Verification removed.";
            return RedirectToAction("Publications");
        }
    }
}







