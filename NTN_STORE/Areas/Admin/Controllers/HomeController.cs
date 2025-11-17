using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")] // Chỉ định đây là Controller thuộc Area "Admin"
    [Authorize]
    public class HomeController : Controller
    {
        // GET: /Admin/Home/Index hoặc /Admin
        public IActionResult Index()
        {
            return View();
        }
    }
}
