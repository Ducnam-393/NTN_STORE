using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models
{
    public class Slider
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề lớn")]
        public string? Title { get; set; }

        [Display(Name = "Mô tả / Tiêu đề nhỏ")]
        public string? SubTitle { get; set; }

        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; } // URL ảnh banner

        [Display(Name = "Link liên kết (Khi bấm nút)")]
        public string? LinkUrl { get; set; } = "/Product";

        public int DisplayOrder { get; set; } = 0; // Thứ tự hiển thị
        public bool IsActive { get; set; } = true;
    }
}