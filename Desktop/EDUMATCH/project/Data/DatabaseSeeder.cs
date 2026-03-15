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
        var adminFullName = configuration["AdminAccount:FullName"] ?? "EduMatch Admin";

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

        // Seed Master Data
        await SeedMasterDataAsync(dbContext);
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
