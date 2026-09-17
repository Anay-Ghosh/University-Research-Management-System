using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class ProgressUpdate
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        // 0 - 100
        public int PercentComplete { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CommitHash { get; set; }
        public string? CommitMessage { get; set; }

        public int ProposalId { get; set; }
        public ResearchProposal? Proposal { get; set; }
    }
}
