namespace NTN_STORE.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public decimal DiscountPercent { get; set; }

        public DateTime ExpireDate { get; set; }
    }
}
