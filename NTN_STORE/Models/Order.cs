namespace NTN_STORE.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public string PaymentMethod { get; set; } // COD, Banking

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
