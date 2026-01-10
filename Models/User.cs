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

        // "Admin" | "Requester" | "Volunteer"
        [Required]
        public string Role { get; set; } = null!;

        // Navigation property for requests created by requester
        public List<HelpRequest>? Requests { get; set; }

        // One-to-one for VolunteerProfile
        public VolunteerProfile? VolunteerProfile { get; set; }
    }
}
