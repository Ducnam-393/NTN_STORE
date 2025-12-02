using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public WishlistController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.WishlistItems
                .Include(w => w.Product).ThenInclude(p => p.Images) // Load ảnh để hiển thị
                .Include(w => w.Product).ThenInclude(p => p.Brand)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Add(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Kiểm tra đã có chưa
            var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == id);
            if (!exists)
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = id });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm vào yêu thích!";
            }

            // Trả về trang cũ
            return Redirect(Request.Headers["Referer"].ToString());
        }
        [HttpPost]
        public async Task<IActionResult> AddAjax(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập!" });
            }

            // Kiểm tra đã có chưa
            var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == id);
            if (exists)
            {
                return Json(new { success = false, message = "Sản phẩm này đã có trong danh sách yêu thích!" });
            }

            // Thêm mới
            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = id });
            await _context.SaveChangesAsync();

            // Đếm lại tổng số để cập nhật Badge trên Header
            int count = await _context.WishlistItems.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = "Đã thêm vào yêu thích!", count = count });
        }
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == id);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa khỏi yêu thích.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}