using Microsoft.AspNetCore.Identity;
using NTN_STORE.Models; // Giả sử RoleManager, UserManager ở đây

namespace NTN_STORE.Data
{
    public static class SeedData
    {
        // Tên các Role
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_CUSTOMER = "Customer";

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Tạo các Roles
            if (!await roleManager.RoleExistsAsync(ROLE_ADMIN))
            {
                await roleManager.CreateAsync(new IdentityRole(ROLE_ADMIN));
            }
            if (!await roleManager.RoleExistsAsync(ROLE_CUSTOMER))
            {
                await roleManager.CreateAsync(new IdentityRole(ROLE_CUSTOMER));
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@ntnstore.com";
            var adminPass = "123456"; // (Hãy đổi mật khẩu này sau)

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Xác thực email luôn
                };

                // Tạo user với mật khẩu
                var result = await userManager.CreateAsync(adminUser, adminPass);

                if (result.Succeeded)
                {
                    // Gán Role "Admin" cho user này
                    await userManager.AddToRoleAsync(adminUser, ROLE_ADMIN);
                }
            }
        }
    }
}