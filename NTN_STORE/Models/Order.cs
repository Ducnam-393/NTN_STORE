using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } // Mã đơn hàng

        // Thông tin người nhận
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Email { get; set; }

        public string? Notes { get; set; } // Ghi chú (có thể null)
        public string? CouponCode { get; set; } // Mã đã dùng
        public decimal DiscountValue { get; set; } = 0; // Số tiền được giảm
        public decimal TotalAmount { get; set; } // Tổng tiền
        public string Status { get; set; } // Ví dụ: "Pending", "Processing", "Completed"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "COD"; // Mặc định là COD
        // Khóa ngoại tới người dùng
        public string? UserId { get; set; }
        public virtual ApplicationUser ? User { get; set; } 

        // Quan hệ 1-Nhiều
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}