using NTN_STORE.Models; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models.ViewModels
{
    public class ProductFormViewModel
    {
        // Dữ liệu sản phẩm
        public Product Product { get; set; }

        // Thêm dấu ? để cho phép null (không bắt buộc validation)
        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? Brands { get; set; }

        // Ảnh có thể null (khi edit không chọn ảnh mới, hoặc create không bắt buộc)
        public IFormFile? ImageFile { get; set; }
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}