using EduMatch.ViewModels;

namespace EduMatch.Services;

public interface ITutorService
{
    Task<List<TutorPostViewModel>> GetPostsByTutorAsync(string tutorId);
    Task<TutorPostViewModel?> GetPostByIdAsync(int postId);
    Task<(bool Success, int PostId, IEnumerable<string> Errors)> CreatePostAsync(string tutorId, CreateTutorPostViewModel model);
    Task<(bool Success, IEnumerable<string> Errors)> UpdatePostAsync(string tutorId, EditTutorPostViewModel model);
    Task<(bool Success, string Error)> DeletePostAsync(string tutorId, int postId);
}
