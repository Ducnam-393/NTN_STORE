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

        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .ToListAsync();

            if (!cartItems.Any())
            {
                // Nếu giỏ hàng trống, đá về trang giỏ hàng
                return RedirectToAction("Index", "Cart");
            }

            var cartViewModel = new CartViewModel
            {
                CartItems = cartItems
                // Các tính toán Subtotal, Total sẽ tự động trong ViewModel
            };

            var checkoutViewModel = new CheckoutViewModel
            {
                Cart = cartViewModel,
                ShippingDetails = new Order() // Khởi tạo đối tượng Order để bind vào form
            };

            // Lấy email của user đang đăng nhập gán sẵn vào form
            var currentUser = await _userManager.GetUserAsync(User);
            checkoutViewModel.ShippingDetails.Email = currentUser.Email;

            return View(checkoutViewModel);
        }

        // POST: /Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = GetUserId();
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn bị trống.");
            }
            ModelState.Remove("Cart");
            // Gắn lại CartViewModel vào model để nếu lỗi thì vẫn hiển thị tóm tắt
            model.Cart = new CartViewModel { CartItems = cartItems };

            if (ModelState.IsValid)
            {
                // Mọi thứ hợp lệ, tiến hành tạo Order

                // 1. Tạo đối tượng Order
                var order = model.ShippingDetails; // Lấy thông tin địa chỉ từ Form
                order.UserId = userId;
                order.CreatedAt = DateTime.Now;
                order.Status = "Pending"; // Trạng thái chờ xử lý
                order.TotalAmount = model.Cart.Total; // Tổng tiền từ ViewModel
                order.OrderCode = $"NTN-{DateTime.Now:yyyyMMddHHmmss}"; // Mã đơn hàng tạm

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu để lấy OrderId

                // 2. Chuyển CartItems thành OrderDetails
                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price // Lấy giá tại thời điểm đặt hàng
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // 3. Xóa giỏ hàng
                _context.CartItems.RemoveRange(cartItems);

                // 4. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // 5. Chuyển hướng đến trang hoàn tất
                return RedirectToAction(nameof(OrderCompleted), new { id = order.Id });
            }

            // Nếu ModelState không hợp lệ, quay lại trang checkout và báo lỗi
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
