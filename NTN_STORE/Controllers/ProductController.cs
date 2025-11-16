using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Data;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System;

namespace NTN_STORE.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
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
        public IActionResult Detail(int id)
        {
            var product = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            var images = _context.ProductImages
                .Where(x => x.ProductId == id)
                .ToList();

            var variants = _context.ProductVariants
                .Where(x => x.ProductId == id)
                .ToList();

            var relatedProducts = _context.Products
    .Include(p => p.Images)
    .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
    .Take(4)
    .ToList();

            var vm = new ProductDetailViewModel
            {
                Product = product,
                Images = images,
                Variants = variants,
                RelatedProducts = relatedProducts
            };

            return View(vm);
        }
    }
}
