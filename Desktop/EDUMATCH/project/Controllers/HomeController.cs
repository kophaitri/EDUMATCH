using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EduMatch.Models;

namespace EduMatch.Controllers;

public class HomeController : Controller
{
    private readonly EduMatchDbContext _db;

    public HomeController(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var banners = await _db.Banners
            .Where(b => b.IsActive && b.StartAt <= now && b.EndAt >= now)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

        ViewBag.Banners = banners;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
