namespace NTN_STORE.Models
{
    public class Review
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Rating { get; set; } // 1-5

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
