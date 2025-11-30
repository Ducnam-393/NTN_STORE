using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly NTNStoreContext _context;

        public HomeController(NTNStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Các con số tổng quan
            int totalOrders = await _context.Orders.CountAsync();
            int totalProducts = await _context.Products.CountAsync();
            int totalCustomers = await _context.Users.CountAsync(); // Đếm user trong bảng Identity
            int sliderCount = await _context.Sliders.CountAsync();
            int blogCount = await _context.BlogPosts.CountAsync();
            int couponCount = await _context.Coupons.Where(c => c.IsActive && c.ExpiryDate > DateTime.Now).CountAsync();
            ViewBag.SliderCount = sliderCount;
            ViewBag.BlogCount = blogCount;
            ViewBag.ActiveCoupons = couponCount;
            // 2. Tính Tài chính (Chỉ tính các đơn đã Hoàn thành/Đang giao)
            // Doanh thu (Tổng tiền bán được)
            decimal totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Shipped")
                .SumAsync(o => o.TotalAmount);

            // Chi phí vốn (Giá nhập * Số lượng đã bán)
            // Lưu ý: Cần join bảng để lấy ImportPrice từ Product
            decimal totalExpense = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.Status == "Completed" || od.Order.Status == "Shipped")
                .SumAsync(od => od.Quantity * (od.Product != null ? od.Product.ImportPrice : 0));
            // 3. Tính Thuế & Lợi nhuận theo Luật Việt Nam 2025 (Mô hình Doanh nghiệp)
            // Lợi nhuận gộp = Doanh thu - Giá vốn
            decimal grossProfit = totalRevenue - totalExpense;

            // Thuế Thu nhập doanh nghiệp (CIT): 20% trên lợi nhuận (Nếu lỗ thì không đóng thuế)
            // Nếu là Hộ kinh doanh: Sửa thành totalRevenue * 0.015m (1.5% doanh thu)
            decimal corporateTaxRate = 0.20m;
            decimal estimatedTax = grossProfit > 0 ? grossProfit * corporateTaxRate : 0;

            // Lợi nhuận ròng (Tiền thực nhận)
            // NetProfit đã được tính trong ViewModel = Revenue - Expense - Tax

            // 3. Biểu đồ doanh thu (7 ngày gần nhất)
            var today = DateTime.Today;
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i)).ToList();
            var chartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();

            // Lấy dữ liệu doanh thu nhóm theo ngày từ DB để tối ưu
            var revenueData = await _context.Orders
                .Where(o => o.CreatedAt >= today.AddDays(-6) && (o.Status == "Completed" || o.Status == "Shipped"))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            // Map dữ liệu vào danh sách 7 ngày (ngày nào ko có đơn thì = 0)
            var chartRevenue = new List<decimal>();
            foreach (var day in last7Days)
            {
                var rev = revenueData.FirstOrDefault(r => r.Date == day)?.Total ?? 0;
                chartRevenue.Add(rev);
            }

            // 4. FIX LỖI: Thống kê Kho hàng theo Hãng
            // Thay vì đi từ Products (bị lồng Sum), ta đi từ ProductVariant (phẳng hơn)
            var brandStats = await _context.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .Where(v => v.Product != null && v.Product.Brand != null) // Check null an toàn
                .GroupBy(v => v.Product.Brand.Name)
                .Select(g => new BrandStat
                {
                    BrandName = g.Key,
                    StockQuantity = g.Sum(v => v.Stock), // Tổng tồn kho
                    ImportCost = g.Sum(v => v.Stock * v.Product.ImportPrice) // Tổng giá trị vốn tồn kho
                })
                .ToListAsync();

            // 5. Đóng gói ViewModel
            var vm = new DashboardViewModel
            {
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalCustomers = totalCustomers,

                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                EstimatedTax = estimatedTax,

                ChartLabels = chartLabels,
                ChartRevenueData = chartRevenue,

                // ChartExpenseData để trống hoặc làm tương tự nếu muốn vẽ 2 đường
                ChartExpenseData = new List<decimal>(),

                BrandStats = brandStats
            };

            return View(vm);
        }
    }
}