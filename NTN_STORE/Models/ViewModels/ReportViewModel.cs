namespace NTN_STORE.Models.ViewModels
{
    public class ReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Báo cáo Tài chính
        public decimal TotalRevenue { get; set; } // Doanh thu
        public decimal TotalCost { get; set; }    // Giá vốn
        public decimal GrossProfit => TotalRevenue - TotalCost; // Lợi nhuận gộp
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }

        // Báo cáo Hủy đơn
        public int CancelledOrders { get; set; }
        public double CancelRate => TotalOrders > 0 ? Math.Round((double)CancelledOrders * 100 / TotalOrders, 2) : 0;
    }
}