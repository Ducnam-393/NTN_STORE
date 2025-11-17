using Microsoft.AspNetCore.Mvc;
using NTN_STORE.Models.ViewModels;

namespace NTN_STORE.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        // POST: /Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // LƯU Ý: Đây là nơi bạn sẽ viết code để gửi Email (dùng MailKit, SendGrid...)
                // Vì chúng ta chưa cài đặt dịch vụ Email, ta sẽ tạm thời
                // trả về một thông báo thành công.

                ViewBag.SuccessMessage = "Gửi tin nhắn thành công! Chúng tôi sẽ sớm liên hệ với bạn.";

                // Xóa model để form được reset
                return View(new ContactViewModel());
            }

            // Nếu model không hợp lệ, trả về view với các lỗi
            return View(model);
        }
    }
}
