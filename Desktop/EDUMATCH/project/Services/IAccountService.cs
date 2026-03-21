using EduMatch.ViewModels;

namespace EduMatch.Services;

public interface IAccountService
{
    Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model, string confirmEmailCallbackUrl);
    Task<(bool Success, string Message)> ConfirmEmailAsync(string userId, string token);
    Task<(bool Success, bool IsLockedOut, bool IsNotAllowed, string FullName)> LoginAsync(LoginViewModel model);
    Task LogoutAsync();
    Task<ProfileViewModel?> GetProfileAsync(string userId);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(string userId, EditProfileViewModel model);
    Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(string userId, ChangePasswordViewModel model);
    Task<string?> GeneratePasswordResetLinkAsync(string email, string callbackBaseUrl);
    Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(ResetPasswordViewModel model);
}
