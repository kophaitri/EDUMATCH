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

        // Seed Admin User 2
        var admin2Email = configuration["AdminAccount2:Email"] ?? "admin2@edumatch.vn";
        var admin2Password = configuration["AdminAccount2:Password"] ?? "Admin@654321";
        var admin2FullName = configuration["AdminAccount2:FullName"] ?? "EduMatch Admin 2";

        if (await userManager.FindByEmailAsync(admin2Email) is null)
        {
            var admin2 = new ApplicationUser
            {
                UserName = admin2Email,
                Email = admin2Email,
                FullName = admin2FullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result2 = await userManager.CreateAsync(admin2, admin2Password);
            if (result2.Succeeded)
            {
                await userManager.AddToRoleAsync(admin2, "Admin");
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
                var subjectId = dbContext.Subjects.First().Id;
                var gradeLevelId = dbContext.GradeLevels.FirstOrDefault(g => g.Name == "Lớp 12")?.Id
                                ?? dbContext.GradeLevels.First().Id;

                // --- Contract 1: Active (cho test booking flow) ---
                var existingActiveContract = dbContext.Contracts
                    .FirstOrDefault(c => c.TutorId == tutor.Id && c.Status == ContractStatus.Active);

                if (existingActiveContract == null)
                {
                    var booking1 = new BookingRequest
                    {
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = subjectId,
                        GradeLevelId = gradeLevelId,
                        Message = "Seed data - active contract",
                        PreferredStartDate = DateTime.UtcNow,
                        SessionsPerWeek = 2,
                        DurationWeeks = 5,
                        SessionDurationHours = 1.5m,
                        HourlyRate = 150000,
                        TotalAmount = 150000 * 1.5m * 10,
                        Status = BookingStatus.Accepted,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.BookingRequests.Add(booking1);
                    await dbContext.SaveChangesAsync();

                    var activeContract = new Contract
                    {
                        BookingRequestId = booking1.Id,
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = subjectId,
                        GradeLevelId = gradeLevelId,
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

                // --- Contract 2: COMPLETED (CHO TEST REVIEW) 🎯 ---
                var existingCompletedContract = dbContext.Contracts
                    .FirstOrDefault(c => c.TutorId == tutor.Id && c.Status == ContractStatus.Completed);

                if (existingCompletedContract == null)
                {
                    var booking2 = new BookingRequest
                    {
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = subjectId,
                        GradeLevelId = gradeLevelId,
                        Message = "Seed data - completed contract",
                        PreferredStartDate = DateTime.UtcNow.AddMonths(-4),
                        SessionsPerWeek = 2,
                        DurationWeeks = 5,
                        SessionDurationHours = 1.5m,
                        HourlyRate = 150000,
                        TotalAmount = 150000 * 1.5m * 10,
                        Status = BookingStatus.Accepted,
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    };
                    dbContext.BookingRequests.Add(booking2);
                    await dbContext.SaveChangesAsync();

                    var completedContract = new Contract
                    {
                        BookingRequestId = booking2.Id,
                        StudentId = student.Id,
                        TutorId = tutor.Id,
                        SubjectId = subjectId,
                        GradeLevelId = gradeLevelId,
                        HourlyRate = 150000,
                        StartDate = DateTime.UtcNow.AddMonths(-4),
                        EndDate = DateTime.UtcNow.AddMonths(-1),
                        TotalSessions = 10,
                        CompletedSessions = 10,
                        Status = ContractStatus.Completed,
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    };
                    dbContext.Contracts.Add(completedContract);
                    await dbContext.SaveChangesAsync();

                    var completedSessions = new List<Session>();
                    for (int i = 1; i <= 10; i++)
                    {
                        completedSessions.Add(new Session
                        {
                            ContractId = completedContract.Id,
                            ScheduledAt = DateTime.UtcNow.AddMonths(-4).AddDays(i * 3),
                            DurationMinutes = 90,
                            Status = SessionStatus.Completed,
                            CreatedAt = DateTime.UtcNow.AddMonths(-4)
                        });
                    }
                    dbContext.Sessions.AddRange(completedSessions);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        // ========================================
        // 🎯 PHẦN 3: Tutor Toán
        // ========================================
        var tutorMathEmail = "tutor.toan@edumatch.vn";
        if (await userManager.FindByEmailAsync(tutorMathEmail) is null)
        {
            var tutorMath = new ApplicationUser
            {
                UserName = tutorMathEmail,
                Email = tutorMathEmail,
                FullName = "Trần Thị Toán",
                EmailConfirmed = true,
                IsActive = true,
                PhoneNumber = "0912345678"
            };

            var result = await userManager.CreateAsync(tutorMath, "Tutor@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(tutorMath, "Tutor");

                var profile = new TutorProfile
                {
                    UserId = tutorMath.Id,
                    Bio = "Giáo viên Toán 10 năm kinh nghiệm, chuyên luyện thi THPT",
                    Education = "Đại học Sư phạm Hà Nội - Cử nhân Toán",
                    YearsOfExperience = 10,
                    HourlyRateMin = 150000,
                    HourlyRateMax = 350000,
                    AvgRating = 4.9m,
                    TotalReviews = 0,
                    ReputationScore = 100m,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.TutorProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                var mathSubject = dbContext.Subjects.FirstOrDefault(s => s.Name == "Toán học");
                if (mathSubject != null)
                {
                    var gradeLevels = dbContext.GradeLevels
                        .Where(g => g.Name == "Lớp 10" || g.Name == "Lớp 11" || g.Name == "Lớp 12")
                        .ToList();

                    foreach (var grade in gradeLevels)
                    {
                        dbContext.TutorSubjects.Add(new TutorSubject
                        {
                            TutorId = profile.Id,
                            SubjectId = mathSubject.Id,
                            GradeLevelId = grade.Id,
                            HourlyRate = 1000
                        });
                    }
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        // ========================================
        // 🎯 PHẦN 4: Tutor Tiếng Anh
        // ========================================
        var tutorEngEmail = "tutor.english@edumatch.vn";
        if (await userManager.FindByEmailAsync(tutorEngEmail) is null)
        {
            var tutorEng = new ApplicationUser
            {
                UserName = tutorEngEmail,
                Email = tutorEngEmail,
                FullName = "Lê Văn English",
                EmailConfirmed = true,
                IsActive = true,
                PhoneNumber = "0987654321"
            };

            var result = await userManager.CreateAsync(tutorEng, "Tutor@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(tutorEng, "Tutor");

                var profile = new TutorProfile
                {
                    UserId = tutorEng.Id,
                    Bio = "Giáo viên Tiếng Anh IELTS 8.0, chuyên luyện thi và giao tiếp",
                    Education = "Đại học Ngoại ngữ - Cử nhân Tiếng Anh",
                    YearsOfExperience = 7,
                    HourlyRateMin = 120000,
                    HourlyRateMax = 300000,
                    AvgRating = 4.7m,
                    TotalReviews = 0,
                    ReputationScore = 100m,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.TutorProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                var engSubject = dbContext.Subjects.FirstOrDefault(s => s.Name == "Tiếng Anh");
                if (engSubject != null)
                {
                    var gradeLevels = dbContext.GradeLevels
                        .Where(g => g.Name == "Lớp 10" || g.Name == "Lớp 11" || g.Name == "Lớp 12")
                        .ToList();

                    foreach (var grade in gradeLevels)
                    {
                        dbContext.TutorSubjects.Add(new TutorSubject
                        {
                            TutorId = profile.Id,
                            SubjectId = engSubject.Id,
                            GradeLevelId = grade.Id,
                            HourlyRate = 1000
                        });
                    }
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