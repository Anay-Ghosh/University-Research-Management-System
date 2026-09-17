using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;

        public MessageController(ApplicationDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId");
        private string? CurrentRole() => HttpContext.Session.GetString("UserRole");

        // People this user is allowed to message
        private List<User> GetContacts(int userId, string role)
        {
            if (role == "Student")
            {
                var supervisorIds = _context.ResearchProposals
                    .Where(p => p.StudentId == userId && p.SupervisorId != null)
                    .Select(p => p.SupervisorId!.Value)
                    .Distinct()
                    .ToList();

                return _context.Users.Where(u => supervisorIds.Contains(u.Id)).ToList();
            }

            if (role == "Supervisor")
            {
                var studentIds = _context.ResearchProposals
                    .Where(p => p.SupervisorId == userId)
                    .Select(p => p.StudentId)
                    .Distinct()
                    .ToList();

                return _context.Users.Where(u => studentIds.Contains(u.Id)).ToList();
            }

            return new List<User>();
        }

        // GET: /Message/Inbox
        public IActionResult Inbox()
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            var contacts = GetContacts(userId.Value, role);

            // unread count per contact
            var unread = _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.SenderId, x => x.Count);

            ViewBag.UnreadCounts = unread;

            return View(contacts);
        }

        // GET: /Message/Chat/5
        public IActionResult Chat(int id)
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            var contacts = GetContacts(userId.Value, role);
            var other = contacts.FirstOrDefault(c => c.Id == id);
            if (other == null) return NotFound();

            // mark their messages to me as read
            var unread = _context.Messages
                .Where(m => m.SenderId == id && m.ReceiverId == userId && !m.IsRead)
                .ToList();
            foreach (var m in unread) m.IsRead = true;
            if (unread.Any()) _context.SaveChanges();

            var thread = _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == id)
                         || (m.SenderId == id && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToList();

            ViewBag.Other = other;
            ViewBag.MyId = userId.Value;

            return View(thread);
        }

        // POST: /Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Send(int receiverId, string content)
        {
            var userId = CurrentUserId();
            var role = CurrentRole();
            if (userId == null || role == null) return RedirectToAction("Login", "User");

            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Chat", new { id = receiverId });

            var contacts = GetContacts(userId.Value, role);
            if (!contacts.Any(c => c.Id == receiverId)) return NotFound();

            _context.Messages.Add(new Message
            {
                SenderId = userId.Value,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            });
            _context.SaveChanges();

            var senderName = HttpContext.Session.GetString("UserName") ?? "Someone";
            _notify.Notify(receiverId,
                "New message from " + senderName + ".",
                "/Message/Chat/" + userId.Value,
                "Message");

            return RedirectToAction("Chat", new { id = receiverId });
        }
    }
}

