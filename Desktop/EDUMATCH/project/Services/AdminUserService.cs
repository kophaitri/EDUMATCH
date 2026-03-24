using EduMatch.DTOs.Admin;
using Microsoft.AspNetCore.Identity;

namespace EduMatch.Services;

public class AdminUserService : IAdminUserService
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<AdminUserListDto>> GetAllUsersAsync(string? search = null, string? role = null)
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
            users = users.Where(u =>
                u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        var result = new List<AdminUserListDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var userRole = roles.FirstOrDefault() ?? "Unknown";

            if (!string.IsNullOrWhiteSpace(role) &&
                !userRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(new AdminUserListDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                Role = userRole,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        return result;
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _db.Users
            .Include(u => u.TutorProfile)
                .ThenInclude(t => t!.Certificates)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        var dto = new AdminUserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Role = roles.FirstOrDefault() ?? "Unknown",
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        if (user.TutorProfile != null)
        {
            dto.TutorProfile = new TutorProfileDetailDto
            {
                Id = user.TutorProfile.Id,
                Bio = user.TutorProfile.Bio,
                Education = user.TutorProfile.Education,
                YearsOfExperience = user.TutorProfile.YearsOfExperience,
                AvgRating = user.TutorProfile.AvgRating,
                TotalReviews = user.TutorProfile.TotalReviews,
                HourlyRateMin = user.TutorProfile.HourlyRateMin,
                HourlyRateMax = user.TutorProfile.HourlyRateMax,
                IsVerified = user.TutorProfile.IsVerified,
                ReputationScore = user.TutorProfile.ReputationScore,
                CreatedAt = user.TutorProfile.CreatedAt,
                Certificates = user.TutorProfile.Certificates.Select(c => new TutorCertificateDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Issuer = c.Issuer,
                    IssueDate = c.IssueDate,
                    FileUrl = c.FileUrl,
                    IsVerified = c.IsVerified
                }).ToList()
            };
        }

        if (user.StudentProfile != null)
        {
            dto.StudentProfile = new StudentProfileDetailDto
            {
                Id = user.StudentProfile.Id,
                CurrentGrade = user.StudentProfile.CurrentGrade,
                SchoolName = user.StudentProfile.SchoolName,
                LearningGoals = user.StudentProfile.LearningGoals,
                CreatedAt = user.StudentProfile.CreatedAt
            };
        }

        return dto;
    }

    public async Task<(bool Success, string Message)> ToggleUserActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "Không tìm thấy người dùng.");

        user.IsActive = !user.IsActive;

        if (!user.IsActive)
        {
            // Khoá tài khoản: bật lockout vô thời hạn
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            // Mở khoá: gỡ lockout và tắt lockout để tránh bị khoá lại do login sai
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
        }

        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, user.IsActive ? "Đã mở khoá tài khoản thành công." : "Đã khoá tài khoản thành công.");
    }

    public async Task<List<PendingTutorDto>> GetPendingTutorsAsync()
    {
        var pending = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.Certificates)
            .Where(t => !t.IsVerified)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return pending.Select(t => new PendingTutorDto
        {
            UserId = t.UserId,
            FullName = t.User.FullName,
            Email = t.User.Email,
            AvatarUrl = t.User.AvatarUrl,
            Bio = t.Bio,
            Education = t.Education,
            YearsOfExperience = t.YearsOfExperience,
            HourlyRateMin = t.HourlyRateMin,
            HourlyRateMax = t.HourlyRateMax,
            CreatedAt = t.CreatedAt,
            PendingCertificates = t.Certificates.Count(c => !c.IsVerified)
        }).ToList();
    }

    public async Task<(bool Success, string Message)> VerifyTutorAsync(string tutorUserId, VerifyTutorRequest request)
    {
        var tutor = await _db.TutorProfiles
            .Include(t => t.Certificates)
            .FirstOrDefaultAsync(t => t.UserId == tutorUserId);

        if (tutor == null) return (false, "Không tìm thấy hồ sơ gia sư.");

        tutor.IsVerified = request.Approve;
        tutor.UpdatedAt = DateTime.UtcNow;

        // Duyệt các bằng cấp được chọn
        if (request.Approve && request.ApprovedCertificateIds.Any())
        {
            foreach (var cert in tutor.Certificates)
            {
                cert.IsVerified = request.ApprovedCertificateIds.Contains(cert.Id);
            }
        }

        // Gửi thông báo cho gia sư
        _db.Notifications.Add(new Notification
        {
            UserId = tutorUserId,
            Type = NotificationType.System,
            Title = request.Approve ? "Hồ sơ đã được xác minh" : "Hồ sơ chưa được duyệt",
            Message = request.Approve
                ? "Chúc mừng! Hồ sơ gia sư của bạn đã được Admin xác minh thành công."
                : $"Hồ sơ gia sư của bạn chưa được duyệt.{(request.Note != null ? " Lý do: " + request.Note : "")}",
            ActionUrl = "/Account/Profile",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return (true, request.Approve
            ? "Đã xác minh gia sư thành công."
            : "Đã từ chối xác minh gia sư.");
    }

    public async Task<List<AdminStudentListDto>> GetAllStudentsAsync(string? search = null)
    {
        var query = _db.StudentProfiles
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.User.FullName.Contains(search) ||
                s.User.Email!.Contains(search));

        var students = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return students.Select(s => new AdminStudentListDto
        {
            UserId = s.UserId,
            FullName = s.User.FullName,
            Email = s.User.Email,
            AvatarUrl = s.User.AvatarUrl,
            CurrentGrade = s.CurrentGrade,
            SchoolName = s.SchoolName,
            IsActive = s.User.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
