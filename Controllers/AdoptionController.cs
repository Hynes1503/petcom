using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using petcomm.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace petcomm.Controllers
{
    public class AdoptionController : Controller
    {
        private readonly AppDbContext _context;

        public AdoptionController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

        // GET: Danh sách tất cả yêu cầu nhận nuôi gửi đến tôi
        [HttpGet]
        public async Task<IActionResult> ReceivedRequests()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Index", "Home");

            // Lấy tất cả yêu cầu gửi đến user hiện tại
            var requests = await _context.AdoptionRequests
                .Include(r => r.Requester)
                .Include(r => r.Post)
                    .ThenInclude(p => p.Images)
                .Where(r => r.OwnerId == userId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Thống kê theo trạng thái
            ViewBag.PendingCount = requests.Count(r => r.Status == "pending");
            ViewBag.ApprovedCount = requests.Count(r => r.Status == "approved");
            ViewBag.RejectedCount = requests.Count(r => r.Status == "rejected");
            ViewBag.CancelledCount = requests.Count(r => r.Status == "cancelled");
            ViewBag.TotalCount = requests.Count;

            // Lấy danh sách bài viết của user để lọc
            var userPosts = await _context.Posts
                .Where(p => p.UserId == userId.Value && p.Type == "Adoption" && p.Status == "Active")
                .Select(p => new { p.Id, p.Title })
                .ToListAsync();

            ViewBag.UserPosts = userPosts;

            return View(requests);
        }

        // GET: Lọc yêu cầu theo bài viết
        [HttpGet]
        public async Task<IActionResult> FilterByPost(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Index", "Home");

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId.Value);

            if (post == null)
                return NotFound();

            var requests = await _context.AdoptionRequests
                .Include(r => r.Requester)
                .Include(r => r.Post)
                    .ThenInclude(p => p.Images)
                .Where(r => r.OwnerId == userId.Value && r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedPostId = postId;
            ViewBag.SelectedPostTitle = post.Title;

            // Thống kê
            ViewBag.PendingCount = requests.Count(r => r.Status == "pending");
            ViewBag.ApprovedCount = requests.Count(r => r.Status == "approved");
            ViewBag.RejectedCount = requests.Count(r => r.Status == "rejected");
            ViewBag.TotalCount = requests.Count;

            var userPosts = await _context.Posts
                .Where(p => p.UserId == userId.Value && p.Type == "Adoption" && p.Status == "Active")
                .Select(p => new { p.Id, p.Title })
                .ToListAsync();

            ViewBag.UserPosts = userPosts;

            return View("ReceivedRequests", requests);
        }

        // GET: Gửi yêu cầu nhận nuôi
        [HttpGet]
        public async Task<IActionResult> Request(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để gửi yêu cầu." });

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId && p.Type == "Adoption" && p.Status == "Active");

            if (post == null)
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc không phải dạng tìm nuôi." });

            if (post.UserId == userId)
                return Json(new { success = false, message = "Bạn không thể gửi yêu cầu cho bài viết của chính mình." });

            var existingRequest = await _context.AdoptionRequests
                .FirstOrDefaultAsync(r => r.PostId == postId && r.RequesterId == userId);

            if (existingRequest != null)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn đã gửi yêu cầu cho bài viết này rồi. Vui lòng chờ phản hồi."
                });
            }

            return View(post);
        }

        // POST: Gửi yêu cầu nhận nuôi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(int postId, [FromForm] string message,
            [FromForm] string phoneNumber, [FromForm] string address,
            [FromForm] string experience, [FromForm] string housingType,
            [FromForm] bool hasOtherPets, [FromForm] string reason)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.Type == "Adoption" && p.Status == "Active");

            if (post == null)
                return Json(new { success = false, message = "Bài viết không hợp lệ." });

            if (post.UserId == userId)
                return Json(new { success = false, message = "Không thể gửi yêu cầu cho bài viết của chính mình." });

            var existingRequest = await _context.AdoptionRequests
                .FirstOrDefaultAsync(r => r.PostId == postId && r.RequesterId == userId);

            if (existingRequest != null)
                return Json(new { success = false, message = "Bạn đã gửi yêu cầu trước đó." });

            var request = new AdoptionRequest
            {
                PostId = postId,
                RequesterId = userId.Value,
                OwnerId = post.UserId,
                Message = message,
                PhoneNumber = phoneNumber,
                Address = address,
                Experience = experience,
                HousingType = housingType,
                HasOtherPets = hasOtherPets,
                Reason = reason,
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            _context.AdoptionRequests.Add(request);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gửi yêu cầu thành công! Chủ bài viết sẽ liên hệ với bạn." });
        }

        // GET: Quản lý yêu cầu cho một bài viết cụ thể (giữ lại cho tương thích)
        [HttpGet]
        public async Task<IActionResult> ManageRequests(int postId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Index", "Home");

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

            if (post == null)
                return NotFound();

            var requests = await _context.AdoptionRequests
                .Include(r => r.Requester)
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Post = post;
            return View(requests);
        }

        // POST: Xác nhận nhận nuôi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int requestId, [FromForm] string adminNote)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var request = await _context.AdoptionRequests
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu." });

            if (request.OwnerId != userId)
                return Json(new { success = false, message = "Bạn không có quyền xử lý yêu cầu này." });

            if (request.Status != "pending")
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý." });

            request.Status = "approved";
            request.ProcessedAt = DateTime.Now;
            request.AdminNote = adminNote;

            var otherRequests = await _context.AdoptionRequests
                .Where(r => r.PostId == request.PostId && r.Id != requestId && r.Status == "pending")
                .ToListAsync();

            foreach (var other in otherRequests)
            {
                other.Status = "rejected";
                other.ProcessedAt = DateTime.Now;
                other.AdminNote = "Chủ bài viết đã chọn người nhận nuôi khác.";
            }

            request.Post.Status = "Adopted";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xác nhận nhận nuôi thành công!" });
        }

        // POST: Từ chối yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int requestId, [FromForm] string adminNote)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var request = await _context.AdoptionRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu." });

            if (request.OwnerId != userId)
                return Json(new { success = false, message = "Bạn không có quyền xử lý yêu cầu này." });

            if (request.Status != "pending")
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý." });

            request.Status = "rejected";
            request.ProcessedAt = DateTime.Now;
            request.AdminNote = adminNote;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã từ chối yêu cầu." });
        }

        // GET: Xem danh sách yêu cầu của tôi (người gửi)
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Index", "Home");

            var requests = await _context.AdoptionRequests
                .Include(r => r.Post)
                .ThenInclude(p => p.Images)
                .Where(r => r.RequesterId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // POST: Hủy yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var request = await _context.AdoptionRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu." });

            if (request.RequesterId != userId)
                return Json(new { success = false, message = "Bạn không có quyền hủy yêu cầu này." });

            if (request.Status != "pending")
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý, không thể hủy." });

            request.Status = "cancelled";
            request.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy yêu cầu." });
        }
    }
}