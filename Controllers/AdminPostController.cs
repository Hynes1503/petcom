using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;

namespace petcomm.Controllers
{
    public class AdminPostController : AdminBaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminPostController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _context.Users.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, List<IFormFile> images)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Home");

            post.UserId = userId.Value;
            post.CreatedAt = DateTime.Now;
            post.Status = "Active";

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await SaveImages(post.Id, images);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, List<IFormFile> images, List<int> deleteImageIds)
        {
            if (id != post.Id) return NotFound();

            var existingPost = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPost == null) return NotFound();

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;
            existingPost.Status = post.Status;

            if (deleteImageIds != null && deleteImageIds.Any())
            {
                var toDelete = await _context.PostImages
                    .Where(i => deleteImageIds.Contains(i.Id))
                    .ToListAsync();

                foreach (var img in toDelete)
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.ImagePath!.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.PostImages.RemoveRange(toDelete);
            }

            await _context.SaveChangesAsync();

            await SaveImages(existingPost.Id, images);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post != null)
            {
                foreach (var img in post.Images)
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.ImagePath!.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string newStatus)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài đăng" });

            var allowed = new[] { "Active", "Hidden", "Pending" };
            if (!allowed.Contains(newStatus))
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });

            post.Status = newStatus;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã cập nhật thành {newStatus}" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài đăng" });

            foreach (var img in post.Images)
            {
                var filePath = Path.Combine(_env.WebRootPath, img.ImagePath!.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xoá bài đăng" });
        }
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