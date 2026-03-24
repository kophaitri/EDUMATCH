using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminUserService
{
    // Danh sách tất cả users (có tìm kiếm và lọc theo role)
    Task<List<AdminUserListDto>> GetAllUsersAsync(string? search = null, string? role = null);

    // Chi tiết một user
    Task<AdminUserDetailDto?> GetUserDetailAsync(string userId);

    // Khoá / Mở khoá tài khoản
    Task<(bool Success, string Message)> ToggleUserActiveAsync(string userId);

    // Danh sách gia sư chờ xác minh
    Task<List<PendingTutorDto>> GetPendingTutorsAsync();

    // Xác minh gia sư + duyệt bằng cấp
    Task<(bool Success, string Message)> VerifyTutorAsync(string tutorUserId, VerifyTutorRequest request);

    // Danh sách học viên
    Task<List<AdminStudentListDto>> GetAllStudentsAsync(string? search = null);
}
