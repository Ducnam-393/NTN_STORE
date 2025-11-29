using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace NTN_STORE.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thêm dòng này
        public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
    }
}