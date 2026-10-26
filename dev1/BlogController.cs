using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;

namespace NovaShop.Controllers;

public class BlogController : Controller
{
    private readonly AppDbContext _db;
    public BlogController(AppDbContext db) => _db = db;

    [Route("blog")]
    public async Task<IActionResult> Index() =>
        View(await _db.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt).ToListAsync());

    [Route("blog/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);
        if (post == null) return NotFound();
        post.ViewCount++;
        await _db.SaveChangesAsync();
        return View(post);
    }
}


