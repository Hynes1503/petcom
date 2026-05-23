using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace petcomm.Controllers
{
    public class BookmarkController : Controller
    {
        private readonly AppDbContext _context;

        public BookmarkController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        [HttpGet]
        public async Task<IActionResult> MyBookmarks()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem bài viết đã lưu.";
                return RedirectToAction("Index", "Home");
            }

            var bookmarks = await _context.Bookmarks
                .Include(b => b.Post)
                    .ThenInclude(p => p.Images)
                .Include(b => b.Post.User)
                .Where(b => b.UserId == userId.Value)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var postIds = bookmarks.Select(b => b.PostId).ToList();
            var commentCounts = new Dictionary<int, int>();

            if (postIds.Any())
            {
                commentCounts = await _context.Comments
                    .Where(c => postIds.Contains(c.PostId))
                    .GroupBy(c => c.PostId)
                    .Select(g => new { PostId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.PostId, x => x.Count);
            }

            ViewBag.CommentCounts = commentCounts;
            ViewBag.BookmarkCount = bookmarks.Count;

            return View(bookmarks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var existing = await _context.Bookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.PostId == postId);

            if (existing != null)
            {
                _context.Bookmarks.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, saved = false });
            }
            else
            {
                var bookmark = new Bookmark
                {
                    UserId = userId.Value,
                    PostId = postId,
                    CreatedAt = DateTime.Now
                };
                _context.Bookmarks.Add(bookmark);
                await _context.SaveChangesAsync();
                return Json(new { success = true, saved = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int bookmarkId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var bookmark = await _context.Bookmarks
                .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId.Value);

            if (bookmark == null)
                return Json(new { success = false, message = "Không tìm thấy bookmark." });

            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa khỏi danh sách lưu." });
        }
    }
}