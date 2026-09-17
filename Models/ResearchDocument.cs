using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class ResearchDocument
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = null!;

        // Name on disk (GUID based, prevents collisions)
        [Required]
        public string StoredName { get; set; } = null!;

        public long SizeBytes { get; set; }

        // "Proposal" | "Draft" | "Final Thesis" | "Other"
        public string? Category { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int ProposalId { get; set; }
        public ResearchProposal? Proposal { get; set; }

        public int UploadedById { get; set; }
        public User? UploadedBy { get; set; }
    }
}
