using Microsoft.AspNetCore.Mvc;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
namespace NTN_STORE.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
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
                .Include(c => c.Product).ThenInclude(p => p.Images)
                .Include(c => c.Variant)
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

                    // === [THÊM ĐOẠN NÀY] ===
                    // Truyền dữ liệu xuống View để JavaScript tính toán khi click chọn/bỏ chọn
                    ViewBag.CouponPercent = coupon.DiscountPercent;
                    ViewBag.CouponAmount = coupon.DiscountAmount;
                    // =======================

                    // Tính tiền giảm (Server side - để hiển thị ban đầu)
                    if (coupon.DiscountPercent > 0)
                    {
                        vm.DiscountAmount = vm.SubTotal * coupon.DiscountPercent / 100;
                    }
                    else
                    {
                        vm.DiscountAmount = coupon.DiscountAmount;
                    }

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
        [HttpPost]
        public async Task<IActionResult> ApplyCouponAjax(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá!" });

            // Tìm mã trong DB
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

            // Validate các trường hợp lỗi
            if (coupon == null)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });

            if (coupon.ExpiryDate < DateTime.Now)
                return Json(new { success = false, message = "Mã này đã hết hạn!" });

            if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
                return Json(new { success = false, message = "Mã này đã hết lượt sử dụng!" });

            // Thành công -> Lưu vào Session
            HttpContext.Session.SetString("CouponCode", coupon.Code);

            return Json(new { success = true, message = "Áp dụng mã giảm giá thành công!" });
        }

        // Thêm Action RemoveCoupon
        [HttpPost]
        public IActionResult RemoveCouponAjax()
        {
            HttpContext.Session.Remove("CouponCode");
            return Json(new { success = true, message = "Đã gỡ mã giảm giá." });
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
