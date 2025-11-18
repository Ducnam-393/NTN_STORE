using Microsoft.AspNetCore.Hosting; // Cần để lưu ảnh
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Inject môi trường

        public ProductsController(NTNStoreContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.Id) // Sắp xếp mới nhất lên đầu
                .ToListAsync();
            return View(products);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductFormViewModel
            {
                Product = new Product { IsActive = true },
                Categories = await GetCategoriesSelectList(),
                Brands = await GetBrandsSelectList()
            };
            return View(viewModel);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Xử lý Upload ảnh
                if (viewModel.ImageFile != null)
                {
                    // 1. Tạo tên file unique
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.ImageFile.FileName);
                    // 2. Xác định đường dẫn lưu: wwwroot/img/products
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "products");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    // 3. Copy file
                    using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(fileStream);
                    }

                    // 4. Gán đường dẫn ảnh vào ProductImage
                    if (viewModel.Product.Images == null) viewModel.Product.Images = new List<ProductImage>();
                    viewModel.Product.Images.Add(new ProductImage
                    {
                        ImageUrl = "/img/products/" + fileName,
                        Product = viewModel.Product
                    });
                }

                _context.Add(viewModel.Product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi validation, load lại danh sách chọn
            viewModel.Categories = await GetCategoriesSelectList();
            viewModel.Brands = await GetBrandsSelectList();
            return View(viewModel);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Images) // Load ảnh cũ
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var viewModel = new ProductFormViewModel
            {
                Product = product,
                Categories = await GetCategoriesSelectList(),
                Brands = await GetBrandsSelectList()
            };
            return View(viewModel);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel viewModel)
        {
            if (id != viewModel.Product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload ảnh mới khi Edit
                    if (viewModel.ImageFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.ImageFile.FileName);
                        string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "products");

                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await viewModel.ImageFile.CopyToAsync(fileStream);
                        }

                        // Thêm ảnh mới
                        var newImage = new ProductImage { ImageUrl = "/img/products/" + fileName, ProductId = id };
                        _context.Add(newImage); // Lưu trực tiếp ảnh mới vào bảng ProductImage
                    }

                    _context.Update(viewModel.Product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            viewModel.Categories = await GetCategoriesSelectList();
            viewModel.Brands = await GetBrandsSelectList();
            return View(viewModel);
        }

        // Các hàm hỗ trợ
        private bool ProductExists(int id) => _context.Products.Any(e => e.Id == id);

        private async Task<IEnumerable<SelectListItem>> GetCategoriesSelectList()
        {
            return (await _context.Categories.ToListAsync()).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetBrandsSelectList()
        {
            return (await _context.Brands.ToListAsync()).Select(b => new SelectListItem
            {
                Text = b.Name,
                Value = b.Id.ToString()
            });
        }
    }
}