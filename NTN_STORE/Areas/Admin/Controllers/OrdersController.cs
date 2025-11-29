using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly NTNStoreContext _context;

        public OrdersController(NTNStoreContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG (CÓ LỌC)
        public async Task<IActionResult> Index(string status)
        {
            // Query cơ bản
            var query = _context.Orders.AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            // Sắp xếp mới nhất lên đầu
            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            // Truyền trạng thái hiện tại ra View để highlight nút lọc
            ViewBag.CurrentStatus = status ?? "All";

            // Đếm số lượng từng trạng thái để hiện badge (Optional)
            ViewBag.CountPending = await _context.Orders.CountAsync(o => o.Status == "Pending");

            return View(orders);
        }

        // 2. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Images) // Load ảnh sản phẩm
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant) // Load Size/Màu
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Logic kiểm tra chuyển trạng thái hợp lệ (nếu cần)
            // Ví dụ: Không thể chuyển từ "Cancelled" về "Pending"

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn #{order.OrderCode} thành công.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 4. IN HÓA ĐƠN (VIEW RIÊNG ĐỂ IN)
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}