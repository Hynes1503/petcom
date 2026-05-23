using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System.Security.Claims;

namespace petcomm.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                TotalPosts = await _context.Posts.CountAsync(),
                ActivePosts = await _context.Posts.CountAsync(p => p.Status == "Active"),
                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentPosts = await _context.Posts
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            ViewData["Title"] = "Admin Dashboard | PetComm";
            return View(vm);
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users(string? search, string? role, string? status, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.Username!.Contains(search) || u.Email!.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (status == "active")
                query = query.Where(u => u.IsActive);
            else if (status == "inactive")
                query = query.Where(u => !u.IsActive);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewData["Title"] = "Quản lý người dùng | Admin";

            return View(users);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> UserDetail(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var posts = await _context.Posts
                .Include(p => p.Images)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.UserPosts = posts;
            ViewData["Title"] = $"Chi tiết: {user.Username} | Admin";
            return View(user);
        }

        [HttpPost("users/{id}/toggle-status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == id)
                return Json(new { success = false, message = "Không thể khoá tài khoản của chính mình." });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = user.IsActive,
                message = user.IsActive ? "Đã kích hoạt tài khoản." : "Đã khoá tài khoản."
            });
        }

        [HttpPost("users/{id}/change-role")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(int id, string role)
        {
            var allowedRoles = new[] { "Member", "Admin", "Moderator" };
            if (!allowedRoles.Contains(role))
                return Json(new { success = false, message = "Vai trò không hợp lệ." });

            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == id)
                return Json(new { success = false, message = "Không thể thay đổi vai trò của chính mình." });

            user.Role = role;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã đổi vai trò thành {role}." });
        }

        [HttpPost("users/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == id)
                return Json(new { success = false, message = "Không thể xoá tài khoản của chính mình." });

            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            if (!string.IsNullOrEmpty(user.AvatarPath))
            {
                var avatarPath = Path.Combine(_env.WebRootPath, user.AvatarPath.TrimStart('/'));
                if (System.IO.File.Exists(avatarPath))
                    System.IO.File.Delete(avatarPath);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xoá người dùng." });
        }

        [HttpGet("posts")]
        public async Task<IActionResult> Posts(string? search, string? type, string? status, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Posts.Include(p => p.User).Include(p => p.Images).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title!.Contains(search) || p.Content!.Contains(search));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(p => p.Type == type);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var totalCount = await query.CountAsync();
            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewData["Title"] = "Quản lý bài đăng | Admin";

            return View(posts);
        }

        [HttpGet("posts/{id}")]
        public async Task<IActionResult> PostDetail(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            ViewData["Title"] = $"Chi tiết bài đăng #{id} | Admin";
            return View(post);
        }

        [HttpPost("posts/{id}/toggle-status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePostStatus(int id, string newStatus)
        {
            var allowedStatuses = new[] { "Active", "Hidden", "Pending" };
            if (!allowedStatuses.Contains(newStatus))
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return Json(new { success = false, message = "Không tìm thấy bài đăng." });

            post.Status = newStatus;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã đổi trạng thái thành {newStatus}.", status = newStatus });
        }

        [HttpPost("posts/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return Json(new { success = false, message = "Không tìm thấy bài đăng." });

            foreach (var img in post.Images)
            {
                if (!string.IsNullOrEmpty(img.ImagePath))
                {
                    var imgPath = Path.Combine(_env.WebRootPath, img.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imgPath))
                        System.IO.File.Delete(imgPath);
                }
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xoá bài đăng." });
        }
        [HttpGet("reports")]
        public async Task<IActionResult> Reports(string? status, int page = 1)
        {
            int pageSize = 15;
            var query = _context.PostReports
                .Include(r => r.Post).ThenInclude(p => p!.User)
                .Include(r => r.Reporter)
                .Include(r => r.ReviewedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            else
                query = query.Where(r => r.Status == "Pending");

            var totalCount = await query.CountAsync();
            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PendingCount = await _context.PostReports.CountAsync(r => r.Status == "Pending");
            ViewBag.ResolvedCount = await _context.PostReports.CountAsync(r => r.Status == "Resolved");
            ViewBag.DismissedCount = await _context.PostReports.CountAsync(r => r.Status == "Dismissed");

            ViewBag.Status = status ?? "Pending";
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewData["Title"] = "Kiểm duyệt báo cáo | Admin";
            return View(reports);
        }

        [HttpPost("reports/{id}/resolve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id, string action, string? adminNote)
        {
            var report = await _context.PostReports
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return Json(new { success = false, message = "Không tìm thấy báo cáo." });

            var adminId = GetCurrentUserId();

            if (action == "hide_post" && report.Post != null)
            {
                report.Post.Status = "Hidden";
                report.Status = "Resolved";
            }
            else if (action == "delete_post" && report.Post != null)
            {
                var images = await _context.Set<PostImage>().Where(i => i.PostId == report.PostId).ToListAsync();
                foreach (var img in images)
                {
                    if (!string.IsNullOrEmpty(img.ImagePath))
                    {
                        var imgPath = Path.Combine(_env.WebRootPath, img.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(imgPath)) System.IO.File.Delete(imgPath);
                    }
                }
                _context.Posts.Remove(report.Post);
                report.Status = "Resolved";
            }
            else if (action == "dismiss")
            {
                report.Status = "Dismissed";
            }
            else
            {
                return Json(new { success = false, message = "Hành động không hợp lệ." });
            }

            report.ReviewedByAdminId = adminId;
            report.ReviewedAt = DateTime.Now;
            report.AdminNote = adminNote?.Trim();

            if (action != "dismiss")
            {
                var otherReports = await _context.PostReports
                    .Where(r => r.PostId == report.PostId && r.Status == "Pending" && r.Id != id)
                    .ToListAsync();
                foreach (var r in otherReports)
                {
                    r.Status = "Resolved";
                    r.ReviewedByAdminId = adminId;
                    r.ReviewedAt = DateTime.Now;
                    r.AdminNote = "Tự động đóng khi xử lý báo cáo liên quan.";
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xử lý báo cáo." });
        }

        [HttpPost("reports/{id}/restore-post")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePost(int id)
        {
            var report = await _context.PostReports.Include(r => r.Post).FirstOrDefaultAsync(r => r.Id == id);
            if (report?.Post == null) return Json(new { success = false, message = "Không tìm thấy." });

            report.Post.Status = "Active";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã khôi phục bài đăng." });
        }
        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}