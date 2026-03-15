using EduMatch.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduMatch.Services;

public class TutorService : ITutorService
{
    private readonly EduMatchDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TutorService> _logger;

    public TutorService(
        EduMatchDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TutorService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<TutorPostViewModel>> GetPostsByTutorAsync(string tutorId)
    {
        return await _context.TutorPosts
            .Where(p => p.TutorId == tutorId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TutorPostViewModel
            {
                Id = p.Id,
                TutorId = p.TutorId,
                TutorName = p.Tutor.FullName,
                TutorAvatarUrl = p.Tutor.AvatarUrl,
                Title = p.Title,
                Content = p.Content,
                ThumbnailUrl = p.ThumbnailUrl,
                IsPublished = p.IsPublished,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<TutorPostViewModel?> GetPostByIdAsync(int postId)
    {
        return await _context.TutorPosts
            .Where(p => p.Id == postId)
            .Select(p => new TutorPostViewModel
            {
                Id = p.Id,
                TutorId = p.TutorId,
                TutorName = p.Tutor.FullName,
                TutorAvatarUrl = p.Tutor.AvatarUrl,
                Title = p.Title,
                Content = p.Content,
                ThumbnailUrl = p.ThumbnailUrl,
                IsPublished = p.IsPublished,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, int PostId, IEnumerable<string> Errors)> CreatePostAsync(string tutorId, CreateTutorPostViewModel model)
    {
        string? thumbnailUrl = null;

        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{tutorId}_{Guid.NewGuid()}{Path.GetExtension(model.ThumbnailFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await model.ThumbnailFile.CopyToAsync(fileStream);

            thumbnailUrl = $"/uploads/posts/{uniqueFileName}";
        }

        var post = new TutorPost
        {
            TutorId = tutorId,
            Title = model.Title,
            Content = model.Content,
            ThumbnailUrl = thumbnailUrl,
            IsPublished = model.IsPublished,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TutorPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tutor {TutorId} created post {PostId}.", tutorId, post.Id);

        return (true, post.Id, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdatePostAsync(string tutorId, EditTutorPostViewModel model)
    {
        var post = await _context.TutorPosts.FindAsync(model.Id);

        if (post == null || post.TutorId != tutorId)
            return (false, ["Bài viết không tồn tại hoặc bạn không có quyền chỉnh sửa."]);

        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{tutorId}_{Guid.NewGuid()}{Path.GetExtension(model.ThumbnailFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await model.ThumbnailFile.CopyToAsync(fileStream);

            post.ThumbnailUrl = $"/uploads/posts/{uniqueFileName}";
        }

        post.Title = model.Title;
        post.Content = model.Content;
        post.IsPublished = model.IsPublished;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tutor {TutorId} updated post {PostId}.", tutorId, post.Id);

        return (true, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, string Error)> DeletePostAsync(string tutorId, int postId)
    {
        var post = await _context.TutorPosts.FindAsync(postId);

        if (post == null || post.TutorId != tutorId)
            return (false, "Bài viết không tồn tại hoặc bạn không có quyền xóa.");

        _context.TutorPosts.Remove(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tutor {TutorId} deleted post {PostId}.", tutorId, postId);

        return (true, string.Empty);
    }
}
