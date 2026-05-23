using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System.Security.Claims;

namespace petcomm.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("user/details/{username?}")]
        public async Task<IActionResult> Details(string? username = null)
        {
            var currentUsername = User.Identity?.Name;
            var isOwnProfile = string.IsNullOrEmpty(username) || username == currentUsername;

            var targetUsername = isOwnProfile ? currentUsername : username;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (user == null) return NotFound();

            var posts = await _context.Posts
                .Include(p => p.Images)
                .Where(p => p.UserId == user.Id && p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            var postIds = posts.Select(p => p.Id).ToList();
            var commentCounts = await _context.Comments
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            var currentUserId = GetCurrentUserId();
            var bookmarkedByUser = new List<int>();
            if (currentUserId.HasValue)
            {
                bookmarkedByUser = await _context.Bookmarks
                    .Where(b => b.UserId == currentUserId.Value && postIds.Contains(b.PostId))
                    .Select(b => b.PostId)
                    .ToListAsync();
            }

            var vm = new UserProfileViewModel
            {
                User = user,
                IsOwnProfile = isOwnProfile,
                TotalPosts = await _context.Posts.CountAsync(p => p.UserId == user.Id && p.Status == "Active"),
                AdoptionPosts = await _context.Posts.CountAsync(p => p.UserId == user.Id && p.Type == "Adoption" && p.Status == "Active"),
                LostPosts = await _context.Posts.CountAsync(p => p.UserId == user.Id && p.Type == "Thất lạc" && p.Status == "Active"),
                SharePosts = await _context.Posts.CountAsync(p => p.UserId == user.Id && p.Type == "Chia sẻ" && p.Status == "Active"),
                RecentPosts = posts,
                BookmarkedPosts = new List<Post>()
            };

            ViewBag.CommentCounts = commentCounts;
            ViewBag.BookmarkedByUser = bookmarkedByUser;
            ViewData["Title"] = $"{user.Username} — Profile | PetComm";
            return View("Profile", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Posts(int userId, int page = 1, int pageSize = 10)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var posts = await _context.Posts
                .Include(p => p.Images)
                .Where(p => p.UserId == userId && p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPosts = await _context.Posts.CountAsync(p => p.UserId == userId && p.Status == "Active");

            ViewBag.User = user;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            return View(posts);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return HttpContext.Session.GetInt32("UserId");
        }
    }
}