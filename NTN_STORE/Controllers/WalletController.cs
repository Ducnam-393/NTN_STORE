using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // 1. Lấy tất cả mã giảm giá đang kích hoạt trong hệ thống
            var allCoupons = await _context.Coupons
                .Where(c => c.IsActive)
                .OrderBy(c => c.ExpiryDate)
                .ToListAsync();

            // 2. Lấy danh sách mã người dùng ĐÃ SỬ DỤNG trong các đơn hàng cũ
            // (Giả sử CouponCode được lưu trong bảng Order khi đặt hàng)
            var usedCouponCodes = await _context.Orders
                .Where(o => o.UserId == userId && !string.IsNullOrEmpty(o.CouponCode))
                .Select(o => o.CouponCode)
                .Distinct()
                .ToListAsync();

            // 3. Phân loại Voucher
            var viewModel = new WalletViewModel
            {
                // Loại 1: Có thể sử dụng (Chưa hết hạn, Chưa dùng, Còn lượt dùng chung)
                UsableCoupons = allCoupons.Where(c =>
                    c.ExpiryDate >= DateTime.Now &&
                    !usedCouponCodes.Contains(c.Code) &&
                    (c.UsageLimit == 0 || c.UsedCount < c.UsageLimit)
                ).ToList(),

                // Loại 2: Đã dùng hoặc Hết hiệu lực
                UsedOrExpiredCoupons = allCoupons.Where(c =>
                    usedCouponCodes.Contains(c.Code) ||
                    c.ExpiryDate < DateTime.Now ||
                    (c.UsageLimit > 0 && c.UsedCount >= c.UsageLimit)
                ).ToList()
            };

            // Loại 3 (Lọc phụ): Sắp hết hạn (Còn hạn nhưng < 3 ngày)
            viewModel.ExpiringSoonCoupons = viewModel.UsableCoupons
                .Where(c => (c.ExpiryDate - DateTime.Now).TotalDays <= 3)
                .ToList();

            return View(viewModel);
        }
    }

    // ViewModel nội bộ cho View này
    public class WalletViewModel
    {
        public List<Coupon> UsableCoupons { get; set; }
        public List<Coupon> ExpiringSoonCoupons { get; set; }
        public List<Coupon> UsedOrExpiredCoupons { get; set; }
    }
}