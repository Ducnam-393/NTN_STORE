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

            var vm = new CartViewModel
            {
                CartItems = cartItemsFromDb
            };
            // -- LOGIC TÍNH GIẢM GIÁ --
            var couponCode = HttpContext.Session.GetString("CouponCode");
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
                if (coupon != null)
                {
                    vm.AppliedCoupon = coupon.Code;

                    // Tính tiền giảm
                    if (coupon.DiscountPercent > 0)
                    {
                        vm.DiscountAmount = vm.SubTotal * coupon.DiscountPercent / 100;
                    }
                    else
                    {
                        vm.DiscountAmount = coupon.DiscountAmount;
                    }

                    // Đảm bảo không giảm quá giá trị đơn hàng
                    if (vm.DiscountAmount > vm.SubTotal) vm.DiscountAmount = vm.SubTotal;
                }
            }
            return View(vm);
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
        // Thêm Action ApplyCoupon
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
            {
                TempData["CouponError"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("Index");
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

            if (coupon == null)
            {
                TempData["CouponError"] = "Mã giảm giá không tồn tại.";
                return RedirectToAction("Index");
            }

            if (coupon.ExpiryDate < DateTime.Now)
            {
                TempData["CouponError"] = "Mã giảm giá đã hết hạn.";
                return RedirectToAction("Index");
            }

            if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
            {
                TempData["CouponError"] = "Mã giảm giá đã hết lượt sử dụng.";
                return RedirectToAction("Index");
            }

            // Mã hợp lệ -> Lưu vào Session
            HttpContext.Session.SetString("CouponCode", coupon.Code);
            TempData["CouponSuccess"] = "Áp dụng mã giảm giá thành công!";

            return RedirectToAction("Index");
        }

        // Thêm Action RemoveCoupon
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("CouponCode");
            TempData["CouponSuccess"] = "Đã hủy mã giảm giá.";
            return RedirectToAction("Index");
        }
        // API: Lấy nội dung Mini Cart (trả về PartialView HTML)
        [HttpGet]
        public async Task<IActionResult> GetMiniCart()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = new List<CartItem>();

            if (userId != null)
            {
                cartItems = await _context.CartItems
                    .Include(c => c.Product).ThenInclude(p => p.Images)
                    .Include(c => c.Variant)
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();
            }

            var vm = new CartViewModel { CartItems = cartItems };
            return PartialView("_MiniCart", vm);
        }

        // API: Xóa nhanh từ Mini Cart (trả về JSON)
        [HttpPost]
        public async Task<IActionResult> RemoveFromMiniCart(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}