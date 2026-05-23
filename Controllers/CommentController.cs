using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace petcomm.Controllers
{
    public class CommentController : Controller
    {
        private readonly AppDbContext _context;

        public CommentController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        [HttpGet]
        public async Task<IActionResult> List(int postId)
        {
            try
            {
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.PostId == postId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        id = c.Id,
                        content = c.Content,
                        createdAt = c.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        username = c.User != null ? c.User.Username : "Người dùng",
                        userAvatar = c.User != null ? c.User.AvatarPath : null,
                        userId = c.UserId,
                        replyCount = _context.CommentReplies.Count(r => r.CommentId == c.Id)
                    })
                    .ToListAsync();

                return Json(comments);
            }
            catch (Exception ex)
            {
                return Json(new[] { new { error = ex.Message } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int postId, string content)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });

                if (string.IsNullOrWhiteSpace(content))
                    return Json(new { success = false, message = "Vui lòng nhập nội dung bình luận." });

                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                    return Json(new { success = false, message = "Bài viết không tồn tại." });

                var comment = new Comment
                {
                    PostId = postId,
                    UserId = userId.Value,
                    Content = content.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId.Value);

                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        username = user?.Username ?? "Người dùng",
                        userAvatar = user?.AvatarPath,
                        userId = userId.Value,
                        replyCount = 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}