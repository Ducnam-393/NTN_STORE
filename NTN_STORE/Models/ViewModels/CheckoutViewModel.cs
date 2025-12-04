using NTN_STORE.Models;

namespace NTN_STORE.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // 1. Dùng để hiển thị tóm tắt đơn hàng
        public CartViewModel Cart { get; set; }

        // 2. Dùng để nhận dữ liệu từ Form
        // Chúng ta dùng luôn Model Order để bind dữ liệu
        public Order ShippingDetails { get; set; }
        public List<UserAddress> SavedAddresses { get; set; }
    }
}