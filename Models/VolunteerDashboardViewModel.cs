namespace LifeNetAssist.MVC.Models
{
    public class VolunteerDashboardViewModel
    {
        public string Location { get; set; } = "Not set";
        public int TotalAssignedRequests { get; set; }
        public int PendingRequests { get; set; }
        public int CompletedRequests { get; set; }

        // <-- NEW PROPERTY
        public bool ProfileExists { get; set; } = false;
    }
}
