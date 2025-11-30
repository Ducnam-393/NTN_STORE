using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using NTN_STORE.Models.ViewModels; // Nếu có dùng ViewModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class InventoryController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InventoryController(NTNStoreContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. DASHBOARD KHO (CÓ LỌC THEO HÃNG)
        public async Task<IActionResult> Index(int? brandId, string filter = "all")
        {
            var query = _context.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .Include(v => v.Product).ThenInclude(p => p.Category) // Thêm Category để hiển thị nếu cần
                .AsQueryable();

            // Lọc theo Hãng
            if (brandId.HasValue)
            {
                query = query.Where(v => v.Product.BrandId == brandId.Value);
            }

            // Lọc theo trạng thái tồn
            if (filter == "low") query = query.Where(v => v.Stock <= 5);
            else if (filter == "out") query = query.Where(v => v.Stock == 0);

            var variants = await query.OrderBy(v => v.Product.Brand.Name).ThenBy(v => v.Product.Name).ToListAsync();

            // Chuẩn bị dữ liệu View
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", brandId);
            ViewBag.CurrentBrandId = brandId;
            ViewBag.TotalStockValue = variants.Sum(v => v.Stock * v.Product.ImportPrice); // Tổng giá trị kho hiện tại
            ViewBag.LowStockCount = await _context.ProductVariants.CountAsync(v => v.Stock <= 5);

            return View(variants);
        }

        // ... (Giữ nguyên Action History) ...
        public async Task<IActionResult> History()
        {
            var logs = await _context.InventoryLogs
               .Include(l => l.Variant).ThenInclude(v => v.Product)
               .OrderByDescending(l => l.CreatedAt)
               .Take(100)
               .ToListAsync();
            return View(logs);
        }

        // 2. TẠO PHIẾU NHẬP (GET)
        public async Task<IActionResult> CreateImport()
        {
            // Load danh sách sản phẩm, nhóm theo Hãng để dễ tìm
            var variants = await _context.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .OrderBy(v => v.Product.Brand.Name)
                .ThenBy(v => v.Product.Name)
                .Select(v => new
                {
                    Id = v.Id,
                    Name = $"[{v.Product.Brand.Name}] {v.Product.Name} ({v.Color} - {v.Size})"
                })
                .ToListAsync();

            ViewBag.VariantList = new SelectList(variants, "Id", "Name");
            return View();
        }

        // --- API AJAX: LẤY THÔNG TIN TỒN KHO & GIÁ CŨ ---
        [HttpGet]
        public async Task<IActionResult> GetVariantInfo(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null) return NotFound();

            return Json(new
            {
                currentStock = variant.Stock,
                lastImportPrice = variant.Product.ImportPrice,
                sku = $"{variant.Product.Name} - {variant.Color}/{variant.Size}"
            });
        }

        // 3. TẠO PHIẾU NHẬP (POST) - GIỮ NGUYÊN LOGIC CŨ
        [HttpPost]
        public async Task<IActionResult> CreateImport(List<int> variantIds, List<int> quantities, List<decimal> prices, string note)
        {
            var user = await _userManager.GetUserAsync(User);
            var importNote = new StockImport
            {
                Code = $"IMP-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                UserId = user.Id,
                CreatedAt = DateTime.Now,
                Note = note,
                Details = new List<StockImportDetail>()
            };

            decimal totalCost = 0;

            for (int i = 0; i < variantIds.Count; i++)
            {
                int vId = variantIds[i];
                int qty = quantities[i];
                decimal price = prices[i];

                if (qty > 0)
                {
                    var detail = new StockImportDetail { ProductVariantId = vId, Quantity = qty, UnitPrice = price };
                    importNote.Details.Add(detail);
                    totalCost += (qty * price);

                    var variant = await _context.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == vId);
                    if (variant != null)
                    {
                        variant.Product.ImportPrice = price; // Cập nhật giá mới nhất
                        variant.Stock += qty; // Cộng kho
                        _context.ProductVariants.Update(variant);

                        // Ghi Log
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductVariantId = vId,
                            Action = "Import",
                            ChangeAmount = qty,
                            RemainingStock = variant.Stock,
                            ReferenceCode = importNote.Code,
                            UserId = user.UserName,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            importNote.TotalCost = totalCost;
            _context.StockImports.Add(importNote);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã nhập kho thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}