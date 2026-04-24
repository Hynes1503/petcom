using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System.Security.Claims;

namespace petcomm.Controllers
{
    public class UserPostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserPostController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── Helper: lấy UserId từ session, trả null nếu chưa đăng nhập ──
        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        // ── Kiểm tra bài viết thuộc về user hiện tại ──
        private async Task<Post?> GetOwnPostAsync(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;

            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId.Value);
        }

        // ─────────────────────────────────────────────────────────────────
        // DETAILS - Trang chi tiết bài viết
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Active");

            if (post == null)
            {
                TempData["ErrorMessage"] = "Bài viết không tồn tại hoặc đã bị xóa.";
                return RedirectToAction("Index", "Home");
            }

            // Tăng lượt xem
            post.ViewCount += 1;
            await _context.SaveChangesAsync();

            // Kiểm tra bài viết đã được lưu bởi user hiện tại chưa
            var currentUserId = GetCurrentUserId();
            bool isBookmarked = false;
            bool isOwnPost = false;
            int requestCount = 0;

            if (currentUserId.HasValue)
            {
                isBookmarked = await _context.Bookmarks
                    .AnyAsync(b => b.UserId == currentUserId.Value && b.PostId == post.Id);
                isOwnPost = post.UserId == currentUserId.Value;

                // Lấy số lượng yêu cầu đang chờ xử lý (cho chủ bài viết)
                if (isOwnPost && post.Type == "Adoption")
                {
                    requestCount = await _context.AdoptionRequests
                        .CountAsync(r => r.PostId == post.Id && r.Status == "pending");
                }
            }

            // Lấy các bài viết liên quan
            var relatedPosts = await _context.Posts
                .Include(p => p.Images)
                .Where(p => p.Status == "Active" && p.Id != post.Id &&
                       (p.Type == post.Type || p.UserId == post.UserId))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.IsBookmarked = isBookmarked;
            ViewBag.IsOwnPost = isOwnPost;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.RelatedPosts = relatedPosts;
            ViewBag.RequestCount = requestCount; // Thêm dòng này

            return View(post);
        }

        // ─────────────────────────────────────────────────────────────────
        // GET EDIT DATA - Lấy dữ liệu bài viết để edit (cho modal)
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetEditData(int id)
        {
            var post = await GetOwnPostAsync(id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy hoặc bạn không có quyền." });

            var data = new
            {
                success = true,
                id = post.Id,
                title = post.Title,
                content = post.Content,
                type = post.Type,
                images = post.Images.Select(i => new {
                    i.Id,
                    path = i.ImagePath
                }).ToList()
            };

            return Json(data);
        }

        // ─────────────────────────────────────────────────────────────────
        // EDIT (POST)  POST /UserPost/Edit
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] int id, [FromForm] string title,
            [FromForm] string content, [FromForm] string type,
            List<IFormFile> images, [FromForm] List<int> deleteImageIds)
        {
            var existing = await GetOwnPostAsync(id);
            if (existing == null)
                return Json(new { success = false, message = "Không tìm thấy hoặc bạn không có quyền." });

            // Cập nhật các trường được phép
            existing.Title = title;
            existing.Content = content;
            existing.Type = type;

            // Xóa ảnh được chọn xóa
            if (deleteImageIds != null && deleteImageIds.Any())
            {
                var toDelete = existing.Images
                    .Where(i => deleteImageIds.Contains(i.Id))
                    .ToList();

                foreach (var img in toDelete)
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.ImagePath!.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                _context.PostImages.RemoveRange(toDelete);
            }

            await _context.SaveChangesAsync();

            // Thêm ảnh mới
            await SaveImages(id, images);

            return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE  POST /UserPost/Create
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, List<IFormFile> images)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            post.UserId = userId.Value;
            post.CreatedAt = DateTime.Now;
            post.Status = "Active";
            post.ViewCount = 0;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await SaveImages(post.Id, images);

            // Trả về JSON để modal đóng và reload trang
            return Json(new { success = true, postId = post.Id });
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE  POST /UserPost/Delete/{id}
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await GetOwnPostAsync(id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy hoặc bạn không có quyền." });

            // Xóa file ảnh vật lý
            foreach (var img in post.Images)
            {
                var filePath = Path.Combine(_env.WebRootPath, img.ImagePath!.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER: lưu ảnh
        // ─────────────────────────────────────────────────────────────────
        private async Task SaveImages(int postId, List<IFormFile> images)
        {
            if (images == null || !images.Any()) return;

            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadPath);

            foreach (var file in images)
            {
                if (file.Length == 0) continue;

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.PostImages.Add(new PostImage
                {
                    PostId = postId,
                    ImagePath = $"/uploads/posts/{fileName}",
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}