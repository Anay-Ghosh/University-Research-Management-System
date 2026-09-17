using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class Publication
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Authors { get; set; } = null!;

        public string? Venue { get; set; }

        // "Journal" | "Conference" | "Thesis" | "Book Chapter"
        public string? Type { get; set; }

        public int? Year { get; set; }

        public string? DOI { get; set; }

        public string? Url { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Supervisor verification
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifierRemarks { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        public int? ProposalId { get; set; }
        public ResearchProposal? Proposal { get; set; }
    }
}
