namespace NTN_STORE.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductImage> Images { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<Product> RelatedProducts { get; set; }
        public IEnumerable<Review> Reviews { get; set; } = new List<Review>(); // Danh sách các review
        public double AverageRating { get; set; } // Điểm đánh giá trung bình
        public int ReviewCount { get; set; } // Tổng số lượng đánh giá
        public SubmitReviewViewModel? NewReview { get; set; }
    }
}
