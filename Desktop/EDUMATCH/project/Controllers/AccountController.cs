using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ITutorService _tutorService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly EduMatchDbContext _db;

    public AccountController(IAccountService accountService, ITutorService tutorService,
        UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager,
        EduMatchDbContext db)
    {
        _accountService = accountService;
        _tutorService = tutorService;
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
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

            // Redirect theo role
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                    return RedirectToAction("Transactions", "Admin");
                if (roles.Contains("Tutor"))
                    return RedirectToAction("Profile", "Account");
            }

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

        if (model.Roles.Contains("Tutor"))
        {
            model.TutorPosts = await _tutorService.GetPostsByTutorAsync(userId);
            model.TotalContracts = await _db.Contracts.CountAsync(c => c.TutorId == userId);
            model.PendingBookings = await _db.BookingRequests.CountAsync(b => b.TutorId == userId && b.Status == BookingStatus.Pending);
            model.CompletedSessions = await _db.Sessions.CountAsync(s => s.Contract.TutorId == userId && s.Status == SessionStatus.Completed);
        }

        if (model.Roles.Contains("Student"))
        {
            model.TotalContracts = await _db.Contracts.CountAsync(c => c.StudentId == userId);
            model.PendingBookings = await _db.BookingRequests.CountAsync(b => b.StudentId == userId && b.Status == BookingStatus.Pending);
            model.CompletedSessions = await _db.Sessions.CountAsync(s => s.Contract.StudentId == userId && s.Status == SessionStatus.Completed);
        }

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        model.WalletBalance = wallet?.Balance ?? 0;

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

    // GET: /Account/CreateAdmin
    [HttpGet]
    public IActionResult CreateAdmin() => View();

    // POST: /Account/CreateAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdmin(string fullName, string email, string password)
    {
        // Chỉ cho tạo nếu chưa có admin nào
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (admins.Any())
        {
            TempData["ErrorMessage"] = "Đã có admin trong hệ thống. Không thể tạo thêm qua đây.";
            return RedirectToAction("Login");
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            ViewBag.Error = "Email này đã tồn tại!";
            return View();
        }

        var admin = new ApplicationUser
        {
            FullName = fullName,
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        await _userManager.AddToRoleAsync(admin, "Admin");

        TempData["SuccessMessage"] = $"Tạo admin '{fullName}' thành công! Hãy đăng nhập.";
        return RedirectToAction("Login");
    }
}
