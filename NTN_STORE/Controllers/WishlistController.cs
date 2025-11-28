using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới xem được
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

            var wishlist = await _context.WishlistItems
                .Include(w => w.Product)
                .ThenInclude(p => p.Images) // Load ảnh sản phẩm
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            return View(wishlist);
        }

        // Action xóa sản phẩm khỏi wishlist
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.ProductId == id && w.UserId == userId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Action thêm sản phẩm (để gọi từ ProductCard)
        public async Task<IActionResult> Add(int id)
        {
            var userId = _userManager.GetUserId(User);
            // Kiểm tra xem đã có chưa
            var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == id);

            if (!exists)
            {
                var item = new WishlistItem
                {
                    UserId = userId,
                    ProductId = id
                };
                _context.WishlistItems.Add(item);
                await _context.SaveChangesAsync();
            }

            // Quay lại trang cũ
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}