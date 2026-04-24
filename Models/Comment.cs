using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace petcomm.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string? Content { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int PostId { get; set; }
        public Post? Post { get; set; }

        public DateTime CreatedAt { get; set; }

        // Thêm collection cho replies
        public virtual ICollection<CommentReply> Replies { get; set; } = new List<CommentReply>();
    }
}