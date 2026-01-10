using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class VolunteerProfile
    {
        public int Id { get; set; }

        [Required]
        public string Address { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        // Coordinates of volunteer's base location
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Required]
        public string Phone { get; set; } = null!;

        public string? TaskTypes { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
