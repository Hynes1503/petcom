using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace petcomm.Controllers
{
    public class CommentReplyController : Controller
    {
        private readonly AppDbContext _context;

        public CommentReplyController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        // POST: /CommentReply/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int commentId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để phản hồi." });

            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Vui lòng nhập nội dung phản hồi." });

            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
                return Json(new { success = false, message = "Không tìm thấy bình luận." });

            var reply = new CommentReply
            {
                CommentId = commentId,
                UserId = userId.Value,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.CommentReplies.Add(reply);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId.Value);

            return Json(new
            {
                success = true,
                reply = new
                {
                    id = reply.Id,
                    content = reply.Content,
                    createdAt = reply.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    username = user?.Username ?? "Người dùng",
                    userAvatar = user?.AvatarPath,
                    userId = userId.Value
                }
            });
        }

        // POST: /CommentReply/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int replyId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var reply = await _context.CommentReplies
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null)
                return Json(new { success = false, message = "Không tìm thấy phản hồi." });

            if (reply.UserId != userId)
                return Json(new { success = false, message = "Bạn không có quyền sửa phản hồi này." });

            reply.Content = content;
            reply.UpdatedAt = DateTime.Now;
            reply.IsEdited = true;

            await _context.SaveChangesAsync();

            return Json(new { success = true, content = content, isEdited = true });
        }

        // POST: /CommentReply/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int replyId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var reply = await _context.CommentReplies
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null)
                return Json(new { success = false, message = "Không tìm thấy phản hồi." });

            if (reply.UserId != userId)
                return Json(new { success = false, message = "Bạn không có quyền xóa phản hồi này." });

            _context.CommentReplies.Remove(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa phản hồi." });
        }

        // GET: /CommentReply/List
        [HttpGet]
        public async Task<IActionResult> List(int commentId)
        {
            var replies = await _context.CommentReplies
                .Include(r => r.User)
                .Where(r => r.CommentId == commentId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    content = r.Content,
                    createdAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    updatedAt = r.UpdatedAt.HasValue ? r.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm") : null,
                    isEdited = r.IsEdited,
                    username = r.User.Username,
                    userAvatar = r.User.AvatarPath,
                    userId = r.UserId
                })
                .ToListAsync();

            return Json(replies);
        }
    }
}