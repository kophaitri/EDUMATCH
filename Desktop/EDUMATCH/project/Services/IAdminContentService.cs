using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminContentService
{
    // Subject
    Task<List<SubjectDto>> GetSubjectsAsync();
    Task<SubjectDto?> GetSubjectByIdAsync(int id);
    Task<(bool Success, string Message)> CreateSubjectAsync(CreateSubjectRequest request);
    Task<(bool Success, string Message)> UpdateSubjectAsync(int id, UpdateSubjectRequest request);
    Task<(bool Success, string Message)> DeleteSubjectAsync(int id);

    // GradeLevel
    Task<List<GradeLevelDto>> GetGradeLevelsAsync();
    Task<(bool Success, string Message)> CreateGradeLevelAsync(CreateGradeLevelRequest request);
    Task<(bool Success, string Message)> UpdateGradeLevelAsync(int id, UpdateGradeLevelRequest request);
    Task<(bool Success, string Message)> DeleteGradeLevelAsync(int id);

    // TeachingStyle
    Task<List<TeachingStyleDto>> GetTeachingStylesAsync();
    Task<(bool Success, string Message)> CreateTeachingStyleAsync(CreateTeachingStyleRequest request);
    Task<(bool Success, string Message)> UpdateTeachingStyleAsync(int id, UpdateTeachingStyleRequest request);
    Task<(bool Success, string Message)> DeleteTeachingStyleAsync(int id);

    // Banner
    Task<List<BannerDto>> GetBannersAsync();
    Task<BannerDto?> GetBannerByIdAsync(int id);
    Task<(bool Success, string Message)> CreateBannerAsync(CreateBannerRequest request);
    Task<(bool Success, string Message)> UpdateBannerAsync(int id, UpdateBannerRequest request);
    Task<(bool Success, string Message)> DeleteBannerAsync(int id);
    Task<(bool Success, string Message)> ToggleBannerActiveAsync(int id);
}
