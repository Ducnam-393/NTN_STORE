using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System;

namespace NTN_STORE.Controllers
{
    public class ProductController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProductController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // /Product
        public async Task<IActionResult> Index(
            int? categoryId,
            int? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            string? search,
            int page = 1,
            int pageSize = 12)
        {
            var products = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // Filter theo danh mục
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            // Filter theo thương hiệu
            if (brandId.HasValue)
                products = products.Where(p => p.BrandId == brandId.Value);

            // Filter theo giá
            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.Name.Contains(search));

            // Phân trang
            int totalItems = await products.CountAsync();
            var data = await products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brandId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Search = search;

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            return View(data);
        }

        // Chi tiết sản phẩm
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants) // Load variants nếu cần
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // Lấy danh sách đánh giá
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Tính điểm trung bình
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var relatedProducts = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4)
                .ToListAsync();

            var vm = new ProductDetailViewModel
            {
                Product = product,
                Images = product.Images?.ToList(),
                Variants = product.Variants?.ToList(),
                RelatedProducts = relatedProducts,
                // Thông tin Review
                Reviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                ReviewCount = reviews.Count,
                NewReview = new SubmitReviewViewModel { ProductId = id }
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReview([Bind(Prefix = "NewReview")] SubmitReviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                var review = new Review
                {
                    ProductId = model.ProductId,
                    UserId = user.Id,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["ReviewSuccess"] = "Đánh giá của bạn đã được gửi thành công!";
                return RedirectToAction(nameof(Detail), new { id = model.ProductId });
            }

            // SỬA THÊM: Nếu lỗi (ví dụ chưa chọn sao), thông báo cho người dùng biết
            TempData["ReviewError"] = "Vui lòng chọn số sao đánh giá và thử lại.";

            // Redirect lại đúng trang sản phẩm (Lưu ý: Model.ProductId có thể bằng 0 nếu bind lỗi, nên dùng RouteValues cẩn thận)
            // Tuy nhiên với [Bind] ở trên thì model.ProductId sẽ có giá trị đúng.
            return RedirectToAction(nameof(Detail), new { id = model.ProductId });
        }
    }
}
