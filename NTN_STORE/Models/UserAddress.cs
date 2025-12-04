using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTN_STORE.Models
{
    public class UserAddress
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } // Liên kết với bảng User
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        public string Address { get; set; } // Số nhà, đường...

        public string Province { get; set; } // Tỉnh/Thành
        public string District { get; set; } // Quận/Huyện
        public string Ward { get; set; } // Phường/Xã

        public bool IsDefault { get; set; } // Đặt làm mặc định
    }
}