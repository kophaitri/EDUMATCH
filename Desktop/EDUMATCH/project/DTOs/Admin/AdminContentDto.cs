namespace EduMatch.DTOs.Admin;

// ===== SUBJECT =====
public class SubjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public int TutorCount { get; set; }
}

public class CreateSubjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSubjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
}

// ===== GRADE LEVEL =====
public class GradeLevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class CreateGradeLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class UpdateGradeLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

// ===== TEACHING STYLE =====
public class TeachingStyleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TutorCount { get; set; }
}

public class CreateTeachingStyleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateTeachingStyleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ===== BANNER =====
public class BannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsCurrentlyActive => IsActive && DateTime.UtcNow >= StartAt && DateTime.UtcNow <= EndAt;
}

public class CreateBannerRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

public class UpdateBannerRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}
