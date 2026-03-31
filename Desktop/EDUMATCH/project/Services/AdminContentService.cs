using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public class AdminContentService : IAdminContentService
{
    private readonly EduMatchDbContext _db;

    public AdminContentService(EduMatchDbContext db)
    {
        _db = db;
    }

    // ===================== SUBJECT =====================

    public async Task<List<SubjectDto>> GetSubjectsAsync()
    {
        return await _db.Subjects
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IconUrl = s.IconUrl,
                IsActive = s.IsActive,
                TutorCount = s.TutorSubjects.Select(ts => ts.TutorId).Distinct().Count()
            })
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(int id)
    {
        return await _db.Subjects
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IconUrl = s.IconUrl,
                IsActive = s.IsActive,
                TutorCount = s.TutorSubjects.Select(ts => ts.TutorId).Distinct().Count()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateSubjectAsync(CreateSubjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên môn học không được để trống.");

        var exists = await _db.Subjects.AnyAsync(s => s.Name == request.Name);
        if (exists) return (false, "Môn học này đã tồn tại.");

        _db.Subjects.Add(new Subject
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IconUrl = request.IconUrl,
            IsActive = request.IsActive
        });

        await _db.SaveChangesAsync();
        return (true, "Tạo môn học thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateSubjectAsync(int id, UpdateSubjectRequest request)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject == null) return (false, "Không tìm thấy môn học.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên môn học không được để trống.");

        var duplicate = await _db.Subjects
            .AnyAsync(s => s.Name == request.Name && s.Id != id);
        if (duplicate) return (false, "Tên môn học đã tồn tại.");

        subject.Name = request.Name.Trim();
        subject.Description = request.Description;
        subject.IconUrl = request.IconUrl;
        subject.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return (true, "Cập nhật môn học thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteSubjectAsync(int id)
    {
        var subject = await _db.Subjects
            .Include(s => s.TutorSubjects)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null) return (false, "Không tìm thấy môn học.");
        if (subject.TutorSubjects.Any())
            return (false, "Không thể xoá môn học đang được sử dụng bởi gia sư.");

        bool usedInBooking = await _db.BookingRequests.AnyAsync(b => b.SubjectId == id);
        if (usedInBooking)
            return (false, "Không thể xoá môn học đang có trong booking.");

        bool usedInContract = await _db.Contracts.AnyAsync(c => c.SubjectId == id);
        if (usedInContract)
            return (false, "Không thể xoá môn học đang có trong hợp đồng.");

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
        return (true, "Xoá môn học thành công.");
    }

    // ===================== GRADE LEVEL =====================

    public async Task<List<GradeLevelDto>> GetGradeLevelsAsync()
    {
        return await _db.GradeLevels
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new GradeLevelDto
            {
                Id = g.Id,
                Name = g.Name,
                DisplayOrder = g.DisplayOrder
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateGradeLevelAsync(CreateGradeLevelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên cấp lớp không được để trống.");

        var exists = await _db.GradeLevels.AnyAsync(g => g.Name == request.Name);
        if (exists) return (false, "Cấp lớp này đã tồn tại.");

        _db.GradeLevels.Add(new GradeLevel
        {
            Name = request.Name.Trim(),
            DisplayOrder = request.DisplayOrder
        });

        await _db.SaveChangesAsync();
        return (true, "Tạo cấp lớp thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateGradeLevelAsync(int id, UpdateGradeLevelRequest request)
    {
        var gradeLevel = await _db.GradeLevels.FindAsync(id);
        if (gradeLevel == null) return (false, "Không tìm thấy cấp lớp.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên cấp lớp không được để trống.");

        gradeLevel.Name = request.Name.Trim();
        gradeLevel.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync();
        return (true, "Cập nhật cấp lớp thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteGradeLevelAsync(int id)
    {
        var gradeLevel = await _db.GradeLevels
            .Include(g => g.TutorSubjects)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gradeLevel == null) return (false, "Không tìm thấy cấp lớp.");
        if (gradeLevel.TutorSubjects.Any())
            return (false, "Không thể xoá cấp lớp đang được sử dụng bởi gia sư.");

        bool usedInBooking = await _db.BookingRequests.AnyAsync(b => b.GradeLevelId == id);
        if (usedInBooking)
            return (false, "Không thể xoá cấp lớp đang có trong booking.");

        bool usedInContract = await _db.Contracts.AnyAsync(c => c.GradeLevelId == id);
        if (usedInContract)
            return (false, "Không thể xoá cấp lớp đang có trong hợp đồng.");

        _db.GradeLevels.Remove(gradeLevel);
        await _db.SaveChangesAsync();
        return (true, "Xoá cấp lớp thành công.");
    }

    // ===================== TEACHING STYLE =====================

    public async Task<List<TeachingStyleDto>> GetTeachingStylesAsync()
    {
        return await _db.TeachingStyles
            .Select(ts => new TeachingStyleDto
            {
                Id = ts.Id,
                Name = ts.Name,
                Description = ts.Description,
                TutorCount = ts.TutorTeachingStyles.Count
            })
            .OrderBy(ts => ts.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateTeachingStyleAsync(CreateTeachingStyleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên phong cách dạy không được để trống.");

        var exists = await _db.TeachingStyles.AnyAsync(ts => ts.Name == request.Name);
        if (exists) return (false, "Phong cách dạy này đã tồn tại.");

        _db.TeachingStyles.Add(new TeachingStyle
        {
            Name = request.Name.Trim(),
            Description = request.Description
        });

        await _db.SaveChangesAsync();
        return (true, "Tạo phong cách dạy thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateTeachingStyleAsync(int id, UpdateTeachingStyleRequest request)
    {
        var style = await _db.TeachingStyles.FindAsync(id);
        if (style == null) return (false, "Không tìm thấy phong cách dạy.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên phong cách dạy không được để trống.");

        style.Name = request.Name.Trim();
        style.Description = request.Description;

        await _db.SaveChangesAsync();
        return (true, "Cập nhật phong cách dạy thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteTeachingStyleAsync(int id)
    {
        var style = await _db.TeachingStyles
            .Include(ts => ts.TutorTeachingStyles)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (style == null) return (false, "Không tìm thấy phong cách dạy.");
        if (style.TutorTeachingStyles.Any())
            return (false, "Không thể xoá phong cách đang được gia sư sử dụng.");

        _db.TeachingStyles.Remove(style);
        await _db.SaveChangesAsync();
        return (true, "Xoá phong cách dạy thành công.");
    }

    // ===================== BANNER =====================

    public async Task<List<BannerDto>> GetBannersAsync()
    {
        return await _db.Banners
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new BannerDto
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                LinkUrl = b.LinkUrl,
                IsActive = b.IsActive,
                DisplayOrder = b.DisplayOrder,
                StartAt = b.StartAt,
                EndAt = b.EndAt
            })
            .ToListAsync();
    }

    public async Task<BannerDto?> GetBannerByIdAsync(int id)
    {
        return await _db.Banners
            .Where(b => b.Id == id)
            .Select(b => new BannerDto
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                LinkUrl = b.LinkUrl,
                IsActive = b.IsActive,
                DisplayOrder = b.DisplayOrder,
                StartAt = b.StartAt,
                EndAt = b.EndAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateBannerAsync(CreateBannerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return (false, "Tiêu đề banner không được để trống.");

        if (request.EndAt <= request.StartAt)
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.");

        _db.Banners.Add(new Banner
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return (true, "Tạo banner thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateBannerAsync(int id, UpdateBannerRequest request)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return (false, "Không tìm thấy banner.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return (false, "Tiêu đề banner không được để trống.");

        if (request.EndAt <= request.StartAt)
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.");

        banner.Title = request.Title.Trim();
        banner.Description = request.Description;
        banner.ImageUrl = request.ImageUrl;
        banner.LinkUrl = request.LinkUrl;
        banner.IsActive = request.IsActive;
        banner.DisplayOrder = request.DisplayOrder;
        banner.StartAt = request.StartAt;
        banner.EndAt = request.EndAt;
        banner.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, "Cập nhật banner thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteBannerAsync(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return (false, "Không tìm thấy banner.");

        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync();
        return (true, "Xoá banner thành công.");
    }

    public async Task<(bool Success, string Message)> ToggleBannerActiveAsync(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return (false, "Không tìm thấy banner.");

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, banner.IsActive ? "Đã bật banner." : "Đã tắt banner.");
    }
}
