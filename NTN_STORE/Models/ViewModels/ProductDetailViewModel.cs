namespace NTN_STORE.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductImage> Images { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }
}
