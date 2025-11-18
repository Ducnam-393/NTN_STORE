using Microsoft.AspNetCore.Mvc.Rendering;
using NTN_STORE.Models;
using System.Collections.Generic;

namespace NTN_STORE.Models.ViewModels
{
    public class ProductFormViewModel
    {
        // 1. Dữ liệu sản phẩm để bind vào form
        public Product Product { get; set; }

        // 2. Danh sách để hiển thị <select> (dropdown)
        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Brands { get; set; }
        public IFormFile ImageFile { get; set; }
    }
}