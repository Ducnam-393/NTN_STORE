using ClosedXML.Excel; // Nhớ thêm thư viện này
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System.IO;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly NTNStoreContext _context;

        public ReportsController(NTNStoreContext context)
        {
            _context = context;
        }

        // 1. XEM BÁO CÁO (HTML)
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate ?? DateTime.Now;

            // Lấy đơn hàng (Include chi tiết để tính vốn)
            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.CreatedAt >= start && o.CreatedAt < end.AddDays(1)) // < end + 1 ngày để lấy trọn ngày cuối
                .ToListAsync();

            // Tính toán
            var successOrders = orders.Where(o => o.Status == "Completed" || o.Status == "Shipped").ToList();
            decimal revenue = successOrders.Sum(o => o.TotalAmount);
            decimal cost = 0;

            foreach (var order in successOrders)
            {
                foreach (var detail in order.OrderDetails)
                {
                    decimal importPrice = detail.Product?.ImportPrice ?? 0;
                    cost += detail.Quantity * importPrice;
                }
            }

            var vm = new ReportViewModel
            {
                FromDate = start,
                ToDate = end,
                TotalOrders = orders.Count,
                CompletedOrders = successOrders.Count,
                CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                TotalRevenue = revenue,
                TotalCost = cost
            };

            return View(vm);
        }

        // 2. XUẤT EXCEL (ACTION MỚI)
        public async Task<IActionResult> ExportReport(DateTime? fromDate, DateTime? toDate)
        {
            var start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate ?? DateTime.Now;

            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.CreatedAt >= start && o.CreatedAt < end.AddDays(1))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                // SHEET 1: TỔNG QUAN
                var sheet1 = workbook.Worksheets.Add("Tổng quan");

                // Tiêu đề
                sheet1.Cell(1, 1).Value = "BÁO CÁO KINH DOANH";
                sheet1.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;
                sheet1.Cell(2, 1).Value = $"Từ ngày: {start:dd/MM/yyyy} - Đến ngày: {end:dd/MM/yyyy}";

                // Số liệu
                var successOrders = orders.Where(o => o.Status == "Completed" || o.Status == "Shipped").ToList();
                decimal revenue = successOrders.Sum(o => o.TotalAmount);
                decimal cost = successOrders.Sum(o => o.OrderDetails.Sum(d => d.Quantity * (d.Product?.ImportPrice ?? 0)));

                sheet1.Cell(4, 1).Value = "Chỉ tiêu";
                sheet1.Cell(4, 2).Value = "Giá trị";
                sheet1.Row(4).Style.Font.Bold = true;

                sheet1.Cell(5, 1).Value = "Tổng đơn hàng";
                sheet1.Cell(5, 2).Value = orders.Count;

                sheet1.Cell(6, 1).Value = "Đơn thành công";
                sheet1.Cell(6, 2).Value = successOrders.Count;

                sheet1.Cell(7, 1).Value = "Doanh thu";
                sheet1.Cell(7, 2).Value = revenue;

                sheet1.Cell(8, 1).Value = "Vốn hàng bán";
                sheet1.Cell(8, 2).Value = cost;

                sheet1.Cell(9, 1).Value = "Lợi nhuận gộp";
                sheet1.Cell(9, 2).Value = revenue - cost;

                sheet1.Columns().AdjustToContents();

                // SHEET 2: CHI TIẾT ĐƠN HÀNG
                var sheet2 = workbook.Worksheets.Add("Chi tiết đơn hàng");
                string[] headers = { "Mã ĐH", "Ngày đặt", "Khách hàng", "Trạng thái", "Tổng tiền" };

                for (int i = 0; i < headers.Length; i++)
                {
                    sheet2.Cell(1, i + 1).Value = headers[i];
                    sheet2.Cell(1, i + 1).Style.Font.Bold = true;
                    sheet2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                int row = 2;
                foreach (var item in orders)
                {
                    sheet2.Cell(row, 1).Value = item.OrderCode;
                    sheet2.Cell(row, 2).Value = item.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                    sheet2.Cell(row, 3).Value = item.CustomerName;
                    sheet2.Cell(row, 4).Value = item.Status;
                    sheet2.Cell(row, 5).Value = item.TotalAmount;
                    row++;
                }
                sheet2.Columns().AdjustToContents();

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"BaoCao_{start:ddMMyy}_{end:ddMMyy}.xlsx");
                }
            }
        }
    }
}