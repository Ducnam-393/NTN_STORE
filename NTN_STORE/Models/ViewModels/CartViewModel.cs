using NTN_STORE.Models;
using System.Collections.Generic;
using System.Linq;

namespace NTN_STORE.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }

        // Tổng tiền hàng chưa giảm
        public decimal SubTotal => CartItems.Sum(x => x.Product.Price * x.Quantity);

        // Số tiền được giảm
        public decimal DiscountAmount { get; set; } = 0;

        // Mã giảm giá đang áp dụng
        public string AppliedCoupon { get; set; }

        // Tổng thanh toán cuối cùng
        public decimal Total => SubTotal - DiscountAmount;
    }
}