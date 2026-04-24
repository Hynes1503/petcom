namespace petcomm.Models
{
    public class PostReport
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public Post? Post { get; set; }
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }

        // Lý do report: Spam, Nội dung không phù hợp, Lừa đảo, Thông tin sai, Khác
        public string? Reason { get; set; }
        public string? Description { get; set; }

        // Pending / Resolved / Dismissed
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Admin xử lý
        public int? ReviewedByAdminId { get; set; }
        public User? ReviewedByAdmin { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
    }
}