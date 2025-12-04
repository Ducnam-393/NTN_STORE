
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using NTN_STORE.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
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
            foreach (var item in cartItems)
            {
                if (item.Quantity > item.Variant.Stock)
                {
                    TempData["Error"] = $"Sản phẩm '{item.Product.Name}' chỉ còn {item.Variant.Stock} món. Vui lòng cập nhật lại.";
                    return RedirectToAction("Index", "Cart"); // Đuổi về giỏ hàng ngay
                }
            }
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
            var savedAddresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
            var checkoutVM = new CheckoutViewModel
            {
                Cart = cartVM,
                ShippingDetails = new Order(),
                SavedAddresses = savedAddresses
            };

            var defaultAddress = savedAddresses.FirstOrDefault(x => x.IsDefault);
            if (defaultAddress != null)
            {
                checkoutVM.ShippingDetails.CustomerName = defaultAddress.FullName;
                checkoutVM.ShippingDetails.PhoneNumber = defaultAddress.PhoneNumber;
                checkoutVM.ShippingDetails.Address = $"{defaultAddress.Address}, {defaultAddress.Ward}, {defaultAddress.District}, {defaultAddress.Province}";
            }
            else
            {
                // Nếu chưa có địa chỉ thì lấy từ User Profile
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    checkoutVM.ShippingDetails.Email = currentUser.Email;
                    checkoutVM.ShippingDetails.CustomerName = currentUser.FullName; 
                }
            }

            return View(checkoutVM);
        }

        // POST: /Checkout/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model, string paymentMethod)
        {
            var userId = _userManager.GetUserId(User);

            // Lấy lại danh sách ID từ Session
            var sessionIds = HttpContext.Session.GetString("CheckoutItems");
            if (string.IsNullOrEmpty(sessionIds)) return RedirectToAction("Index", "Cart");

            var selectedIds = sessionIds.Split(',').Select(int.Parse).ToList();

            // Lấy lại giỏ hàng và LỌC
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && selectedIds.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Tính toán lại (Logic y hệt phần GET)
            decimal subTotal = cartItems.Sum(x => x.Product.Price * x.Quantity);
            decimal shippingFee = (subTotal > 500000 || (model.ShippingDetails.Address != null && model.ShippingDetails.Address.Contains("Hà Nội"))) ? 0 : 30000;

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

            // --- 1. BỎ QUA VALIDATION KHÔNG CẦN THIẾT ---
            ModelState.Remove("Cart");
            ModelState.Remove("SavedAddresses"); // Bỏ qua list này
            ModelState.Remove("ShippingDetails.UserId");
            ModelState.Remove("ShippingDetails.OrderCode");
            ModelState.Remove("ShippingDetails.TotalAmount");
            ModelState.Remove("ShippingDetails.Status");
            ModelState.Remove("ShippingDetails.CreatedAt");
            ModelState.Remove("ShippingDetails.OrderDetails");
            ModelState.Remove("ShippingDetails.User");

            // QUAN TRỌNG: Bỏ qua lỗi PaymentMethod vì ta lấy từ tham số riêng
            ModelState.Remove("ShippingDetails.PaymentMethod"); // <--- THÊM DÒNG NÀY

            // Kiểm tra tồn kho
            foreach (var item in cartItems)
            {
                // Luôn lấy tồn kho mới nhất từ DB (không tin tưởng dữ liệu cũ trong session/cart)
                var currentStock = await _context.ProductVariants
                    .Where(v => v.Id == item.VariantId)
                    .Select(v => v.Stock)
                    .FirstOrDefaultAsync();

                if (item.Quantity > currentStock)
                {
                    // Nếu quá số lượng -> Đẩy về giỏ hàng kèm thông báo
                    TempData["Error"] = $"Sản phẩm '{item.Product.Name}' chỉ còn {currentStock} món. Vui lòng cập nhật lại giỏ hàng.";
                    return RedirectToAction("Index", "Cart");
                }
            }

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

                // Lưu phương thức thanh toán vào Model để lưu xuống DB
                order.PaymentMethod = paymentMethod; // <--- GÁN GIÁ TRỊ VÀO ĐÂY

                if (paymentMethod == "VNPAY")
                {
                    order.Status = "Unpaid";
                }
                else
                {
                    order.Status = "Pending"; // COD
                }

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

                    // Trừ tồn kho
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant != null)
                    {
                        variant.Stock -= item.Quantity;

                        // Ghi Log Kho
                        var log = new InventoryLog
                        {
                            ProductVariantId = item.VariantId,
                            Action = "Sale",
                            ChangeAmount = -item.Quantity,
                            RemainingStock = variant.Stock,
                            ReferenceCode = order.OrderCode,
                            UserId = User.Identity.Name ?? "Guest",
                            CreatedAt = DateTime.Now
                        };
                        _context.InventoryLogs.Add(log);
                        _context.ProductVariants.Update(variant);
                    }
                }

                // Xử lý thanh toán VNPAY
                if (paymentMethod == "VNPAY")
                {
                    var vnpay = new VnPayLibrary();
                    vnpay.AddRequestData("vnp_Version", "2.1.0");
                    vnpay.AddRequestData("vnp_Command", "pay");
                    vnpay.AddRequestData("vnp_TmnCode", "YOUR_TMN_CODE");
                    vnpay.AddRequestData("vnp_Amount", ((long)order.TotalAmount * 100).ToString());
                    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", "VND");
                    vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                    vnpay.AddRequestData("vnp_Locale", "vn");
                    vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.OrderCode);
                    vnpay.AddRequestData("vnp_OrderType", "other");
                    vnpay.AddRequestData("vnp_ReturnUrl", Url.Action("PaymentCallback", "Checkout", null, Request.Scheme));
                    vnpay.AddRequestData("vnp_TxnRef", order.OrderCode);

                    string paymentUrl = vnpay.CreateRequestUrl("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", "YOUR_HASH_SECRET");

                    // Lưu thay đổi trước khi redirect
                    await _context.SaveChangesAsync();
                    return Redirect(paymentUrl);
                }

                // Trừ lượt dùng Coupon
                if (coupon != null)
                {
                    coupon.UsedCount++;
                    _context.Coupons.Update(coupon);
                }

                // Xóa giỏ hàng & Session
                _context.CartItems.RemoveRange(cartItems);
                HttpContext.Session.Remove("CheckoutItems");
                HttpContext.Session.Remove("CouponCode");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(OrderCompleted), new { id = order.Id });
            }

            // --- 2. XỬ LÝ KHI CÓ LỖI (ĐỂ KHÔNG BỊ MẤT GIAO DIỆN) ---

            // Load lại Sổ địa chỉ từ DB (QUAN TRỌNG - Code cũ thiếu dòng này)
            model.SavedAddresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            // Trả về lại View với đầy đủ dữ liệu
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
        // GET: /Checkout/PaymentCallback
        public async Task<IActionResult> PaymentCallback()
        {
            var response = Request.Query;
            if (response.Count > 0)
            {
                var vnpay = new VnPayLibrary();
                foreach (var s in response)
                {
                    if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s.Key, s.Value);
                    }
                }

                // Lấy mã đơn hàng
                string orderCode = vnpay.GetResponseData("vnp_TxnRef");
                // Lấy mã phản hồi (00 = Thành công)
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = response["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, "YOUR_HASH_SECRET"); // Phải khớp với SecretKey lúc tạo

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        // Thanh toán thành công -> Cập nhật DB
                        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
                        if (order != null)
                        {
                            order.Status = "Paid"; // Đã thanh toán
                            await _context.SaveChangesAsync();

                            // Xóa giỏ hàng (nếu chưa xóa ở bước trước)
                            // ... 

                            return View("PaymentSuccess"); // Tạo View thông báo thành công
                        }
                    }
                    else
                    {
                        // Thanh toán thất bại / Hủy bỏ
                        return View("PaymentFail");
                    }
                }
            }
            return View("PaymentFail");
        }
    }
}
