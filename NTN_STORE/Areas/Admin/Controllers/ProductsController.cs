using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Bắt buộc phải đăng nhập để vào controller này
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

        // (Chúng ta sẽ thêm code cho Create, Edit, Delete ở đây sau)
    }
}
