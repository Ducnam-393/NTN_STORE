using NTN_STORE.Models;
using System.Collections.Generic;
using System.Linq;

namespace NTN_STORE.Models.ViewModels
{
    public class CartViewModel
    {
        // Danh sách các CartItem lấy từ CSDL
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Tính toán dựa trên CartItems
        // Lưu ý: Cần Include(c => c.Product) trong Controller
        public decimal Subtotal => CartItems.Sum(item => item.Product.Price * item.Quantity);

        public decimal Shipping { get; set; } = 30000; // Tạm tính 30k

        public decimal Total => Subtotal + Shipping;
    }
}