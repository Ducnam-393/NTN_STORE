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
            var vm = new HomeViewModel();

            // 1. Sản phẩm mới (giữ nguyên)
            vm.RecentProducts = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync();

            // 2. Sản phẩm bán chạy (Logic: Đếm trong OrderDetail)
            // Tạm thời lấy ngẫu nhiên để demo nếu chưa có nhiều đơn hàng
            vm.BestSellers = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.IsFeatured) // Hoặc logic count order
                .Take(8).ToListAsync();

            // 3. Brands
            vm.Brands = await _context.Brands.ToListAsync();

            // 4. Blog (Nếu chưa có bảng Blog thì mock data hoặc null)
            vm.BlogPosts = await _context.BlogPosts
                .Where(b => b.IsVisible)
                .OrderByDescending(b => b.CreatedAt)
                .Take(3)
                .ToListAsync();
            vm.Sliders = await _context.Sliders
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return View(vm);
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
        public IActionResult Returns()
        {
            return View();
        }

        // 2. Action cho trang Bảo hành
        public IActionResult Warranty()
        {
            return View();
        }

        // 3. Action cho trang Vận chuyển
        public IActionResult Shipping()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }

        // 2. Action cho trang Hỗ trợ chung (Help Center)
        public IActionResult Support()
        {
            return View();
        }

        // 3. Action cho trang Câu hỏi thường gặp (FAQ)
        public IActionResult FAQ()
        {
            return View();
        }
    }
}