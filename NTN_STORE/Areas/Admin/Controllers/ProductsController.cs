using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
                Variants = new List<ProductVariant> { new ProductVariant { Stock = 100 } },
                Categories = await LoadCategories(),
                Brands = await LoadBrands()
            };

            return View(vm);
        }

        // =================== CREATE POST ===================
        // =================== CREATE POST ===================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel vm)
        {
            var variantErrors = ModelState.Keys.Where(k => k.StartsWith("Variants")).ToList();
            foreach (var key in variantErrors)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                vm.Categories = await LoadCategories();
                vm.Brands = await LoadBrands();
                return View(vm);
            }

            // 2. Lưu Product trước để lấy ID
            vm.Product.CreatedAt = DateTime.Now;
            _context.Products.Add(vm.Product);
            await _context.SaveChangesAsync();

            // 3. Xử lý danh sách Variants thủ công
            if (vm.Variants != null && vm.Variants.Any())
            {
                foreach (var variant in vm.Variants)
                {
                    // Chỉ lưu những dòng có dữ liệu (Size và Color không rỗng)
                    if (!string.IsNullOrWhiteSpace(variant.Size) && !string.IsNullOrWhiteSpace(variant.Color))
                    {
                        variant.Id = 0; // Đảm bảo là thêm mới
                        variant.ProductId = vm.Product.Id;
                        _context.ProductVariants.Add(variant);
                    }
                }
            }

            // 4. Lưu ảnh (nếu có)
            if (vm.ImageFile != null)
            {
                string url = await SaveImage(vm.ImageFile);
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = vm.Product.Id,
                    ImageUrl = url
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =================== EDIT GET ===================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new ProductFormViewModel
            {
                Product = product,
                Variants = product.Variants.ToList(),
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
            // 1. SỬA LỖI: Xóa toàn bộ lỗi validation liên quan đến Variants
            var variantErrors = ModelState.Keys.Where(k => k.StartsWith("Variants")).ToList();
            foreach (var key in variantErrors)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                vm.Categories = await LoadCategories();
                vm.Brands = await LoadBrands();
                return View(vm);
            }

            var productInDb = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == vm.Product.Id);

            if (productInDb == null) return NotFound();

            // 2. Cập nhật thông tin Product
            productInDb.Name = vm.Product.Name;
            productInDb.Description = vm.Product.Description;
            productInDb.Price = vm.Product.Price;
            productInDb.OriginalPrice = vm.Product.OriginalPrice;
            productInDb.CategoryId = vm.Product.CategoryId;
            productInDb.BrandId = vm.Product.BrandId;
            productInDb.IsActive = vm.Product.IsActive;
            productInDb.IsFeatured = vm.Product.IsFeatured;
            // 3. Xử lý Variants
            if (vm.Variants != null)
            {
                // A. Lấy danh sách ID gửi lên
                var incomingIds = vm.Variants.Where(v => v.Id > 0).Select(v => v.Id).ToList();

                // B. Xóa variant cũ không còn tồn tại
                var variantsToDelete = productInDb.Variants
                    .Where(v => !incomingIds.Contains(v.Id))
                    .ToList();

                if (variantsToDelete.Any())
                    _context.ProductVariants.RemoveRange(variantsToDelete);

                // C. Thêm mới hoặc Cập nhật
                foreach (var v in vm.Variants)
                {
                    // Bỏ qua dòng trống
                    if (string.IsNullOrWhiteSpace(v.Size) || string.IsNullOrWhiteSpace(v.Color)) continue;

                    if (v.Id == 0)
                    {
                        var newVariant = new ProductVariant
                        {
                            ProductId = productInDb.Id,
                            Size = v.Size,
                            Color = v.Color,
                            Stock = v.Stock
                        };
                        _context.ProductVariants.Add(newVariant);
                    }
                    else
                    {
                        var existingVariant = productInDb.Variants.FirstOrDefault(x => x.Id == v.Id);
                        if (existingVariant != null)
                        {
                            existingVariant.Size = v.Size;
                            existingVariant.Color = v.Color;
                            existingVariant.Stock = v.Stock;
                            _context.Entry(existingVariant).State = EntityState.Modified;
                        }
                    }
                }
            }

            // 4. Xử lý ảnh
            if (vm.ImageFile != null)
            {
                foreach (var img in productInDb.Images)
                {
                    DeleteImageFile(img.ImageUrl);
                }
                _context.ProductImages.RemoveRange(productInDb.Images);

                string url = await SaveImage(vm.ImageFile);
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productInDb.Id,
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
