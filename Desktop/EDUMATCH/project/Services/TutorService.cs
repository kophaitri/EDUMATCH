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
            .Include(p => p.Images)
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
                ImageUrls = p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
                IsPublished = p.IsPublished,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<TutorPostViewModel?> GetPostByIdAsync(int postId)
    {
        return await _context.TutorPosts
            .Include(p => p.Images)
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
                ImageUrls = p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
                IsPublished = p.IsPublished,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, int PostId, IEnumerable<string> Errors)> CreatePostAsync(string tutorId, CreateTutorPostViewModel model)
    {
        var post = new TutorPost
        {
            TutorId = tutorId,
            Title = model.Title,
            Content = model.Content,
            IsPublished = model.IsPublished,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Upload images
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            var imageUrls = await SaveImagesAsync(tutorId, model.ImageFiles);
            post.ThumbnailUrl = imageUrls.First();

            for (int i = 0; i < imageUrls.Count; i++)
            {
                post.Images.Add(new TutorPostImage
                {
                    ImageUrl = imageUrls[i],
                    DisplayOrder = i
                });
            }
        }

        _context.TutorPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tutor {TutorId} created post {PostId}.", tutorId, post.Id);

        return (true, post.Id, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdatePostAsync(string tutorId, EditTutorPostViewModel model)
    {
        var post = await _context.TutorPosts
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (post == null || post.TutorId != tutorId)
            return (false, ["Bài viết không tồn tại hoặc bạn không có quyền chỉnh sửa."]);

        post.Title = model.Title;
        post.Content = model.Content;
        post.IsPublished = model.IsPublished;
        post.UpdatedAt = DateTime.UtcNow;

        // Upload new images if provided
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            // Remove old images from DB
            _context.TutorPostImages.RemoveRange(post.Images);

            var imageUrls = await SaveImagesAsync(tutorId, model.ImageFiles);
            post.ThumbnailUrl = imageUrls.First();

            for (int i = 0; i < imageUrls.Count; i++)
            {
                post.Images.Add(new TutorPostImage
                {
                    ImageUrl = imageUrls[i],
                    DisplayOrder = i
                });
            }
        }

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

    private async Task<List<string>> SaveImagesAsync(string tutorId, List<IFormFile> files)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
        Directory.CreateDirectory(uploadsFolder);

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length <= 0) continue;

            var uniqueFileName = $"{tutorId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            urls.Add($"/uploads/posts/{uniqueFileName}");
        }

        return urls;
    }
}
