using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartBadgeViewComponent(NTNStoreContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Nếu chưa đăng nhập -> 0
            if (!User.Identity.IsAuthenticated)
            {
                return View(0);
            }

            var userId = _userManager.GetUserId(HttpContext.User);

            // Đếm tổng số lượng item trong giỏ
            // Lưu ý: Bạn có thể đếm số dòng (Count) hoặc tổng số lượng sản phẩm (Sum(Quantity))
            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .CountAsync();

            return View(count);
        }
    }
}
