using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeNetAssist.MVC.Models
{
    public class HelpRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestType { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Required]
        public string RequesterName { get; set; } = null!;

        [Required]
        public string Contact { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Assigned, Completed

        // Only store the volunteer ID, no navigation property
        public int? AssignedVolunteerId { get; set; }

        // Foreign key to the requester (User)
        public int RequesterId { get; set; }
        public User? Requester { get; set; } = null!;
    }
}
