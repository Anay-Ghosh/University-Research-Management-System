using System.ComponentModel.DataAnnotations;

namespace LifeNetAssist.MVC.Models
{
    public class SupervisorProfile
    {
        public int Id { get; set; }

        public string? ResearchAreas { get; set; }

        public string? Designation { get; set; }

        public string? OfficeRoom { get; set; }

        public int MaxStudents { get; set; } = 5;

        public bool IsAvailable { get; set; } = true;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
