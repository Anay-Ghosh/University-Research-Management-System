using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string Message { get; set; } = null!;

        // Where clicking it should take the user, e.g. "/Student/Details/3"
        public string? Link { get; set; }

        // "Feedback" | "Assignment" | "Message" | "Publication" | "System"
        public string? Type { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
