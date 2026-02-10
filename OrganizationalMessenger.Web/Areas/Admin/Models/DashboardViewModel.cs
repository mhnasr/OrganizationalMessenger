namespace OrganizationalMessenger.Web.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalGroups { get; set; }
        public int TotalChannels { get; set; }
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int TotalCalls { get; set; }
        public int TodayCalls { get; set; }
    }
}
