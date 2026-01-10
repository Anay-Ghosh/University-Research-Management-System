namespace LifeNetAssist.MVC.Models
{
    // For each request and its assigned volunteer + optional distance
    public class RequestWithVolunteerViewModel
    {
        public HelpRequest Request { get; set; } = null!;
        public User? AssignedVolunteer { get; set; }
        public double? DistanceKm { get; set; }
    }
}
