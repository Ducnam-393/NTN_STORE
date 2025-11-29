using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;

namespace NTN_STORE.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BlogController : Controller
    {
        private readonly NTNStoreContext _context;
        private readonly IWebHostEnvironment _env;

        public BlogController(NTNStoreContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. Danh sách bài viết
        public async Task<IActionResult> Index()
        {
            return View(await _context.BlogPosts.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        // 2. Tạo bài viết (GET)
        public IActionResult Create() => View();

        // 3. Tạo bài viết (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost post, IFormFile? ThumbnailFile)
        {
            if (ModelState.IsValid)
            {
                if (ThumbnailFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                    string path = Path.Combine(_env.WebRootPath, "img/blog");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await ThumbnailFile.CopyToAsync(stream);
                    }
                    post.ThumbnailUrl = "/img/blog/" + fileName;
                }

                post.CreatedAt = DateTime.Now;
                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // 4. Chỉnh sửa (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        // 5. Chỉnh sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost post, IFormFile? ThumbnailFile)
        {
            if (id != post.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (ThumbnailFile != null)
                {
                    // Upload ảnh mới
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                    string path = Path.Combine(_env.WebRootPath, "img/blog", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await ThumbnailFile.CopyToAsync(stream);
                    }
                    post.ThumbnailUrl = "/img/blog/" + fileName;
                }

                _context.Update(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // 6. Xóa
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}