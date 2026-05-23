using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;

namespace petcomm.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string? type)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var query = _context.Posts
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(p => p.Type == type);

            var posts = await query.ToListAsync();
            var postIds = posts.Select(p => p.Id).ToList();

            var commentCounts = await _context.Comments
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);


            var bookmarkedByUser = userId != null
                ? await _context.Bookmarks
                    .Where(b => postIds.Contains(b.PostId) && b.UserId == userId)
                    .Select(b => b.PostId)
                    .ToListAsync()
                : new List<int>();

            ViewBag.CommentCounts = commentCounts;
            ViewBag.BookmarkedByUser = bookmarkedByUser;
            ViewBag.IsLoggedIn = userId != null;

            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}