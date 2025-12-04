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
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
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
            // --- LOGIC SẢN PHẨM ĐÃ XEM ---
            var recentIds = new List<int>();
            if (Request.Cookies["RecentProducts"] != null)
            {
                // Lấy danh sách ID từ Cookie
                recentIds = Request.Cookies["RecentProducts"].Split(',').Select(int.Parse).ToList();
            }

            // Xóa ID hiện tại (để đưa lên đầu) và giới hạn 6 sản phẩm
            recentIds.Remove(id);
            recentIds.Insert(0, id);
            if (recentIds.Count > 6) recentIds = recentIds.Take(6).ToList();

            // Lưu lại Cookie (Hạn 30 ngày)
            Response.Cookies.Append("RecentProducts", string.Join(",", recentIds),
                new CookieOptions { Expires = DateTime.Now.AddDays(30) });

            // Lấy danh sách sản phẩm từ DB để hiển thị (Trừ sản phẩm đang xem)
            var viewedProducts = await _context.Products
                .Include(p => p.Images)
                .Where(p => recentIds.Contains(p.Id) && p.Id != id)
                .ToListAsync();

            // Sắp xếp đúng thứ tự đã xem
            viewedProducts = viewedProducts.OrderBy(p => recentIds.IndexOf(p.Id)).ToList();

            // Truyền sang View
            ViewBag.RecentlyViewed = viewedProducts;
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
        // API: Tìm kiếm gợi ý (Autocomplete)
        [HttpGet]
        public async Task<IActionResult> SearchJson(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var products = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive && (p.Name.Contains(term) || p.Description.Contains(term)))
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    image = p.Images != null && p.Images.Any() ? p.Images.First().ImageUrl : "/img/no-image.jpg"
                })
                .Take(5) // Gợi ý 5 sản phẩm
                .ToListAsync();

            return Json(products);
        }
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SubscribeStock(string email, int productId, string note)
        {
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Vui lòng nhập email!" });

            // Lưu vào Database (Ví dụ: Bảng StockSubscriptions)
            // Hoặc đơn giản là log ra console/gửi mail cho Admin

            // Demo trả về thành công
            return Json(new { success = true, message = "Đăng ký thành công! Chúng tôi sẽ báo khi có hàng." });
        }
    }
}
