namespace petcomm.Models
{
    public class PostImage
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }

        public Post? Post { get; set; }
    }
}