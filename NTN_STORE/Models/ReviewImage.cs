using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTN_STORE.Models
{
    public class ReviewImage
    {
        public int Id { get; set; }

        [Required]
        public string Url { get; set; } // Đường dẫn ảnh

        public int ReviewId { get; set; }

        [ForeignKey("ReviewId")]
        public Review Review { get; set; }
    }
}