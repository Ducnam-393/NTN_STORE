using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTN_STORE.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NTN_STORE.Controllers
{
    public class BlogController : Controller
    {
        private readonly NTNStoreContext _context;

        public BlogController(NTNStoreContext context)
        {
            _context = context;
        }

        // GET: /Blog
        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogPosts
                .Where(b => b.IsVisible)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET: /Blog/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null || !post.IsVisible)
            {
                return NotFound();
            }
            return View(post);
        }
    }
}