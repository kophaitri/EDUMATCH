using Microsoft.AspNetCore.Identity;
using EduMatch.ViewModels;

namespace EduMatch.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly EduMatchDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        EduMatchDbContext context,
        IEmailService emailService,
        ILogger<AccountService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model, string confirmEmailCallbackUrl)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        _logger.LogInformation("User {Email} created a new account.", model.Email);

        await _userManager.AddToRoleAsync(user, model.Role);

        if (model.Role == "Tutor")
        {
            _context.TutorProfiles.Add(new TutorProfile
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            _context.StudentProfiles.Add(new StudentProfile
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Wallets.Add(new Wallet
        {
            UserId = user.Id,
            Balance = 0,
            TotalEarned = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmLink = $"{confirmEmailCallbackUrl}?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailConfirmationAsync(user.Email!, user.FullName, confirmLink);

        _logger.LogInformation("Confirmation email sent to {Email}.", model.Email);

        return (true, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, string Message)> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "Liên kết xác minh không hợp lệ.");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} confirmed email.", userId);
            return (true, "Email xác minh thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        return (false, "Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
    }

    public async Task<(bool Success, bool IsLockedOut, bool IsNotAllowed, string FullName)> LoginAsync(LoginViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !user.IsActive)
            return (false, false, false, string.Empty);

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return (true, false, false, user.FullName);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            return (false, true, false, string.Empty);
        }

        if (result.IsNotAllowed)
        {
            _logger.LogWarning("User {Email} login not allowed (email not confirmed).", model.Email);
            return (false, false, true, string.Empty);
        }

        return (false, false, false, string.Empty);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
    }

    public async Task<ProfileViewModel?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new ProfileViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(string userId, EditProfileViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, ["Không tìm thấy người dùng."]);

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        if (model.AvatarFile != null && model.AvatarFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(model.AvatarFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await model.AvatarFile.CopyToAsync(fileStream);

            user.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
        }

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? (true, Enumerable.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(string userId, ChangePasswordViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, ["Không tìm thấy người dùng."]);

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserId} changed password successfully.", userId);
            return (true, Enumerable.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description));
    }

    public async Task<string?> GeneratePasswordResetLinkAsync(string email, string callbackBaseUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{callbackBaseUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, link);

        _logger.LogInformation("Password reset email sent to {Email}.", email);

        return link;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(ResetPasswordViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return (true, Enumerable.Empty<string>()); // Không tiết lộ user không tồn tại

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        // Nếu email chưa được xác nhận, tự động xác nhận vì đã chứng minh sở hữu email qua link reset
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        return (true, Enumerable.Empty<string>());
    }

}
