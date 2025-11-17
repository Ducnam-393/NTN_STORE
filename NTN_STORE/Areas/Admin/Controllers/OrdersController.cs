using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly NTNStoreContext _context;

        public OrdersController(NTNStoreContext context)
        {
            _context = context;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            // Sắp xếp đơn hàng mới nhất lên đầu
            var orders = await _context.Orders
                                       .OrderByDescending(o => o.CreatedAt)
                                       .ToListAsync();
            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Tải chi tiết đơn hàng
                    .ThenInclude(od => od.Product) // Tải sản phẩm
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant) // Tải biến thể
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Bạn có thể thêm các hành động [POST] để cập nhật trạng thái đơn hàng (Status) ở đây
        // Ví dụ: [HttpPost] public async Task<IActionResult> UpdateStatus(int id, string status) { ... }
    }
}