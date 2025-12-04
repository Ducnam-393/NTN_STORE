using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách địa chỉ
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
            return View(addresses);
        }

        // 2. Tạo mới (GET + POST)
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(UserAddress model)
        {
            var userId = _userManager.GetUserId(User);
            model.UserId = userId;
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            if (ModelState.IsValid)
            {
                // Nếu đây là địa chỉ đầu tiên, set mặc định luôn
                if (!await _context.UserAddresses.AnyAsync(a => a.UserId == userId))
                {
                    model.IsDefault = true;
                }
                else if (model.IsDefault)
                {
                    // Nếu user chọn mặc định -> Bỏ mặc định các cái cũ
                    var defaults = await _context.UserAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                    defaults.ForEach(a => a.IsDefault = false);
                }

                _context.UserAddresses.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 3. Sửa (GET + POST)
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null) return NotFound();
            return View(address);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserAddress model)
        {
            if (id != model.Id) return NotFound();
            var userId = _userManager.GetUserId(User);

            // --- FIX LỖI LOAD LẠI TRANG ---
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            // -----------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAddress = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
                    if (existingAddress == null) return NotFound();

                    existingAddress.FullName = model.FullName;
                    existingAddress.PhoneNumber = model.PhoneNumber;
                    existingAddress.Address = model.Address;

                    // Chỉ cập nhật Tỉnh/Huyện/Xã nếu người dùng có chọn mới (nếu không input sẽ rỗng hoặc giữ nguyên tùy logic view)
                    if (!string.IsNullOrEmpty(model.Province)) existingAddress.Province = model.Province;
                    if (!string.IsNullOrEmpty(model.District)) existingAddress.District = model.District;
                    if (!string.IsNullOrEmpty(model.Ward)) existingAddress.Ward = model.Ward;

                    if (model.IsDefault)
                    {
                        var defaults = await _context.UserAddresses.Where(a => a.UserId == userId && a.Id != id && a.IsDefault).ToListAsync();
                        defaults.ForEach(a => a.IsDefault = false);
                        existingAddress.IsDefault = true;
                    }
                    else
                    {
                        existingAddress.IsDefault = false;
                    }

                    _context.Update(existingAddress);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserAddressExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
        private bool UserAddressExists(int id)
        {
            return _context.UserAddresses.Any(e => e.Id == id);
        }
        // 4.1. Xóa(GET) - Hiển thị trang xác nhận
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (address == null) return NotFound();

            return View(address);
        }

        // 4.2. Xóa (POST) - Thực hiện xóa trong DB
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address != null)
            {
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}