using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên của bạn.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn.")]
        public string Message { get; set; }
    }
}