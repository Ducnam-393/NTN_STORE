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
            List<int> brandIds,      // Đổi từ int? brandId sang List<int> để chọn nhiều hãng
            List<string> sizes,      // Mới: Lọc theo size
            List<string> colors,     // Mới: Lọc theo màu
            decimal? minPrice,
            decimal? maxPrice,
            string sort,             // Mới: Sắp xếp (price_asc, price_desc, newest)
            string? search,
            int? categoryId,
            int page = 1,
            int pageSize = 9)
        {
            // 1. Query cơ bản
            var products = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants) // Quan trọng: Load variants để lọc size/màu
                .Where(p => p.IsActive)   // Chỉ lấy sp đang kích hoạt
                .AsQueryable();

            // 2. Áp dụng các bộ lọc

            // Filter Category
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            // Filter Brand (Nhiều lựa chọn)
            if (brandIds != null && brandIds.Any())
                products = products.Where(p => brandIds.Contains(p.BrandId));

            // Filter Price
            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);

            // Filter Search
            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            // Filter Size (Quan hệ 1-N)
            if (sizes != null && sizes.Any())
            {
                // Lấy sản phẩm có ít nhất 1 variant nằm trong danh sách size đã chọn
                products = products.Where(p => p.Variants.Any(v => sizes.Contains(v.Size) && v.Stock > 0));
            }

            // Filter Color
            if (colors != null && colors.Any())
            {
                products = products.Where(p => p.Variants.Any(v => colors.Contains(v.Color) && v.Stock > 0));
            }

            // 3. Sắp xếp
            switch (sort)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default: // "newest" hoặc null
                    products = products.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // 4. Chuẩn bị dữ liệu cho View (Sidebar)
            // Lấy tất cả Size và Color unique đang có trong Database để hiện checkbox
            var allVariants = await _context.ProductVariants.ToListAsync();

            ViewBag.AllSizes = allVariants.Select(v => v.Size).Distinct().OrderBy(s => s).ToList();
            ViewBag.AllColors = allVariants.Select(v => v.Color).Distinct().OrderBy(c => c).ToList();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Lưu lại trạng thái lọc hiện tại để hiển thị lại trên View
            ViewBag.CurrentBrandIds = brandIds;
            ViewBag.CurrentSizes = sizes;
            ViewBag.CurrentColors = colors;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.Search = search;

            // 5. Phân trang
            int totalItems = await products.CountAsync();
            var data = await products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

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
                .Include(r => r.User)        // Load thông tin người dùng
                .Include(r => r.Images)      // Load ảnh review (QUAN TRỌNG)
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
                if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                // Tạo Review
                var review = new Review
                {
                    ProductId = model.ProductId,
                    UserId = user.Id,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync(); // Lưu để có ReviewId trước

                // Xử lý upload ảnh (nếu có)
                if (model.ReviewImages != null && model.ReviewImages.Count > 0)
                {
                    // Đường dẫn thư mục lưu: wwwroot/img/reviews/
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "reviews");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    foreach (var file in model.ReviewImages)
                    {
                        if (file.Length > 0)
                        {
                            // Đặt tên file ngẫu nhiên để tránh trùng
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string filePath = Path.Combine(folderPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Lưu vào bảng ReviewImage
                            var reviewImage = new ReviewImage
                            {
                                ReviewId = review.Id,
                                Url = "/img/reviews/" + fileName
                            };
                            _context.ReviewImages.Add(reviewImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["ReviewSuccess"] = "Đánh giá của bạn đã được gửi!";
                return RedirectToAction(nameof(Detail), new { id = model.ProductId });
            }

            TempData["ReviewError"] = "Vui lòng kiểm tra lại thông tin đánh giá.";
            return RedirectToAction(nameof(Detail), new { id = model.ProductId });
        }
    }
}
