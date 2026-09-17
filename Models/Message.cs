using System;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public int SenderId { get; set; }
        public User? Sender { get; set; }

        public int ReceiverId { get; set; }
        public User? Receiver { get; set; }

        // Optional: message tied to a specific research
        public int? ProposalId { get; set; }
        public ResearchProposal? Proposal { get; set; }
    }
}
