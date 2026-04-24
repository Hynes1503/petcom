namespace petcomm.Models
{
    public class UserProfileViewModel
    {
        public User User { get; set; } = null!;
        public int TotalPosts { get; set; }
        public int AdoptionPosts { get; set; }
        public int LostPosts { get; set; }
        public int SharePosts { get; set; }
        public List<Post> RecentPosts { get; set; } = new List<Post>();
        public List<Post> BookmarkedPosts { get; set; } = new List<Post>();
        public bool IsOwnProfile { get; set; }
    }
}