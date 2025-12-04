using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace NTN_STORE.Models
{
    public class CartItem
    {
        [Key] // Thêm Khóa chính
        public int Id { get; set; }

        // Thêm Mã người dùng
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }

        public Product Product { get; set; }
        public ProductVariant Variant { get; set; }
    }
}
