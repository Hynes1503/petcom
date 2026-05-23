using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace petcomm.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["LoginError"] = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                TempData["LoginError"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return RedirectToAction("Index", "Home");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Member"),
                new Claim("Avatar", user.AvatarPath ?? ""),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Đồng bộ Session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserRole", user.Role ?? "Member");
            HttpContext.Session.SetString("UserAvatar", user.AvatarPath ?? "");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["RegisterError"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToAction("Index", "Home");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                TempData["RegisterError"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (existingEmail != null)
            {
                TempData["RegisterError"] = "Email đã được sử dụng.";
                return RedirectToAction("Index", "Home");
            }

            string hashedPassword = HashPassword(password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hashedPassword,
                Role = "Member",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Member")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserRole", user.Role ?? "Member");

            TempData["SuccessMessage"] = "Đăng ký thành công!";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [Route("account/profile")]
        public async Task<IActionResult> Profile(string? username = null)
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
            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProfileData()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            return Json(new
            {
                success = true,
                username = user.Username,
                email = user.Email,
                bio = user.Bio,
                avatarPath = user.AvatarPath,
                phoneNumber = user.PhoneNumber,
                fullName = user.FullName
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] string username, [FromForm] string email,
            [FromForm] string bio, [FromForm] string phoneNumber, [FromForm] string fullName,
            IFormFile avatar)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            if (username != user.Username)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Id != userId.Value);
                if (existingUser != null)
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });
            }

            if (email != user.Email)
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.Id != userId.Value);
                if (existingEmail != null)
                    return Json(new { success = false, message = "Email đã được sử dụng." });
            }

            user.Username = username;
            user.Email = email;
            user.Bio = bio;
            user.PhoneNumber = phoneNumber;
            user.FullName = fullName;

            if (avatar != null && avatar.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Định dạng ảnh không hợp lệ." });

                if (avatar.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB." });

                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, user.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.AvatarPath = $"/uploads/avatars/{fileName}";
            }

            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Member"),
                new Claim("Avatar", user.AvatarPath ?? ""),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserAvatar", user.AvatarPath ?? "");

            return Json(new { success = true, message = "Cập nhật hồ sơ thành công!" });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return HttpContext.Session.GetInt32("UserId");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
    }
}