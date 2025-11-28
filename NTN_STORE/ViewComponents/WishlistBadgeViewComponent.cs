using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.ViewComponents
{
    public class WishlistBadgeViewComponent : ViewComponent
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public WishlistBadgeViewComponent(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View(0);
            }

            var userId = _userManager.GetUserId(HttpContext.User);
            var count = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .CountAsync();

            return View(count);
        }
    }
}