namespace petcomm.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int? PetId { get; set; }
        public int ViewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    }
}