using Microsoft.AspNetCore.Mvc;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        // 1. Action GET (Khi bấm nút MUA HÀNG từ Giỏ)
        [HttpGet]
        public async Task<IActionResult> Index(List<int> selectedIds)
        {
            var userId = _userManager.GetUserId(User);

            // Nếu không có ID nào được chọn (trường hợp truy cập trực tiếp link), thử lấy từ Session cũ
            if (selectedIds == null || !selectedIds.Any())
            {
                var savedIds = HttpContext.Session.GetString("CheckoutItems");
                if (!string.IsNullOrEmpty(savedIds))
                {
                    selectedIds = savedIds.Split(',').Select(int.Parse).ToList();
                }
                else
                {
                    // Nếu vẫn không có -> Đá về giỏ hàng
                    TempData["Error"] = "Vui lòng chọn sản phẩm để thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Lưu danh sách ID vào Session để dùng cho bước POST sau này
            HttpContext.Session.SetString("CheckoutItems", string.Join(",", selectedIds));

            // Lấy Cart Items từ DB và LỌC theo selectedIds
            var cartItems = await _context.CartItems
                .Include(c => c.Product).ThenInclude(p => p.Images)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId && selectedIds.Contains(c.Id)) // QUAN TRỌNG: Chỉ lấy item được chọn
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Tính toán tiền nong
            var cartVM = new CartViewModel { CartItems = cartItems };

            // Logic Coupon (Áp dụng cho Subtotal của các món ĐÃ CHỌN)
            var couponCode = HttpContext.Session.GetString("CouponCode");
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);
                if (coupon != null && coupon.ExpiryDate >= DateTime.Now && (coupon.UsageLimit == 0 || coupon.UsedCount < coupon.UsageLimit))
                {
                    cartVM.AppliedCoupon = coupon.Code;
                    if (coupon.DiscountPercent > 0)
                        cartVM.DiscountAmount = cartVM.SubTotal * coupon.DiscountPercent / 100;
                    else
                        cartVM.DiscountAmount = coupon.DiscountAmount;

                    if (cartVM.DiscountAmount > cartVM.SubTotal) cartVM.DiscountAmount = cartVM.SubTotal;
                }
            }

            var checkoutVM = new CheckoutViewModel
            {
                Cart = cartVM,
                ShippingDetails = new Order()
            };

            // Điền sẵn thông tin user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null) checkoutVM.ShippingDetails.Email = currentUser.Email;

            return View(checkoutVM);
        }

        // 2. Action POST (Khi bấm ĐẶT HÀNG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            // Lấy lại danh sách ID từ Session
            var sessionIds = HttpContext.Session.GetString("CheckoutItems");
            if (string.IsNullOrEmpty(sessionIds)) return RedirectToAction("Index", "Cart");

            var selectedIds = sessionIds.Split(',').Select(int.Parse).ToList();

            // Lấy lại giỏ hàng và LỌC
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && selectedIds.Contains(c.Id)) // QUAN TRỌNG
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Tính toán lại (Logic y hệt phần GET)
            decimal subTotal = cartItems.Sum(x => x.Product.Price * x.Quantity);
            decimal shippingFee = (subTotal > 1500000 || model.ShippingDetails.Address.Contains("Hà Nội")) ? 0 : 30000;

            decimal discountAmount = 0;
            string appliedCouponCode = null;
            Coupon coupon = null;

            var sessionCoupon = HttpContext.Session.GetString("CouponCode");
            if (!string.IsNullOrEmpty(sessionCoupon))
            {
                coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == sessionCoupon && c.IsActive);
                if (coupon != null && coupon.ExpiryDate >= DateTime.Now && (coupon.UsageLimit == 0 || coupon.UsedCount < coupon.UsageLimit))
                {
                    appliedCouponCode = coupon.Code;
                    if (coupon.DiscountPercent > 0) discountAmount = subTotal * coupon.DiscountPercent / 100;
                    else discountAmount = coupon.DiscountAmount;
                    if (discountAmount > subTotal) discountAmount = subTotal;
                }
            }

            decimal totalAmount = subTotal + shippingFee - discountAmount;
            if (totalAmount < 0) totalAmount = 0;

            // Bỏ qua validate
            ModelState.Remove("Cart");
            ModelState.Remove("ShippingDetails.UserId");
            ModelState.Remove("ShippingDetails.OrderCode");
            ModelState.Remove("ShippingDetails.TotalAmount");
            ModelState.Remove("ShippingDetails.Status");
            ModelState.Remove("ShippingDetails.CreatedAt");
            ModelState.Remove("ShippingDetails.OrderDetails");
            ModelState.Remove("ShippingDetails.User");

            if (ModelState.IsValid)
            {
                // Tạo đơn hàng
                var order = model.ShippingDetails;
                order.UserId = userId;
                order.CreatedAt = DateTime.Now;
                order.Status = "Pending";
                order.OrderCode = $"NTN-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
                order.TotalAmount = totalAmount;
                order.CouponCode = appliedCouponCode;
                order.DiscountValue = discountAmount;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Lưu chi tiết
                foreach (var item in cartItems)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    });
                }

                // Trừ lượt dùng Coupon
                if (coupon != null)
                {
                    coupon.UsedCount++;
                    _context.Coupons.Update(coupon);
                }

                // Xóa các món ĐÃ CHỌN khỏi giỏ (Món chưa chọn giữ nguyên)
                _context.CartItems.RemoveRange(cartItems);

                // Clear Session
                HttpContext.Session.Remove("CheckoutItems");
                HttpContext.Session.Remove("CouponCode");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(OrderCompleted), new { id = order.Id });
            }

            // Nếu lỗi, trả về View với dữ liệu tính toán
            model.Cart = new CartViewModel
            {
                CartItems = cartItems,
                DiscountAmount = discountAmount,
                AppliedCoupon = appliedCouponCode
            };
            return View(model);
        }


        // GET: /Checkout/OrderCompleted/5
        public IActionResult OrderCompleted(int id)
        {
            // Lấy mã đơn hàng để hiển thị
            var order = _context.Orders.Find(id);
            if (order == null || order.UserId != GetUserId())
            {
                return NotFound();
            }

            ViewBag.OrderCode = order.OrderCode;
            return View();
        }
    }
}
