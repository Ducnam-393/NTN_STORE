using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly NTNStoreContext _context;

        public ProductsController(NTNStoreContext context)
        {
            _context = context;
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sản phẩm, bao gồm cả thông tin Danh mục và Thương hiệu
            var products = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Brand)
                                        .ToListAsync();
            return View(products);
        }

        // (Chúng ta sẽ thêm code cho Create, Edit, Delete)
        public async Task<IActionResult> Create()
        {
            // Tạo ViewModel và tải danh sách Categories/Brands
            var viewModel = new ProductFormViewModel
            {
                Product = new Product(), // Khởi tạo sản phẩm mới
                Categories = (await _context.Categories.ToListAsync()).Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                Brands = (await _context.Brands.ToListAsync()).Select(b => new SelectListItem
                {
                    Text = b.Name,
                    Value = b.Id.ToString()
                })
            };

            return View(viewModel);
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Thêm sản phẩm từ ViewModel vào Context
                _context.Add(viewModel.Product);
                await _context.SaveChangesAsync();

                // (Tùy chọn: Thêm TempData để báo thành công)
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";

                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, tải lại danh sách
            viewModel.Categories = (await _context.Categories.ToListAsync()).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
            viewModel.Brands = (await _context.Brands.ToListAsync()).Select(b => new SelectListItem
            {
                Text = b.Name,
                Value = b.Id.ToString()
            });

            return View(viewModel); // Trả về form với lỗi
        }
    }
}
