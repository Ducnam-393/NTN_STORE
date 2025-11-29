using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SlidersController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly IWebHostEnvironment _env;

        public SlidersController(NTNStoreContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. INDEX
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sliders.OrderBy(s => s.DisplayOrder).ToListAsync());
        }

        // 2. CREATE (GET)
        public IActionResult Create() => View();

        // 3. CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider slider, IFormFile? ImageFile)
        {
            if (ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(_env.WebRootPath, "img/sliders", fileName);

                if (!Directory.Exists(Path.Combine(_env.WebRootPath, "img/sliders")))
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "img/sliders"));

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                slider.ImageUrl = "/img/sliders/" + fileName;
            }

            if (string.IsNullOrEmpty(slider.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Vui lòng chọn ảnh banner");
                return View(slider);
            }

            _context.Add(slider);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 4. EDIT (GET) - MỚI
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null) return NotFound();
            return View(slider);
        }

        // 5. EDIT (POST) - MỚI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Slider slider, IFormFile? ImageFile)
        {
            if (id != slider.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu có upload ảnh mới
                    if (ImageFile != null)
                    {
                        // 1. Xóa ảnh cũ nếu cần (Optional)
                        // if (!string.IsNullOrEmpty(slider.ImageUrl)) ...

                        // 2. Lưu ảnh mới
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        string path = Path.Combine(_env.WebRootPath, "img/sliders", fileName);
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        slider.ImageUrl = "/img/sliders/" + fileName;
                    }
                    else
                    {
                        // Nếu không chọn ảnh mới, giữ nguyên ảnh cũ từ DB (cần truy vấn lại hoặc dùng AsNoTracking)
                        // Cách đơn giản nhất: Dùng Hidden Input ở View để giữ ImageUrl cũ
                    }

                    _context.Update(slider);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(slider.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(slider);
        }

        // 6. DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var slider = await _context.Sliders.FindAsync(id);
            if (slider != null)
            {
                _context.Sliders.Remove(slider);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(int id)
        {
            return _context.Sliders.Any(e => e.Id == id);
        }
    }
}