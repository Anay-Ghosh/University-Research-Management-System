using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        // "Admin" | "Student" | "Supervisor"
        [Required]
        public string Role { get; set; } = null!;

        public string? Department { get; set; }
        public string? UniversityId { get; set; }

        public string? GitHubUsername { get; set; }

        public List<ResearchProposal>? Proposals { get; set; }

        public SupervisorProfile? SupervisorProfile { get; set; }
    }
}
