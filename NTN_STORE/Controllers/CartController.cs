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
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
        public async Task<IActionResult> AddToCart(int productId, int variantId, int quantity = 1)
        {
            var userId = GetUserId();

            // Kiểm tra xem sản phẩm + biến thể này đã có trong giỏ hàng của user chưa
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.VariantId == variantId);

            if (cartItem != null)
            {
                // Đã có, tăng số lượng
                cartItem.Quantity += quantity;
            }
            else
            {
                // Chưa có, tạo mới
                cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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