using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(NTNStoreContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =================== INDEX ===================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        // =================== CREATE GET ===================
        public async Task<IActionResult> Create()
        {
            var vm = new ProductFormViewModel
            {
                Product = new Product { IsActive = true },
                Categories = await LoadCategories(),
                Brands = await LoadBrands()
            };

            return View(vm);
        }

        // =================== CREATE POST ===================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = await LoadCategories();
                vm.Brands = await LoadBrands();
                return View(vm);
            }

            // Lưu product
            _context.Products.Add(vm.Product);
            await _context.SaveChangesAsync();

            // Lưu ảnh (nếu có)
            if (vm.ImageFile != null)
            {
                string url = await SaveImage(vm.ImageFile);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = vm.Product.Id,
                    ImageUrl = url
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =================== EDIT GET ===================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new ProductFormViewModel
            {
                Product = product,
                Categories = await LoadCategories(),
                Brands = await LoadBrands()
            };

            return View(vm);
        }

        // =================== EDIT POST ===================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = await LoadCategories();
                vm.Brands = await LoadBrands();
                return View(vm);
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == vm.Product.Id);

            if (product == null) return NotFound();

            // Gán dữ liệu mới
            product.Name = vm.Product.Name;
            product.Description = vm.Product.Description;
            product.Price = vm.Product.Price;
            product.OriginalPrice = vm.Product.OriginalPrice;
            product.CategoryId = vm.Product.CategoryId;
            product.BrandId = vm.Product.BrandId;
            product.IsActive = vm.Product.IsActive;

            // Nếu có ảnh mới → xóa ảnh cũ + lưu ảnh mới
            if (vm.ImageFile != null)
            {
                // Xóa file ảnh cũ
                foreach (var img in product.Images)
                {
                    DeleteImageFile(img.ImageUrl);
                }

                // Xóa DB ảnh cũ
                _context.ProductImages.RemoveRange(product.Images);
                await _context.SaveChangesAsync();

                // Thêm ảnh mới
                string url = await SaveImage(vm.ImageFile);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = url
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =================== DELETE GET ===================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // =================== DELETE POST ===================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Xóa file ảnh
            foreach (var img in product.Images)
            {
                DeleteImageFile(img.ImageUrl);
            }

            // Xóa ảnh trong DB
            _context.ProductImages.RemoveRange(product.Images);

            // Xóa product
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==========================================================
        // =============== HÀM PHỤ TRỢ =============================
        // ==========================================================

        private async Task<IEnumerable<SelectListItem>> LoadCategories()
        {
            return await _context.Categories
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        private async Task<IEnumerable<SelectListItem>> LoadBrands()
        {
            return await _context.Brands
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "img", "products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(folder, fileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return "/img/products/" + fileName;
        }

        private void DeleteImageFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            string filePath = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
