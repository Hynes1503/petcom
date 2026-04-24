using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System.Security.Claims;

namespace petcomm.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id)) return id;
            return HttpContext.Session.GetInt32("UserId");
        }

        // POST: /Report/SubmitPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPost(int postId, string reason, string? description)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return Json(new { success = false, message = "Bài đăng không tồn tại." });

            if (post.UserId == userId.Value)
                return Json(new { success = false, message = "Bạn không thể báo cáo bài đăng của chính mình." });

            var already = await _context.PostReports
                .AnyAsync(r => r.PostId == postId && r.ReporterId == userId.Value && r.Status == "Pending");
            if (already)
                return Json(new { success = false, message = "Bạn đã báo cáo bài đăng này rồi." });

            _context.PostReports.Add(new PostReport
            {
                PostId = postId,
                ReporterId = userId.Value,
                Reason = reason,
                Description = description?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã gửi báo cáo. Chúng tôi sẽ xem xét sớm nhất có thể." });
        }

        // POST: /Report/SubmitComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComment(int commentId, string reason, string? description)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return Json(new { success = false, message = "Bình luận không tồn tại." });

            if (comment.UserId == userId.Value)
                return Json(new { success = false, message = "Bạn không thể báo cáo bình luận của chính mình." });

            var already = await _context.CommentReports
                .AnyAsync(r => r.CommentId == commentId && r.ReporterId == userId.Value && r.Status == "Pending");
            if (already)
                return Json(new { success = false, message = "Bạn đã báo cáo bình luận này rồi." });

            _context.CommentReports.Add(new CommentReport
            {
                CommentId = commentId,
                ReporterId = userId.Value,
                Reason = reason,
                Description = description?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã gửi báo cáo bình luận." });
        }
    }
}
