using Microsoft.EntityFrameworkCore;

namespace petcomm.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<AdoptionRequest> AdoptionRequests { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<PostImage> PostImages { get; set; }
        public DbSet<CommentReply> CommentReplies { get; set; }
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<CommentReport> CommentReports { get; set; }
    }
}