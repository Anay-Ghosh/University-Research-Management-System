using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class ResearchProposal
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Abstract { get; set; } = null!;

        public string? Keywords { get; set; }

        // "Pending" | "Approved" | "Rejected" | "Ongoing" | "Completed"
        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public string? GitHubRepoUrl { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        public int? SupervisorId { get; set; }
        public User? Supervisor { get; set; }

        public string? Feedback { get; set; }

        // Deadline set by the supervisor for the student to respond to the review
        public DateTime? ReviewDeadline { get; set; }
        public string? DeadlineTask { get; set; }
        public bool DeadlineResolved { get; set; } = false;

        public List<ProgressUpdate>? ProgressUpdates { get; set; }
    }
}
