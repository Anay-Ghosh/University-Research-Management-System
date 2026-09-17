using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace LifeNetAssist.MVC.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;

        public NotificationController(ApplicationDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId");

        // GET: /Notification/Index
        public IActionResult Index()
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var all = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(all);
        }

        // GET: /Notification/Open/5  -> marks read, then redirects to its link
        public IActionResult Open(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var n = _context.Notifications.FirstOrDefault(x => x.Id == id && x.UserId == userId);
            if (n == null) return NotFound();

            if (!n.IsRead)
            {
                n.IsRead = true;
                _context.SaveChanges();
            }

            if (!string.IsNullOrWhiteSpace(n.Link) && n.Link.StartsWith("/"))
                return Redirect(n.Link);

            return RedirectToAction("Index");
        }

        // POST: /Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllRead()
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "User");

            var unread = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToList();

            foreach (var n in unread) n.IsRead = true;
            if (unread.Any()) _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
