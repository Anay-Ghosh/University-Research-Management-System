using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace LifeNetAssist.MVC.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] Allowed = { ".pdf", ".docx", ".doc", ".pptx", ".zip" };
        private const long MaxBytes = 20 * 1024 * 1024; // 20 MB

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId");
        private string? CurrentRole() => HttpContext.Session.GetString("UserRole");

        // Can this user see documents of this proposal?
        private bool CanAccess(int proposalId, int userId, string role)
        {
            if (role == "Admin") return true;

            var p = _context.ResearchProposals.FirstOrDefault(x => x.Id == proposalId);
            if (p == null) return false;

            if (role == "Student") return p.StudentId == userId;
            if (role == "Supervisor") return p.SupervisorId == userId;

            return false;
        }

        private string UploadFolder()
        {
            var root = string.IsNullOrEmpty(_env.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                : _env.WebRootPath;

            var folder = Path.Combine(root, "uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> Upload(int proposalId, string category, IFormFile file, string returnAction)
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            var back = role == "Supervisor"
                ? RedirectToAction("Review", "Supervisor", new { id = proposalId })
                : RedirectToAction("Details", "Student", new { id = proposalId });

            if (!CanAccess(proposalId, userId.Value, role)) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a file.";
                return back;
            }

            if (file.Length > MaxBytes)
            {
                TempData["Error"] = "File is too large. Maximum size is 20 MB.";
                return back;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Allowed.Contains(ext))
            {
                TempData["Error"] = "Only PDF, DOC, DOCX, PPTX and ZIP files are allowed.";
                return back;
            }

            var storedName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = Path.Combine(UploadFolder(), storedName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _context.ResearchDocuments.Add(new ResearchDocument
            {
                FileName = Path.GetFileName(file.FileName),
                StoredName = storedName,
                SizeBytes = file.Length,
                Category = string.IsNullOrWhiteSpace(category) ? "Other" : category,
                UploadedAt = DateTime.Now,
                ProposalId = proposalId,
                UploadedById = userId.Value
            });
            _context.SaveChanges();

            TempData["Success"] = "File uploaded.";
            return back;
        }

        // GET: /Document/Download/5
        public IActionResult Download(int id)
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            var doc = _context.ResearchDocuments.FirstOrDefault(d => d.Id == id);
            if (doc == null) return NotFound();

            if (!CanAccess(doc.ProposalId, userId.Value, role)) return NotFound();

            var fullPath = Path.Combine(UploadFolder(), doc.StoredName);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return File(bytes, "application/octet-stream", doc.FileName);
        }

        // POST: /Document/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            var doc = _context.ResearchDocuments.FirstOrDefault(d => d.Id == id);
            if (doc == null) return NotFound();

            // only the uploader (or admin) may delete
            if (role != "Admin" && doc.UploadedById != userId.Value) return NotFound();

            var fullPath = Path.Combine(UploadFolder(), doc.StoredName);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

            var proposalId = doc.ProposalId;
            _context.ResearchDocuments.Remove(doc);
            _context.SaveChanges();

            TempData["Success"] = "File deleted.";

            return role == "Supervisor"
                ? RedirectToAction("Review", "Supervisor", new { id = proposalId })
                : RedirectToAction("Details", "Student", new { id = proposalId });
        }
    }
}
