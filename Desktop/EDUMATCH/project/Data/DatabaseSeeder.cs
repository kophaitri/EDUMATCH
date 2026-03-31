using Microsoft.AspNetCore.Identity;

namespace EduMatch.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = serviceProvider.GetRequiredService<EduMatchDbContext>();

        // Seed Roles
        string[] roles = { "Admin", "Tutor", "Student" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role"
                });
            }
        }

        // Seed Admin User
        var adminEmail = configuration["AdminAccount:Email"] ?? "admin@edumatch.vn";
        var adminPassword = configuration["AdminAccount:Password"] ?? "Admin@123456";
        var adminFullName = configuration["AdminAccount:FullName"] ?? "EduMatchAdmin";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminFullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // ========================================
        // 🎯 PHẦN 1: Tạo Tutor Account
        // ========================================
        var tutorEmail = "tutor@edumatch.vn";
        var tutorPassword = "Tutor@123456";
        ApplicationUser? tutor = null;
        
        if (await userManager.FindByEmailAsync(tutorEmail) is null)
        {
            tutor = new ApplicationUser
            {
                UserName = tutorEmail,
                Email = tutorEmail,
                FullName = "Nguyễn Văn Tutor",
                EmailConfirmed = true,
                IsActive = true,
                PhoneNumber = "0901234567"
            };

            var result = await userManager.CreateAsync(tutor, tutorPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(tutor, "Tutor");

                var tutorProfile = new TutorProfile
                {
                    UserId = tutor.Id,
                    Bio = "Gia sư có kinh nghiệm 5 năm",
                    HourlyRateMin = 100000,
                    HourlyRateMax = 300000,
                    AvgRating = 4.8m,
                    TotalReviews = 0,
                    ReputationScore = 100m,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.TutorProfiles.Add(tutorProfile);
                await dbContext.SaveChangesAsync();
            }
        }
        else
        {
            tutor = await userManager.FindByEmailAsync(tutorEmail);
        }

        // ========================================
        // ✅ SEED MASTER DATA TRƯỚC (QUAN TRỌNG)
        // ========================================
        await SeedMasterDataAsync(dbContext);

        // ========================================
        // 🎯 PHẦN 2: Tạo Contract + Session (SAU KHI ĐÃ CÓ SUBJECTS)
        // ========================================
        if (tutor != null)
        {
            // Tạo Student
            var studentEmail = "student1@edumatch.vn";
            ApplicationUser? student = null;
            
            if (await userManager.FindByEmailAsync(studentEmail) is null)
            {
                student = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "Nguyễn Văn Student",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(student, "Student@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                }
            }
            else
            {
                student = await userManager.FindByEmailAsync(studentEmail);
            }

            // ✅ BÂY GIỜ SUBJECTS ĐÃ CÓ → TẠO CONTRACT + SESSION
            if (student != null && dbContext.Subjects.Any() && dbContext.GradeLevels.Any())
            {
                // --- Contract 1: Active (cho test booking flow) ---
                var existingActiveContract = dbContext.Contracts
                    .FirstOrDefault(c => c.TutorId == tutor.Id && c.Status == ContractStatus.Active);

                if (existingActiveContract == null)
                {
                    var activeContract = new Contract
                    {
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = dbContext.Subjects.First().Id,
                        GradeLevelId = dbContext.GradeLevels.FirstOrDefault(g => g.Name == "Lớp 12")?.Id 
                                    ?? dbContext.GradeLevels.First().Id,
                        HourlyRate = 150000,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(3),
                        TotalSessions = 10,
                        CompletedSessions = 0,
                        Status = ContractStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Contracts.Add(activeContract);
                    await dbContext.SaveChangesAsync();

                    // Tạo sessions cho active contract
                    var existingSessions = dbContext.Sessions
                        .Where(s => s.ContractId == activeContract.Id)
                        .ToList();

                    if (!existingSessions.Any())
                    {
                        var sessions = new List<Session>();
                        for (int i = 1; i <= 3; i++)
                        {
                            sessions.Add(new Session
                            {
                                ContractId = activeContract.Id,
                                ScheduledAt = DateTime.UtcNow.AddDays(i * 7),
                                DurationMinutes = 90,
                                Status = SessionStatus.Scheduled,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        dbContext.Sessions.AddRange(sessions);
                        await dbContext.SaveChangesAsync();
                    }
                }

                // --- Contract 2: COMPLETED (CHO TEST REVIEW) 🎯 ---
                var existingCompletedContract = dbContext.Contracts
                    .FirstOrDefault(c => c.TutorId == tutor.Id && c.Status == ContractStatus.Completed);

                if (existingCompletedContract == null)
                {
                    var completedContract = new Contract
                    {
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = dbContext.Subjects.First().Id,
                        GradeLevelId = dbContext.GradeLevels.FirstOrDefault(g => g.Name == "Lớp 12")?.Id 
                                    ?? dbContext.GradeLevels.First().Id,
                        HourlyRate = 150000,
                        StartDate = DateTime.UtcNow.AddMonths(-4),  // ← Bắt đầu 4 tháng trước
                        EndDate = DateTime.UtcNow.AddMonths(-1),    // ← Kết thúc 1 tháng trước (ĐÃ HOÀN THÀNH)
                        TotalSessions = 10,
                        CompletedSessions = 10,                      // ← Đã hoàn thành tất cả buổi
                        Status = ContractStatus.Completed,           // ← QUAN TRỌNG: Status = Completed
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    };

                    dbContext.Contracts.Add(completedContract);
                    await dbContext.SaveChangesAsync();

                    // Tạo sessions đã completed cho contract này
                    var completedSessions = new List<Session>();
                    for (int i = 1; i <= 10; i++)
                    {
                        completedSessions.Add(new Session
                        {
                            ContractId = completedContract.Id,
                            ScheduledAt = DateTime.UtcNow.AddMonths(-4).AddDays(i * 3),  // Các buổi trong quá khứ
                            DurationMinutes = 90,
                            Status = SessionStatus.Completed,  // ← Sessions đã hoàn thành
                            CreatedAt = DateTime.UtcNow.AddMonths(-4)
                        });
                    }
                    dbContext.Sessions.AddRange(completedSessions);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        // ========================================
        // ✅ HOÀN TẤT
        // ========================================
    }

    private static async Task SeedMasterDataAsync(EduMatchDbContext db)
    {
        // Seed Subjects
        if (!db.Subjects.Any())
        {
            db.Subjects.AddRange(
                new Subject { Name = "Toán học", Description = "Mathematics", IsActive = true },
                new Subject { Name = "Vật lý", Description = "Physics", IsActive = true },
                new Subject { Name = "Hóa học", Description = "Chemistry", IsActive = true },
                new Subject { Name = "Sinh học", Description = "Biology", IsActive = true },
                new Subject { Name = "Tiếng Anh", Description = "English", IsActive = true },
                new Subject { Name = "Văn học", Description = "Literature", IsActive = true },
                new Subject { Name = "Lịch sử", Description = "History", IsActive = true },
                new Subject { Name = "Địa lý", Description = "Geography", IsActive = true },
                new Subject { Name = "Tin học", Description = "Computer Science", IsActive = true }
            );
        }

        // Seed Grade Levels
        if (!db.GradeLevels.Any())
        {
            db.GradeLevels.AddRange(
                new GradeLevel { Name = "Lớp 1", DisplayOrder = 1 },
                new GradeLevel { Name = "Lớp 2", DisplayOrder = 2 },
                new GradeLevel { Name = "Lớp 3", DisplayOrder = 3 },
                new GradeLevel { Name = "Lớp 4", DisplayOrder = 4 },
                new GradeLevel { Name = "Lớp 5", DisplayOrder = 5 },
                new GradeLevel { Name = "Lớp 6", DisplayOrder = 6 },
                new GradeLevel { Name = "Lớp 7", DisplayOrder = 7 },
                new GradeLevel { Name = "Lớp 8", DisplayOrder = 8 },
                new GradeLevel { Name = "Lớp 9", DisplayOrder = 9 },
                new GradeLevel { Name = "Lớp 10", DisplayOrder = 10 },
                new GradeLevel { Name = "Lớp 11", DisplayOrder = 11 },
                new GradeLevel { Name = "Lớp 12", DisplayOrder = 12 }
            );
        }

        // Seed Teaching Styles
        if (!db.TeachingStyles.Any())
        {
            db.TeachingStyles.AddRange(
                new TeachingStyle { Name = "Trực quan", Description = "Visual learning" },
                new TeachingStyle { Name = "Thực hành", Description = "Hands-on learning" },
                new TeachingStyle { Name = "Tương tác", Description = "Interactive learning" },
                new TeachingStyle { Name = "Cá nhân hóa", Description = "Personalized learning" },
                new TeachingStyle { Name = "Nhóm", Description = "Group learning" }
            );
        }

        await db.SaveChangesAsync();
    }
}