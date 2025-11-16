namespace NTN_STORE.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }

        public Product Product { get; set; }
        public ProductVariant Variant { get; set; }
    }
}
