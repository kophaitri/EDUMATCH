using System.ComponentModel.DataAnnotations;

namespace EduMatch.ViewModels;

public class TutorPostViewModel
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string? TutorAvatarUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTutorPostViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    public string Content { get; set; } = string.Empty;

    public List<IFormFile>? ImageFiles { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class EditTutorPostViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    public string Content { get; set; } = string.Empty;

    public List<IFormFile>? ImageFiles { get; set; }
    public List<string> ExistingImageUrls { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
