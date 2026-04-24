using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace petcomm.Models
{
    public class CommentReply
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; } = false;

        // Navigation properties
        [ForeignKey("CommentId")]
        public virtual Comment? Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}