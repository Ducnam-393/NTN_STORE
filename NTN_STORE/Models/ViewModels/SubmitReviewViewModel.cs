using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; 
using System.Collections.Generic;

namespace NTN_STORE.Models.ViewModels
{
    public class SubmitReviewViewModel
    {
        public int ProductId { get; set; }

        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(500, ErrorMessage = "Nội dung tối đa 500 ký tự.")]
        public string Comment { get; set; }

        // Thêm thuộc tính này để nhận danh sách ảnh upload
        public List<IFormFile>? ReviewImages { get; set; }
    }
}