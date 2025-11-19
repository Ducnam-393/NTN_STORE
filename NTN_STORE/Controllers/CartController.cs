using Microsoft.AspNetCore.Mvc;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Dùng Include
using System.Threading.Tasks; // Dùng Task
using Microsoft.AspNetCore.Authorization; // Dùng [Authorize]
using Microsoft.AspNetCore.Identity; // Dùng UserManager

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Lấy UserId của người dùng đang đăng nhập
        private string GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        // GET: /Cart (Trang giỏ hàng)
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var cartItemsFromDb = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product).ThenInclude(p => p.Images) // Lấy thông tin Sản phẩm và Ảnh
                .Include(c => c.Variant) // Lấy thông tin Biến thể (Size, Color)
                .ToListAsync();

            var viewModel = new CartViewModel
            {
                CartItems = cartItemsFromDb
            };

            return View(viewModel);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [Authorize] // Đảm bảo user đã đăng nhập
        public async Task<IActionResult> AddToCart(int productId, int variantId, int quantity)
        {
            // 1. KIỂM TRA HỢP LỆ (VALIDATION)
            if (quantity <= 0) quantity = 1;

            // Kiểm tra xem Variant có tồn tại thực sự trong DB không?
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == variantId);

            if (!variantExists)
            {
                // Nếu variantId = 0 hoặc ID lạ -> Báo lỗi hoặc quay lại trang cũ
                TempData["Error"] = "Vui lòng chọn Size/Màu hợp lệ trước khi thêm vào giỏ!";
                return RedirectToAction("Detail", "Product", new { id = productId });
            }

            var userId = _userManager.GetUserId(User);

            // 2. XỬ LÝ THÊM VÀO GIỎ
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.VariantId == variantId);

            if (cartItem != null)
            {
                // Nếu đã có -> Cộng dồn số lượng
                cartItem.Quantity += quantity;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                // Nếu chưa có -> Tạo mới
                var newCartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    VariantId = variantId, // Đảm bảo ID này đã được kiểm tra ở bước 1
                    Quantity = quantity
                };
                _context.CartItems.Add(newCartItem);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }


        // POST: /Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId) // Xóa theo Id của CartItem
        {
            var userId = GetUserId();

            // Tìm CartItem theo Id VÀ UserId (để đảm bảo user không xóa item của người khác)
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateCart (Dùng cho nút +/-)
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                // Nếu giảm số lượng về 0 (hoặc âm), xóa luôn
                return await RemoveItem(cartItemId);
            }

            var userId = GetUserId();
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}