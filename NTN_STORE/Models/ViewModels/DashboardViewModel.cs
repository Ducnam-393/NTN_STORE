using System;
using System.Collections.Generic;

namespace NTN_STORE.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Các thẻ thống kê (Cards)
        public decimal MonthlyRevenue { get; set; } // Doanh thu tháng này
        public decimal AnnualRevenue { get; set; }  // Doanh thu năm nay
        public int PendingOrders { get; set; }      // Đơn chờ xử lý
        public int TotalProducts { get; set; }      // Tổng số mẫu giày

        // Biểu đồ vùng (Area Chart - Doanh thu 12 tháng)
        public List<decimal> RevenueData { get; set; }
        public List<string> RevenueLabels { get; set; }

        // Biểu đồ tròn (Pie Chart - Tỷ lệ thương hiệu hoặc danh mục)
        public List<int> CategoryData { get; set; }
        public List<string> CategoryLabels { get; set; }

        // Danh sách đơn hàng mới nhất
        public List<Order> RecentOrders { get; set; }
    }
}