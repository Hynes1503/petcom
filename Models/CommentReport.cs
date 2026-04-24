namespace petcomm.Models
{
    public class CommentReport
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public Comment? Comment { get; set; }
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? ReviewedByAdminId { get; set; }
        public User? ReviewedByAdmin { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
    }
}
