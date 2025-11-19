namespace NTN_STORE.Models
{
    public class Brand
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<Product>? Products { get; set; }
    }
}
