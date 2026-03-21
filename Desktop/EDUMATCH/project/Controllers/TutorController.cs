using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

[Authorize(Policy = "TutorOnly")]
public class TutorController : Controller
{
    private readonly ITutorService _tutorService;

    public TutorController(ITutorService tutorService)
    {
        _tutorService = tutorService;
    }

    // GET: /Tutor/Posts
    [HttpGet]
    public async Task<IActionResult> Posts()
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var posts = await _tutorService.GetPostsByTutorAsync(tutorId);
        return View(posts);
    }

    // GET: /Tutor/PostDetail/5
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> PostDetail(int id)
    {
        var post = await _tutorService.GetPostByIdAsync(id);
        if (post == null)
            return NotFound();

        // Chỉ chủ bài viết mới xem được bài nháp
        if (!post.IsPublished)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != post.TutorId)
                return NotFound();
        }

        return View(post);
    }

    // GET: /Tutor/CreatePost
    [HttpGet]
    public IActionResult CreatePost() => View();

    // POST: /Tutor/CreatePost
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CreateTutorPostViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, _, errors) = await _tutorService.CreatePostAsync(tutorId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Đăng bài viết thành công!";
            return RedirectToAction(nameof(Posts));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Tutor/EditPost/5
    [HttpGet]
    public async Task<IActionResult> EditPost(int id)
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var post = await _tutorService.GetPostByIdAsync(id);

        if (post == null || post.TutorId != tutorId)
            return NotFound();

        var model = new EditTutorPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            ExistingImageUrls = post.ImageUrls,
            IsPublished = post.IsPublished
        };

        return View(model);
    }

    // POST: /Tutor/EditPost/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(EditTutorPostViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, errors) = await _tutorService.UpdatePostAsync(tutorId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
            return RedirectToAction(nameof(Posts));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // POST: /Tutor/DeletePost/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _tutorService.DeletePostAsync(tutorId, id);

        if (success)
            TempData["SuccessMessage"] = "Xóa bài viết thành công!";
        else
            TempData["ErrorMessage"] = error;

        return RedirectToAction(nameof(Posts));
    }
}
