using petcomm.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }

    public string? AvatarPath { get; set; }
    public string? Bio { get; set; }
    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}