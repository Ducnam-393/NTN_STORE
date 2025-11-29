using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } // Mã voucher, ví dụ: "NAMMOI2024"

        public string Description { get; set; }

        [Range(0, 100)]
        public int DiscountPercent { get; set; } = 0; // Giảm theo % (0-100)

        public decimal DiscountAmount { get; set; } = 0; // Giảm số tiền cố định (VD: 50,000)

        public DateTime ExpiryDate { get; set; } // Hạn sử dụng

        public int UsageLimit { get; set; } // Giới hạn số lần dùng

        public int UsedCount { get; set; } = 0; // Đã dùng bao nhiêu lần

        public bool IsActive { get; set; } = true;
    }
}