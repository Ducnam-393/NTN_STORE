using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTN_STORE.Models
{
    public class BlogPost
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [StringLength(250)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Summary { get; set; } // Tóm tắt ngắn (Hiển thị ở trang chủ/danh sách)

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Content { get; set; } // Nội dung chính (HTML)

        public string? ThumbnailUrl { get; set; } // Ảnh đại diện (Đổi tên từ Thumbnail -> ThumbnailUrl cho khớp View)

        [StringLength(100)]
        public string Author { get; set; } = "Admin"; // Tên tác giả

        public bool IsVisible { get; set; } = true; // Trạng thái hiển thị (Ẩn/Hiện)

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}