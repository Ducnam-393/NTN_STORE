using NTN_STORE.Models;

namespace NTN_STORE.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Product> RecentProducts { get; set; } // Sản phẩm mới
        public IEnumerable<Product> BestSellers { get; set; }    // Bán chạy
        public IEnumerable<Brand> Brands { get; set; }
        public IEnumerable<BlogPost> BlogPosts { get; set; }     // Tin tức
    }
}