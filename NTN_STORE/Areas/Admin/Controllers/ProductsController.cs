using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System.IO;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Để lưu file ảnh

        public ProductsController(NTNStoreContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH (Đã làm đẹp ở bước trước, giữ nguyên hoặc cập nhật nếu cần)
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

        // 2. TẠO MỚI (GET)
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name");

            var vm = new ProductFormViewModel();
            // Tạo sẵn 1 dòng variant mẫu
            vm.Variants.Add(new ProductVariantViewModel { Color = "Mặc định", Size = "39", Stock = 10 });
            return View(vm);
        }

        // 3. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. Lưu Sản phẩm chính
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImportPrice = model.ImportPrice,
                    OriginalPrice = model.OriginalPrice,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    CreatedAt = DateTime.Now
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // Lưu để lấy ID

                // B. Lưu Ảnh
                if (model.ImageFiles != null)
                {
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    foreach (var file in model.ImageFiles)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(folderPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        _context.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = "/img/products/" + fileName });
                    }
                }

                // C. Lưu Biến thể
                if (model.Variants != null)
                {
                    foreach (var v in model.Variants)
                    {
                        if (!string.IsNullOrEmpty(v.Color) && !string.IsNullOrEmpty(v.Size))
                        {
                            _context.ProductVariants.Add(new ProductVariant
                            {
                                ProductId = product.Id,
                                Color = v.Color,
                                Size = v.Size,
                                Stock = v.Stock
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại dropdown
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", model.BrandId);
            return View(model);
        }

        // 4. CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImportPrice = product.ImportPrice,
                OriginalPrice = product.OriginalPrice,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                ExistingImages = product.Images.ToList(),
                Variants = product.Variants.Select(v => new ProductVariantViewModel
                {
                    Id = v.Id,
                    Color = v.Color,
                    Size = v.Size,
                    Stock = v.Stock
                }).ToList()
            };

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(vm);
        }

        // 5. CHỈNH SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Update info
                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.ImportPrice = model.ImportPrice;
                product.OriginalPrice = model.OriginalPrice;
                product.CategoryId = model.CategoryId;
                product.BrandId = model.BrandId;
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;

                // Xử lý xóa ảnh cũ
                if (model.ImagesToDelete != null)
                {
                    var imagesToDelete = await _context.ProductImages.Where(i => model.ImagesToDelete.Contains(i.Id)).ToListAsync();
                    _context.ProductImages.RemoveRange(imagesToDelete);
                    // (Optional: Xóa file vật lý trong wwwroot nếu cần)
                }

                // Xử lý thêm ảnh mới
                if (model.ImageFiles != null)
                {
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "img/products");
                    foreach (var file in model.ImageFiles)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(folderPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        _context.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = "/img/products/" + fileName });
                    }
                }

                // Xử lý Variants (Cập nhật / Thêm mới / Xóa)
                if (model.Variants != null)
                {
                    foreach (var v in model.Variants)
                    {
                        if (v.IsDeleted && v.Id > 0) // Xóa
                        {
                            var variantToDelete = await _context.ProductVariants.FindAsync(v.Id);
                            if (variantToDelete != null) _context.ProductVariants.Remove(variantToDelete);
                        }
                        else if (v.Id > 0) // Cập nhật
                        {
                            var variantToUpdate = await _context.ProductVariants.FindAsync(v.Id);
                            if (variantToUpdate != null)
                            {
                                variantToUpdate.Color = v.Color;
                                variantToUpdate.Size = v.Size;
                                variantToUpdate.Stock = v.Stock;
                            }
                        }
                        else if (!v.IsDeleted) // Thêm mới
                        {
                            _context.ProductVariants.Add(new ProductVariant
                            {
                                ProductId = id,
                                Color = v.Color,
                                Size = v.Size,
                                Stock = v.Stock
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "Name", model.BrandId);
            return View(model);
        }

        // Delete Action (Giữ nguyên hoặc làm đẹp view)
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}