using System.ComponentModel.DataAnnotations;

namespace NTN_STORE.Models.ViewModels
{
    public class SubmitReviewViewModel
    {
        public int ProductId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
        [Required(ErrorMessage = "Vui lòng chọn Rating.")]
        public int Rating { get; set; } // 1-5

        [StringLength(500, ErrorMessage = "Bình luận không được vượt quá 500 ký tự.")]
        public string? Comment { get; set; }
    }
}