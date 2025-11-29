namespace NTN_STORE.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Thống kê tổng quan
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }

        // Tài chính
        public decimal TotalRevenue { get; set; } // Tổng thu (Doanh số)
        public decimal TotalExpense { get; set; } // Tổng chi (Tiền vốn hàng đã bán)
        public decimal EstimatedTax { get; set; } // Thuế ước tính (VD: 8-10%)
        public decimal NetProfit => TotalRevenue - TotalExpense - EstimatedTax; // Lợi nhuận ròng

        // Biểu đồ
        public List<string> ChartLabels { get; set; } // Nhãn ngày/tháng
        public List<decimal> ChartRevenueData { get; set; } // Dữ liệu doanh thu
        public List<decimal> ChartExpenseData { get; set; } // Dữ liệu chi phí

        // Thống kê theo Hãng (Brand)
        public List<BrandStat> BrandStats { get; set; }
    }

    public class BrandStat
    {
        public string BrandName { get; set; }
        public decimal ImportCost { get; set; } // Tiền nhập hàng của hãng này
        public int StockQuantity { get; set; }  // Số lượng tồn kho
    }
}