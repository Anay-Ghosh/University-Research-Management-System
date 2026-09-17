using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GitHubService _github;
        private readonly NotificationService _notify;

        public StudentController(ApplicationDbContext context, GitHubService github, NotificationService notify)
        {
            _context = context;
            _github = github;
            _notify = notify;
        }

        private int? CurrentStudentId()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return null;
            return HttpContext.Session.GetInt32("UserId");
        }

        public IActionResult Dashboard()
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var proposals = _context.ResearchProposals
                .Include(p => p.Supervisor)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            return View(proposals);
        }

        public IActionResult SubmitProposal()
        {
            if (CurrentStudentId() == null) return RedirectToAction("Login", "User");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitProposal(ResearchProposal proposal)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            proposal.StudentId = studentId.Value;
            proposal.Status = "Pending";
            proposal.SubmittedAt = DateTime.Now;

            ModelState.Remove("Student");
            ModelState.Remove("Status");

            if (!ModelState.IsValid) return View(proposal);

            _context.ResearchProposals.Add(proposal);
            _context.SaveChanges();

            TempData["Success"] = "Proposal submitted successfully!";
            return RedirectToAction("Dashboard");
        }

        public IActionResult Details(int id)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .Include(p => p.Supervisor)
                .Include(p => p.ProgressUpdates)
                .FirstOrDefault(p => p.Id == id && p.StudentId == studentId);

            if (proposal == null) return NotFound();

            ViewBag.Documents = _context.ResearchDocuments
                .Where(d => d.ProposalId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToList();

            ViewBag.Reviews = _context.Reviews
                .Include(r => r.Supervisor)
                .Where(r => r.ProposalId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(proposal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProgress(int proposalId, string title, string description, int percentComplete)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var owns = _context.ResearchProposals
                .Any(p => p.Id == proposalId && p.StudentId == studentId);
            if (!owns) return NotFound();

            _context.ProgressUpdates.Add(new ProgressUpdate
            {
                ProposalId = proposalId,
                Title = title,
                Description = description,
                PercentComplete = percentComplete,
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            var proposal = _context.ResearchProposals.FirstOrDefault(p => p.Id == proposalId);
            if (proposal != null && proposal.SupervisorId != null)
            {
                var studentName = HttpContext.Session.GetString("UserName") ?? "Your student";
                _notify.Notify(proposal.SupervisorId.Value,
                    studentName + " posted a progress update on \"" + proposal.Title + "\".",
                    "/Supervisor/Review/" + proposalId,
                    "Feedback");
            }

            TempData["Success"] = "Progress update added.";
            return RedirectToAction("Details", new { id = proposalId });
        }

        // POST: /Student/ResolveDeadline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveDeadline(int proposalId)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .FirstOrDefault(p => p.Id == proposalId && p.StudentId == studentId);
            if (proposal == null) return NotFound();

            proposal.DeadlineResolved = true;
            _context.SaveChanges();

            if (proposal.SupervisorId != null)
            {
                var studentName = HttpContext.Session.GetString("UserName") ?? "Your student";
                _notify.Notify(proposal.SupervisorId.Value,
                    studentName + " marked the review task on \"" + proposal.Title + "\" as done.",
                    "/Supervisor/Review/" + proposalId,
                    "Feedback");
            }

            TempData["Success"] = "Marked as done. Your supervisor has been notified.";
            return RedirectToAction("Details", new { id = proposalId });
        }

        // POST: /Student/UpdateRepo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateRepo(int proposalId, string gitHubRepoUrl)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .FirstOrDefault(p => p.Id == proposalId && p.StudentId == studentId);
            if (proposal == null) return NotFound();

            proposal.GitHubRepoUrl = gitHubRepoUrl;
            _context.SaveChanges();

            TempData["Success"] = "Repository link saved.";
            return RedirectToAction("Details", new { id = proposalId });
        }

        // POST: /Student/SyncGitHub
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncGitHub(int proposalId)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var proposal = _context.ResearchProposals
                .FirstOrDefault(p => p.Id == proposalId && p.StudentId == studentId);
            if (proposal == null) return NotFound();

            var (commits, error) = await _github.GetCommitsAsync(proposal.GitHubRepoUrl);

            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", new { id = proposalId });
            }

            var existing = _context.ProgressUpdates
                .Where(u => u.ProposalId == proposalId && u.CommitHash != null)
                .Select(u => u.CommitHash!)
                .ToHashSet();

            int added = 0;
            foreach (var c in commits)
            {
                if (existing.Contains(c.Sha)) continue;

                var firstLine = c.Message.Split('\n')[0];
                if (firstLine.Length > 120) firstLine = firstLine.Substring(0, 120);

                _context.ProgressUpdates.Add(new ProgressUpdate
                {
                    ProposalId = proposalId,
                    Title = "Commit: " + firstLine,
                    Description = "Committed by " + c.Author + " on " + c.Date.ToString("dd MMM yyyy") + "\n" + c.Url,
                    PercentComplete = 0,
                    CreatedAt = c.Date,
                    CommitHash = c.Sha,
                    CommitMessage = c.Message
                });
                added++;
            }

            if (added > 0) _context.SaveChanges();

            TempData["Success"] = added > 0
                ? added + " new commit(s) imported from GitHub."
                : "Already up to date with GitHub.";

            return RedirectToAction("Details", new { id = proposalId });
        }

        public IActionResult Publications()
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var pubs = _context.Publications
                .Include(p => p.Proposal)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Year)
                .ToList();

            return View(pubs);
        }

        public IActionResult AddPublication()
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            ViewBag.MyProposals = _context.ResearchProposals
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPublication(Publication publication)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            publication.StudentId = studentId.Value;
            publication.AddedAt = DateTime.Now;

            ModelState.Remove("Student");
            ModelState.Remove("Proposal");

            if (!ModelState.IsValid)
            {
                ViewBag.MyProposals = _context.ResearchProposals
                    .Where(p => p.StudentId == studentId)
                    .ToList();
                return View(publication);
            }

            _context.Publications.Add(publication);
            _context.SaveChanges();

            var supervisorIds = _context.ResearchProposals
                .Where(p => p.StudentId == studentId && p.SupervisorId != null)
                .Select(p => p.SupervisorId!.Value)
                .Distinct()
                .ToList();

            var name = HttpContext.Session.GetString("UserName") ?? "Your student";
            foreach (var sid in supervisorIds)
            {
                _notify.Notify(sid,
                    name + " added a publication awaiting your verification.",
                    "/Supervisor/Publications",
                    "Publication");
            }

            TempData["Success"] = "Publication recorded.";
            return RedirectToAction("Publications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePublication(int id)
        {
            var studentId = CurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "User");

            var pub = _context.Publications
                .FirstOrDefault(p => p.Id == id && p.StudentId == studentId);
            if (pub == null) return NotFound();

            _context.Publications.Remove(pub);
            _context.SaveChanges();

            TempData["Success"] = "Publication removed.";
            return RedirectToAction("Publications");
        }
    }
}





