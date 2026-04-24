using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace petcomm.Models
{
    public class AdoptionRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Experience { get; set; }

        [MaxLength(50)]
        public string? HousingType { get; set; }

        public bool HasOtherPets { get; set; } = false;

        [MaxLength(200)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        [ForeignKey("RequesterId")]
        public virtual User? Requester { get; set; }

        [ForeignKey("OwnerId")]
        public virtual User? Owner { get; set; }
    }
}