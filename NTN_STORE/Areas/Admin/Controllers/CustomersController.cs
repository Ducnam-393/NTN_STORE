using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin cao nhất mới được vào
    public class CustomersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly NTNStoreContext _context;

        public CustomersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, NTNStoreContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // 1. DANH SÁCH KHÁCH HÀNG
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userVMs = new List<UserViewModel>();

            foreach (var user in users)
            {
                var vm = new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    // Kiểm tra xem user có bị khóa không
                    IsLocked = await _userManager.IsLockedOutAsync(user),
                    Roles = await _userManager.GetRolesAsync(user)
                };
                userVMs.Add(vm);
            }

            return View(userVMs);
        }

        // 2. CHI TIẾT & LỊCH SỬ MUA HÀNG
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Lấy lịch sử đơn hàng của khách
            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Lấy Roles hiện tại và Tất cả Roles để hiển thị dropdown
            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var vm = new UserDetailViewModel
            {
                User = user,
                Orders = orders,
                Roles = userRoles,
                AllRoles = allRoles
            };

            return View(vm);
        }

        // 3. KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Nếu đang bị khóa -> Mở khóa
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Đã mở khóa tài khoản {user.UserName}.";
            }
            else
            {
                // Khóa vĩnh viễn (hoặc 100 năm)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["Warning"] = $"Đã khóa tài khoản {user.UserName}.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. CẬP NHẬT QUYỀN (Role)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains(role))
            {
                // Nếu đã có -> Xóa quyền (Gỡ bỏ Admin/Staff)
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["Warning"] = $"Đã gỡ quyền {role} khỏi user {user.UserName}.";
            }
            else
            {
                // Nếu chưa có -> Thêm quyền
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"Đã cấp quyền {role} cho user {user.UserName}.";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}