using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Thêm thư viện này
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace NTN_STORE.Models
{
    public class NTNStoreContext : IdentityDbContext<IdentityUser>
    {
        public NTNStoreContext(DbContextOptions<NTNStoreContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<StoreLocation> Stores { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Quan hệ nhiều ảnh - 1 sản phẩm
            modelBuilder.Entity<ProductImage>()
                .HasOne(p => p.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(p => p.ProductId);

            // Quan hệ variant
            modelBuilder.Entity<ProductVariant>()
                .HasOne(p => p.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(p => p.ProductId);
            // Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.OriginalPrice)
                .HasPrecision(18, 2);

            // OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            // Promotion
            modelBuilder.Entity<Promotion>()
                .Property(p => p.DiscountPercent)
                .HasPrecision(5, 2);   // Ví dụ: 12.50%

            // (OPTIONAL) Các decimal khác bạn muốn chuẩn hóa
            modelBuilder.Entity<OrderDetail>()
    .HasOne(od => od.Product)
    .WithMany()
    .HasForeignKey(od => od.ProductId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Variant)
                .WithMany()
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // <-- Sửa 1

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Variant)
                .WithMany()
                .HasForeignKey(ci => ci.VariantId)
                .OnDelete(DeleteBehavior.Restrict); // <-- Sửa 2
        }
    }
}
