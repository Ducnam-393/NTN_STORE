using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            using (var context = new NTNStoreContext(
                serviceProvider.GetRequiredService<DbContextOptions<NTNStoreContext>>()))
            {
                await context.Database.EnsureCreatedAsync();

                // 1. Seed Categories (SỬA LẠI NHƯ SAU)
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Sneaker", ImageUrl = "~/img/cat-sneaker.png" },
                        new Category { Name = "Giày chạy bộ", ImageUrl = "~/img/cat-running.png" },
                        new Category { Name = "Giày thể thao", ImageUrl = "~/img/cat-sport.jpg" },
                        new Category { Name = "Giày bóng rổ", ImageUrl = "~/img/cat-basketball.jpg" },
                        new Category { Name = "Giày đá bóng", ImageUrl = "~/img/cat-football.jpg" },
                        new Category { Name = "Boot thời trang", ImageUrl = "~/img/cat-boot.png" },
                        new Category { Name = "Sandal", ImageUrl = "~/img/cat-sandal.jpg" },
                        new Category { Name = "Dép / Slides", ImageUrl = "~/img/cat-slide.jpg" }
                    );
                }

                // 2. Seed Brands (SỬA LẠI NHƯ SAU)
                if (!context.Brands.Any())
                {
                    context.Brands.AddRange(
                        new Brand { Name = "Nike", ImageUrl = "~/img/brand-nike.jpg" },
                        new Brand { Name = "Adidas", ImageUrl = "~/img/brand-adidas.jpg" },
                        new Brand { Name = "Puma", ImageUrl = "~/img/brand-puma.jpg" },
                        new Brand { Name = "MLB", ImageUrl = "~/img/brand-mlb.png" },
                        new Brand { Name = "Vans", ImageUrl = "~/img/brand-vans.jpg" },
                        new Brand { Name = "Converse", ImageUrl = "~/img/brand-converse.png" }
                    );
                }

                // (Các phần seed data khác)

                await context.SaveChangesAsync();
            }
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