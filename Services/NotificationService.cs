using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;

namespace LifeNetAssist.MVC.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Notify(int userId, string message, string? link = null, string? type = "System")
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                Link = link,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();
        }

        public int UnreadCount(int userId)
        {
            return _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
        }

        public List<Notification> Recent(int userId, int take = 8)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToList();
        }
    }
}
