using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduMatch.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly EduMatchDbContext _db;

    public NotificationController(EduMatchDbContext db)
    {
        _db = db;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Notification/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
        return View(notifications);
    }

    // POST: /Notification/MarkRead/{id}
    // Đánh dấu đã đọc rồi redirect đến ActionUrl nếu có
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetUserId();
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(notification.ActionUrl))
                return Redirect(notification.ActionUrl);
        }

        return RedirectToAction("Index");
    }

    // POST: /Notification/MarkAllRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã đánh dấu {unread.Count} thông báo là đã đọc.";
        return RedirectToAction("Index");
    }

    // POST: /Notification/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification != null)
        {
            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }
}
