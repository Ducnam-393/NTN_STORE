using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly NTNStoreContext _context;

        public CouponsController(NTNStoreContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH MÃ
        public async Task<IActionResult> Index()
        {
            return View(await _context.Coupons.OrderByDescending(c => c.Id).ToListAsync());
        }

        // 2. TẠO MÃ MỚI (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. TẠO MÃ MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng mã
                if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
                {
                    ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
                    return View(coupon);
                }

                _context.Add(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // 4. XÓA MÃ
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. KHÓA/MỞ KHÓA NHANH (AJAX hoặc Action thường)
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}