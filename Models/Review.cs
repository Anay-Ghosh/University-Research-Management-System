using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string Feedback { get; set; } = null!;

        // Status the supervisor set with this review
        public string? Status { get; set; }

        // Deadline attached to this particular review, if any
        public DateTime? Deadline { get; set; }
        public string? Task { get; set; }
        public bool Resolved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int ProposalId { get; set; }
        public ResearchProposal? Proposal { get; set; }

        public int SupervisorId { get; set; }
        public User? Supervisor { get; set; }
    }
}
