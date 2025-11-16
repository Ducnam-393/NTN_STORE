using Microsoft.AspNetCore.Mvc;

namespace NTN_STORE.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
