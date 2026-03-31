using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduMatch.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly EduMatchDbContext _db;

    public NotificationBellViewComponent(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Content(string.Empty);

        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return View(unreadCount);
    }
}
