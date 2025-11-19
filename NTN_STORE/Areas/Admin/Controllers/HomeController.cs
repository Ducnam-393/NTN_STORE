
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
    [Authorize(Roles = "Admin,Manager")] // Đảm bảo có quyền truy cập
    public class HomeController : Controller
    {
        private readonly NTNStoreContext _context;

        public HomeController(NTNStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var viewModel = new DashboardViewModel();

            // 1. Thống kê cơ bản (Cards)
            // Giả sử Order có trạng thái và TotalAmount. Cần kiểm tra model thực tế của bạn.

            // Doanh thu tháng này (Chỉ tính đơn đã hoàn thành nếu có trạng thái)
            viewModel.MonthlyRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Month == currentMonth && o.CreatedAt.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            // Doanh thu năm nay
            viewModel.AnnualRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            // Đơn hàng chờ xử lý (Giả sử chưa giao là chờ xử lý, hoặc check status = 0)
            // Bạn cần điều chỉnh điều kiện Where tùy theo Enum trạng thái đơn hàng của bạn
            viewModel.PendingOrders = await _context.Orders.CountAsync();

            // Tổng số sản phẩm
            viewModel.TotalProducts = await _context.Products.CountAsync();


            // 2. Dữ liệu biểu đồ doanh thu (12 tháng)
            var revenueData = new List<decimal>();
            var revenueLabels = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                decimal monthRev = _context.Orders
                    .Where(o => o.CreatedAt.Year == currentYear && o.CreatedAt.Month == i)
                    .Sum(o => o.TotalAmount);
                revenueData.Add(monthRev);
                revenueLabels.Add("Tháng " + i);
            }
            viewModel.RevenueData = revenueData;
            viewModel.RevenueLabels = revenueLabels;


            // 3. Dữ liệu biểu đồ tròn (Tỷ lệ sản phẩm theo Hãng/Category)
            // Ví dụ lấy theo Category
            var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
            viewModel.CategoryLabels = categories.Select(c => c.Name).ToList();
            viewModel.CategoryData = categories.Select(c => c.Products.Count).ToList();


            // 4. Đơn hàng gần đây (Lấy 5 đơn mới nhất)
            viewModel.RecentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }
    }
}