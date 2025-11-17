using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public CustomersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Admin/Customers
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/Customers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy danh sách roles của user (ví dụ: "Admin", "Customer")
            ViewBag.Roles = await _userManager.GetRolesAsync(user);

            return View(user);
        }

        // Bạn có thể thêm các hành động để quản lý Roles (thêm/xóa user khỏi Role)
    }
}