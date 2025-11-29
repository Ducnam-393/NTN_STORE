using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class OrderController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách đơn hàng (Có lọc theo trạng thái)
        public async Task<IActionResult> Index(string status = "All")
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.Images)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .Where(o => o.UserId == userId);

            // Lọc trạng thái
            if (status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // 2. Chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.Images)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View(order);
        }

        // 3. Hủy đơn hàng (Chỉ khi đang Pending)
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng này.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. CHỨC NĂNG MUA LẠI (RE-ORDER)
        public async Task<IActionResult> BuyAgain(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    // 1. Check xem sản phẩm/biến thể còn tồn tại và còn hàng không
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant != null && variant.Stock > 0)
                    {
                        // 2. Thêm vào giỏ hàng
                        var cartItem = await _context.CartItems
                            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == item.ProductId && c.VariantId == item.VariantId);

                        if (cartItem != null)
                        {
                            cartItem.Quantity += 1; // Đã có thì +1
                        }
                        else
                        {
                            _context.CartItems.Add(new CartItem
                            {
                                UserId = userId,
                                ProductId = item.ProductId,
                                VariantId = item.VariantId,
                                Quantity = 1 // Mặc định mua lại số lượng 1
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
                return RedirectToAction("Index", "Cart");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}