using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        if (model.Role != "Tutor" && model.Role != "Student")
        {
            ModelState.AddModelError("Role", "Vai trò không hợp lệ");
            return View(model);
        }

        var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", null, Request.Scheme)!;
        var (success, errors) = await _accountService.RegisterAsync(model, callbackUrl);

        if (success)
            return RedirectToAction(nameof(RegisterConfirmation), new { email = model.Email });

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Account/RegisterConfirmation
    [HttpGet]
    public IActionResult RegisterConfirmation(string email)
    {
        ViewData["Email"] = email;
        return View();
    }

    // GET: /Account/ConfirmEmail
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        if (userId == null || token == null)
            return BadRequest("Liên kết xác minh không hợp lệ.");

        var (success, message) = await _accountService.ConfirmEmailAsync(userId, token);
        ViewData["Success"] = success;
        ViewData["Message"] = message;
        return View();
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, isLockedOut, isNotAllowed, fullName) = await _accountService.LoginAsync(model);

        if (success)
        {
            TempData["SuccessMessage"] = $"Chào mừng trở lại, {fullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (isLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần.");
            return View(model);
        }

        if (isNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng xác nhận email trước khi đăng nhập. Kiểm tra hộp thư của bạn.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        TempData["SuccessMessage"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Profile
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return NotFound();

        var model = await _accountService.GetProfileAsync(userId);
        if (model == null) return NotFound();

        return View(model);
    }

    // GET: /Account/EditProfile
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return NotFound();

        var profile = await _accountService.GetProfileAsync(userId);
        if (profile == null) return NotFound();

        var model = new EditProfileViewModel
        {
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            CurrentAvatarUrl = profile.AvatarUrl
        };

        return View(model);
    }

    // POST: /Account/EditProfile
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return NotFound();

        var (success, errors) = await _accountService.UpdateProfileAsync(userId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Account/ChangePassword
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    // POST: /Account/ChangePassword
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return NotFound();

        var (success, errors) = await _accountService.ChangePasswordAsync(userId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    // POST: /Account/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var callbackBaseUrl = Url.Action(nameof(ResetPassword), "Account", null, Request.Scheme)!;
        await _accountService.GeneratePasswordResetLinkAsync(model.Email, callbackBaseUrl);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    // GET: /Account/ForgotPasswordConfirmation
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // GET: /Account/ResetPassword
    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
            return BadRequest("Token hoặc email không hợp lệ.");

        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, errors) = await _accountService.ResetPasswordAsync(model);

        if (success)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Account/ResetPasswordConfirmation
    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
