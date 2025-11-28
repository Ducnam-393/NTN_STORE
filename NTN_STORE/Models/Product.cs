using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTN_STORE.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public int CategoryId { get; set; }
        public Category ? Category { get; set; }

        public int BrandId { get; set; }
        public Brand ? Brand { get; set; }

        public ICollection<ProductImage> ? Images { get; set; }
        public ICollection<ProductVariant> ? Variants { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public bool IsFeatured { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
