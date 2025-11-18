using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models; // Thêm
using NTN_STORE.Models.ViewModels;

namespace NTN_STORE.Controllers
{
    public class HomeController : Controller
    {
        private readonly NTNStoreContext _context; // Thêm

        // Thêm constructor
        public HomeController(NTNStoreContext context)
        {
            _context = context;
        }

        // Sửa action Index
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Products) // Include để đếm số lượng sản phẩm
                .ToListAsync();

            var featuredProducts = await _context.Products
                .Include(p => p.Images) // Include ảnh
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8) // Lấy 8 sản phẩm
                .ToListAsync();

            var recentProducts = await _context.Products
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8) // Lấy 8 sản phẩm mới nhất
                .ToListAsync();

            var brands = await _context.Brands.ToListAsync();

            var vm = new HomeViewModel
            {
                Categories = categories,
                FeaturedProducts = featuredProducts,
                RecentProducts = recentProducts,
                Brands = brands
            };

            return View(vm); // Truyền ViewModel
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                ViewBag.SuccessMessage = "Gửi tin nhắn thành công! Chúng tôi sẽ sớm liên hệ với bạn.";
                return View(new ContactViewModel());
            }
            return View(model);
        }
    }
}