using Microsoft.AspNetCore.Identity;
using EduMatch.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<EduMatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITutorService, TutorService>();

// Admin Services
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminContentService, AdminContentService>();
builder.Services.AddScoped<IAdminComplaintService, AdminComplaintService>();
builder.Services.AddScoped<IAdminTicketService, AdminTicketService>();
builder.Services.AddScoped<IAdminEmailService, AdminEmailService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
builder.Services.AddScoped<ITutorDashboardService, TutorDashboardService>();

// Anti-Cheat Services
builder.Services.AddScoped<WritingStyleAnalyzer>();
builder.Services.AddScoped<ISuspiciousScoreService, SuspiciousScoreService>();
builder.Services.AddScoped<IExamBehaviorService, ExamBehaviorService>();
builder.Services.AddScoped<IFraudService, FraudService>();

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.RequireUniqueEmail = true;

    // Sign-in
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<EduMatchDbContext>()
.AddDefaultTokenProviders();

// Thêm service cho file upload
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});

// Nếu dùng Word parsing, thêm thư viện:
// dotnet add package DocumentFormat.OpenXml
// hoặc dotnet add package DocX

// Configure application cookie for web UI
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Authorization Policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("TutorOnly", p => p.RequireRole("Tutor"))
    .AddPolicy("StudentOnly", p => p.RequireRole("Student"))
    .AddPolicy("TutorOrStudent", p => p.RequireRole("Tutor", "Student"));

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
