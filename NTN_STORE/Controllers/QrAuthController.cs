using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTN_STORE.Models;
using NTN_STORE.Services;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NTN_STORE.Controllers
{
    public class QrAuthController : Controller
    {
        private readonly QrLoginService _qrService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public QrAuthController(QrLoginService qrService, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _qrService = qrService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Hàm lấy IP LAN của máy tính (Vd: 192.168.1.10)
        private string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "localhost"; // Fallback nếu không tìm thấy
        }

        [HttpGet]
        public IActionResult GetQrToken()
        {
            var token = _qrService.GenerateToken();

            // Lấy IP LAN thực tế
            string localIp = GetLocalIpAddress();

            // Lấy port hiện tại (lưu ý: bạn phải chạy http profile, không phải IIS Express để dễ config IP)
            var port = HttpContext.Connection.LocalPort;
            var scheme = HttpContext.Request.Scheme; // http hoặc https

            // Tạo URL với IP LAN (Ví dụ: https://192.168.1.5:7055/QrAuth/Confirm?token=...)
            // Lưu ý: Điện thoại có thể cảnh báo bảo mật nếu dùng HTTPS tự ký (self-signed) với IP.
            // Tốt nhất lúc test LAN nên dùng HTTP thường hoặc chấp nhận cảnh báo trên điện thoại.

            // Nếu đang chạy IIS Express, port có thể khác, ta lấy từ Request gốc nhưng thay Host
            var currentHost = HttpContext.Request.Host.Value; // localhost:7055
            var ipHost = currentHost.Replace("localhost", localIp).Replace("127.0.0.1", localIp);

            var confirmUrl = $"{scheme}://{ipHost}/QrAuth/Confirm?token={token}";

            return Json(new { token, confirmUrl, displayUrl = confirmUrl });
        }

        [HttpGet]
        public async Task<IActionResult> CheckLogin(string token)
        {
            var status = _qrService.GetTokenStatus(token);

            if (status == "Expired") return Json(new { success = false, message = "Expired" });
            if (status == "Pending") return Json(new { success = false, message = "Waiting" });

            // Đã có người quét
            var user = await _userManager.FindByIdAsync(status);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Json(new { success = true, redirectUrl = "/" });
            }
            return Json(new { success = false });
        }

        [Authorize]
        [HttpGet]
        public IActionResult Confirm(string token)
        {
            var userId = _userManager.GetUserId(User);
            var result = _qrService.ConfirmToken(token, userId);
            if (result) return View("ConfirmSuccess");
            return Content("Mã không hợp lệ hoặc đã hết hạn.");
        }
    }
}