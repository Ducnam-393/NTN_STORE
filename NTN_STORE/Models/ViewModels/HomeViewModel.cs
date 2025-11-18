using NTN_STORE.Models;

namespace NTN_STORE.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Product> FeaturedProducts { get; set; }
        public List<Product> RecentProducts { get; set; }
        public List<Brand> Brands { get; set; }
    }
}