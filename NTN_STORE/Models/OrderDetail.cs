namespace NTN_STORE.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int VariantId { get; set; }
        public ProductVariant Variant { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
