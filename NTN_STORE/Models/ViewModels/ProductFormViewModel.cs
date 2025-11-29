using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace NTN_STORE.Models.ViewModels
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Display(Name = "Giá nhập (Vốn)")]
        public decimal ImportPrice { get; set; }

        [Display(Name = "Giá gốc (để gạch ngang)")]
        public decimal? OriginalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int BrandId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        // --- XỬ LÝ ẢNH ---
        [Display(Name = "Ảnh sản phẩm")]
        public List<IFormFile>? ImageFiles { get; set; } // Hứng file upload
        public List<ProductImage>? ExistingImages { get; set; } // Hiển thị ảnh cũ khi Edit
        public List<int>? ImagesToDelete { get; set; } // Danh sách ID ảnh cần xóa

        // --- XỬ LÝ BIẾN THỂ (Size/Màu) ---
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();
    }

    public class ProductVariantViewModel
    {
        public int Id { get; set; } // = 0 nếu là variant mới
        public string Color { get; set; }
        public string Size { get; set; }
        public int Stock { get; set; }
        public bool IsDeleted { get; set; } = false; // Đánh dấu để xóa
    }
}