using petcomm.Models;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalPosts { get; set; }
    public int ActivePosts { get; set; }
    public List<User> RecentUsers { get; set; } = new();
    public List<Post> RecentPosts { get; set; } = new();
}