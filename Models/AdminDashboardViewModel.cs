namespace LifeNetAssist.MVC.Models
{
    // For showing main dashboard stats
    public class AdminDashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int AssignedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int TotalVolunteers { get; set; }

        // Optional: Closest volunteer info for pending requests
        public List<ClosestVolunteerViewModel> ClosestVolunteers { get; set; } = new List<ClosestVolunteerViewModel>();
    }

    public class ClosestVolunteerViewModel
    {
        public string VolunteerName { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }
}
