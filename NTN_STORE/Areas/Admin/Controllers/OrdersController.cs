using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

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
            // 1. KIỂM TRA AN TOÀN (Fix lỗi Crash)
            if (string.IsNullOrEmpty(status))
            {
                TempData["Error"] = "Lỗi: Trạng thái không hợp lệ (Rỗng). Vui lòng chọn lại.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // 2. CHẶN THAY ĐỔI NẾU ĐƠN ĐÃ KẾT THÚC
            if (order.Status == "Cancelled" || order.Status == "Completed")
            {
                TempData["Error"] = $"Đơn hàng đang ở trạng thái '{order.Status}', không thể thay đổi được nữa!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // 3. LOGIC HOÀN KHO KHI HỦY
            if (status == "Cancelled" && order.Status != "Cancelled")
            {
                foreach (var item in order.OrderDetails)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant != null)
                    {
                        variant.Stock += item.Quantity; // Cộng lại kho
                        _context.Update(variant);

                        // Ghi log kho (nếu bạn đã làm phần Inventory)
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductVariantId = item.VariantId,
                            Action = "Restock (Cancel Order)",
                            ChangeAmount = item.Quantity,
                            RemainingStock = variant.Stock,
                            ReferenceCode = order.OrderCode,
                            UserId = User.Identity.Name ?? "Admin",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            // Cập nhật trạng thái mới
            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{order.OrderCode} thành {status}.";

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
        public async Task<IActionResult> ExportExcel()
        {
            // 1. Lấy dữ liệu
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 2. Tạo file Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách đơn hàng");

                // Header
                worksheet.Cell(1, 1).Value = "Mã ĐH";
                worksheet.Cell(1, 2).Value = "Khách hàng";
                worksheet.Cell(1, 3).Value = "SĐT";
                worksheet.Cell(1, 4).Value = "Ngày đặt";
                worksheet.Cell(1, 5).Value = "Tổng tiền";
                worksheet.Cell(1, 6).Value = "Trạng thái";
                worksheet.Cell(1, 7).Value = "Ghi chú";

                // Data
                int row = 2;
                foreach (var item in orders)
                {
                    worksheet.Cell(row, 1).Value = item.OrderCode;
                    worksheet.Cell(row, 2).Value = item.CustomerName;
                    worksheet.Cell(row, 3).Value = "'" + item.PhoneNumber; // Thêm dấu ' để giữ định dạng số 0 đầu
                    worksheet.Cell(row, 4).Value = item.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 5).Value = item.TotalAmount;
                    worksheet.Cell(row, 6).Value = item.Status;
                    worksheet.Cell(row, 7).Value = item.Notes;
                    row++;
                }

                // Style
                var header = worksheet.Range("A1:G1");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Columns().AdjustToContents();

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DonHang_" + DateTime.Now.ToString("ddMMyyyy") + ".xlsx");
                }
            }
        }
    }
}