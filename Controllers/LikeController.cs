using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;

namespace petcomm.Controllers
{
    public class LikeController : Controller
    {
        private readonly AppDbContext _context;
        public LikeController(AppDbContext context) { _context = context; }

        [HttpPost]
        public async Task<IActionResult> Toggle(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            var existing = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            bool liked;
            if (existing != null)
            {
                _context.Likes.Remove(existing);
                liked = false;
            }
            else
            {
                _context.Likes.Add(new Like { PostId = postId, UserId = userId.Value });
                liked = true;
            }

            await _context.SaveChangesAsync();
            var count = await _context.Likes.CountAsync(l => l.PostId == postId);
            return Json(new { success = true, liked, count });
        }
    }
}