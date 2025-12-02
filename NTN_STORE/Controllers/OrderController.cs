using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. DANH SÁCH ĐƠN HÀNG (CÓ TAB LỌC)
        public async Task<IActionResult> Index(string status = "All")
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.Images)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .Where(o => o.UserId == userId);

            // Lọc theo Tab
            switch (status)
            {
                case "Pending": query = query.Where(o => o.Status == "Pending" || o.Status == "Unpaid"); break;
                case "Shipping": query = query.Where(o => o.Status == "Shipping"); break;
                case "Completed": query = query.Where(o => o.Status == "Completed"); break;
                case "Cancelled": query = query.Where(o => o.Status == "Cancelled"); break;
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // 2. CHI TIẾT ĐƠN HÀNG & TIMELINE
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

        // 3. HỦY ĐƠN HÀNG (Chỉ cho phép khi còn ở trạng thái Pending/Unpaid)
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id, string reason)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order != null && (order.Status == "Pending" || order.Status == "Unpaid"))
            {
                order.Status = "Cancelled";
                order.Notes = order.Notes + $" [Khách hủy: {reason}]"; // Lưu lý do hủy

                // (Optional) Tại đây bạn nên cộng lại số lượng tồn kho cho ProductVariant

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Đơn hàng đã được xử lý, không thể hủy.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 4. CHỨC NĂNG MUA LẠI (RE-ORDER)
        public async Task<IActionResult> BuyAgain(int id)
        {
            var userId = _userManager.GetUserId(User);
            // Lấy đơn hàng cũ
            var oldOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (oldOrder == null) return NotFound();

            // Duyệt qua từng sản phẩm trong đơn cũ để thêm vào giỏ hiện tại
            foreach (var item in oldOrder.OrderDetails)
            {
                // Kiểm tra xem sản phẩm/variant còn tồn tại và còn hàng không
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant != null && variant.Stock > 0)
                {
                    // Kiểm tra xem trong giỏ đã có chưa
                    var cartItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.VariantId == item.VariantId);

                    if (cartItem != null)
                    {
                        cartItem.Quantity += 1; // Hoặc += item.Quantity
                    }
                    else
                    {
                        _context.CartItems.Add(new CartItem
                        {
                            UserId = userId,
                            ProductId = item.ProductId,
                            VariantId = item.VariantId,
                            Quantity = 1 // Mặc định thêm 1
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index", "Cart");
        }
    }
}