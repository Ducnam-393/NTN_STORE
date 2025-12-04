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
using ClosedXML.Excel;
using System.IO;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class InventoryController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(NTNStoreContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. DASHBOARD KHO (CÓ LỌC THEO HÃNG)
        public async Task<IActionResult> Index(int? brandId, string filter = "all", int page = 1)
        {
            int pageSize = 20; // Hiển thị 20 dòng/trang

            var query = _context.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .AsQueryable();

            // --- Lọc ---
            if (brandId.HasValue)
            {
                query = query.Where(v => v.Product.BrandId == brandId.Value);
            }
            if (filter == "low") query = query.Where(v => v.Stock <= 5);
            else if (filter == "out") query = query.Where(v => v.Stock == 0);

            // --- Thống kê (Tính trên toàn bộ kết quả lọc) ---
            // Lưu ý: Phải tính trước khi phân trang
            var totalStockValue = await query.SumAsync(v => v.Stock * v.Product.ImportPrice);
            var lowStockCount = await query.CountAsync(v => v.Stock <= 5);

            // --- Phân trang ---
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var variants = await query
                .OrderBy(v => v.Product.Brand.Name).ThenBy(v => v.Product.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu ra View
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", brandId);
            ViewBag.CurrentBrandId = brandId;
            ViewBag.CurrentFilter = filter;
            ViewBag.TotalStockValue = totalStockValue;
            ViewBag.LowStockCount = lowStockCount;

            // Dữ liệu phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(variants);
        }
        // 2. LỊCH SỬ KHO (CÓ PHÂN TRANG)
        public async Task<IActionResult> History(int page = 1)
        {
            int pageSize = 50; // Lịch sử thì hiện nhiều hơn chút

            var query = _context.InventoryLogs
                .Include(l => l.Variant).ThenInclude(v => v.Product)
                .OrderByDescending(l => l.CreatedAt);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

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
        // 3. XUẤT EXCEL TỒN KHO (ACTION MỚI)
        public async Task<IActionResult> ExportToExcel()
        {
            // Lấy toàn bộ dữ liệu kho hiện tại
            var variants = await _context.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .OrderBy(v => v.Product.Brand.Name).ThenBy(v => v.Product.Name)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Tồn Kho");

                // Header
                worksheet.Cell(1, 1).Value = "BÁO CÁO TỒN KHO";
                worksheet.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

                // Cột
                worksheet.Cell(4, 1).Value = "Thương hiệu";
                worksheet.Cell(4, 2).Value = "Tên sản phẩm";
                worksheet.Cell(4, 3).Value = "Màu sắc";
                worksheet.Cell(4, 4).Value = "Kích thước";
                worksheet.Cell(4, 5).Value = "Giá vốn (VNĐ)";
                worksheet.Cell(4, 6).Value = "Tồn kho";
                worksheet.Cell(4, 7).Value = "Thành tiền tồn";

                var headerRow = worksheet.Range("A4:G4");
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Dữ liệu
                int row = 5;
                foreach (var item in variants)
                {
                    worksheet.Cell(row, 1).Value = item.Product.Brand?.Name;
                    worksheet.Cell(row, 2).Value = item.Product.Name;
                    worksheet.Cell(row, 3).Value = item.Color;
                    worksheet.Cell(row, 4).Value = item.Size;
                    worksheet.Cell(row, 5).Value = item.Product.ImportPrice;
                    worksheet.Cell(row, 6).Value = item.Stock;
                    worksheet.Cell(row, 7).FormulaA1 = $"=E{row}*F{row}"; // Công thức Excel

                    // Cảnh báo nếu tồn thấp
                    if (item.Stock <= 5) worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TonKho_{DateTime.Now:ddMMyy}.xlsx");
                }
            }
        }
    }

}