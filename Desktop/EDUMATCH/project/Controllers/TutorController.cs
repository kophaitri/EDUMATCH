using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

[Authorize(Policy = "TutorOnly")]
public class TutorController : Controller
{
    private readonly ITutorService _tutorService;
    private readonly ITutorDashboardService _dashboardService;
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TutorController(
        ITutorService tutorService,
        ITutorDashboardService dashboardService,
        EduMatchDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _tutorService = tutorService;
        _dashboardService = dashboardService;
        _db = db;
        _userManager = userManager;
    }

    // ============================================================
    // Dashboard & Statistics
    // ============================================================

    // GET: /Tutor/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var dto = await _dashboardService.GetDashboardAsync(userId);
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải dashboard: " + ex.Message;
            return View(new EduMatch.DTOs.Tutor.TutorDashboardDto());
        }
    }

    // GET: /Tutor/StatsSessions?year=2025
    public async Task<IActionResult> StatsSessions(int? year)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var dto = await _dashboardService.GetSessionStatsAsync(userId, year);
            ViewBag.SelectedYear = dto.Year;
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải thống kê buổi học: " + ex.Message;
            return View(new EduMatch.DTOs.Tutor.TutorSessionStatsDto());
        }
    }

    // GET: /Tutor/StatsRevenue?year=2025
    public async Task<IActionResult> StatsRevenue(int? year)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var dto = await _dashboardService.GetRevenueStatsAsync(userId, year);
            ViewBag.SelectedYear = dto.Year;
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải thống kê doanh thu: " + ex.Message;
            return View(new EduMatch.DTOs.Tutor.TutorRevenueStatsDto());
        }
    }

    // GET: /Tutor/StatsReputation
    public async Task<IActionResult> StatsReputation()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var dto = await _dashboardService.GetReputationStatsAsync(userId);
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải thống kê uy tín: " + ex.Message;
            return View(new EduMatch.DTOs.Tutor.TutorReputationStatsDto());
        }
    }

    // GET: /Tutor/Posts
    [HttpGet]
    public async Task<IActionResult> Posts()
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var posts = await _tutorService.GetPostsByTutorAsync(tutorId);
        return View(posts);
    }

    // GET: /Tutor/PostDetail/5
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> PostDetail(int id)
    {
        var post = await _tutorService.GetPostByIdAsync(id);
        if (post == null)
            return NotFound();

        // Chỉ chủ bài viết mới xem được bài nháp
        if (!post.IsPublished)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != post.TutorId)
                return NotFound();
        }

        return View(post);
    }

    // GET: /Tutor/CreatePost
    [HttpGet]
    public IActionResult CreatePost() => View();

    // POST: /Tutor/CreatePost
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CreateTutorPostViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, _, errors) = await _tutorService.CreatePostAsync(tutorId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Đăng bài viết thành công!";
            return RedirectToAction(nameof(Posts));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // GET: /Tutor/EditPost/5
    [HttpGet]
    public async Task<IActionResult> EditPost(int id)
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var post = await _tutorService.GetPostByIdAsync(id);

        if (post == null || post.TutorId != tutorId)
            return NotFound();

        var model = new EditTutorPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            ExistingImageUrls = post.ImageUrls,
            IsPublished = post.IsPublished
        };

        return View(model);
    }

    // POST: /Tutor/EditPost/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(EditTutorPostViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, errors) = await _tutorService.UpdatePostAsync(tutorId, model);

        if (success)
        {
            TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
            return RedirectToAction(nameof(Posts));
        }

        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // POST: /Tutor/DeletePost/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _tutorService.DeletePostAsync(tutorId, id);

        if (success)
            TempData["SuccessMessage"] = "Xóa bài viết thành công!";
        else
            TempData["ErrorMessage"] = error;

        return RedirectToAction(nameof(Posts));
    }

    // ============================================================
    // Subject Management
    // ============================================================

    // GET: /Tutor/ManageSubjects
    public async Task<IActionResult> ManageSubjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var tutorSubjects = await _db.TutorSubjects
            .Include(ts => ts.Subject)
            .Include(ts => ts.GradeLevel)
            .Where(ts => ts.TutorId == profile.Id)
            .OrderBy(ts => ts.Subject.Name)
            .ThenBy(ts => ts.GradeLevel.DisplayOrder)
            .ToListAsync();

        ViewBag.TutorSubjects = tutorSubjects;
        ViewBag.Subjects = await _db.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.GradeLevels = await _db.GradeLevels.OrderBy(g => g.DisplayOrder).ToListAsync();

        return View();
    }

    // POST: /Tutor/AddSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSubject(int subjectId, int gradeLevelId, decimal hourlyRate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        // Check duplicate
        var exists = await _db.TutorSubjects.AnyAsync(ts =>
            ts.TutorId == profile.Id && ts.SubjectId == subjectId && ts.GradeLevelId == gradeLevelId);

        if (exists)
        {
            TempData["ErrorMessage"] = "Bạn đã đăng ký môn này với cấp độ này rồi.";
            return RedirectToAction("ManageSubjects");
        }

        if (hourlyRate <= 0)
        {
            TempData["ErrorMessage"] = "Giá/giờ phải lớn hơn 0.";
            return RedirectToAction("ManageSubjects");
        }

        _db.TutorSubjects.Add(new TutorSubject
        {
            TutorId = profile.Id,
            SubjectId = subjectId,
            GradeLevelId = gradeLevelId,
            HourlyRate = hourlyRate
        });
        await _db.SaveChangesAsync();

        // Update min/max rates on TutorProfile
        await UpdateHourlyRateRange(profile);

        TempData["SuccessMessage"] = "Thêm môn dạy thành công!";
        return RedirectToAction("ManageSubjects");
    }

    // POST: /Tutor/RemoveSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var tutorSubject = await _db.TutorSubjects
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TutorId == profile.Id);

        if (tutorSubject == null) return NotFound();

        _db.TutorSubjects.Remove(tutorSubject);
        await _db.SaveChangesAsync();

        await UpdateHourlyRateRange(profile);

        TempData["SuccessMessage"] = "Đã xóa môn dạy.";
        return RedirectToAction("ManageSubjects");
    }

    // POST: /Tutor/UpdateSubjectRate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSubjectRate(int id, decimal hourlyRate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var tutorSubject = await _db.TutorSubjects
            .FirstOrDefaultAsync(ts => ts.Id == id && ts.TutorId == profile.Id);

        if (tutorSubject == null) return NotFound();

        if (hourlyRate <= 0)
        {
            TempData["ErrorMessage"] = "Giá/giờ phải lớn hơn 0.";
            return RedirectToAction("ManageSubjects");
        }

        tutorSubject.HourlyRate = hourlyRate;
        await _db.SaveChangesAsync();

        await UpdateHourlyRateRange(profile);

        TempData["SuccessMessage"] = "Cập nhật giá thành công!";
        return RedirectToAction("ManageSubjects");
    }

    private async Task UpdateHourlyRateRange(TutorProfile profile)
    {
        var rates = await _db.TutorSubjects
            .Where(ts => ts.TutorId == profile.Id)
            .Select(ts => ts.HourlyRate)
            .ToListAsync();

        if (rates.Any())
        {
            profile.HourlyRateMin = rates.Min();
            profile.HourlyRateMax = rates.Max();
        }
        else
        {
            profile.HourlyRateMin = 0;
            profile.HourlyRateMax = 0;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ============================================================
    // Booking Management
    // ============================================================

    // GET: /Tutor/Bookings
    public async Task<IActionResult> Bookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bookings = await _db.BookingRequests
            .Include(b => b.Student)
            .Include(b => b.Subject)
            .Where(b => b.TutorId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    // GET: /Tutor/BookingDetail/{id}
    public async Task<IActionResult> BookingDetail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.BookingRequests
            .Include(b => b.Student)
            .Include(b => b.Subject)
            .Include(b => b.GradeLevel)
            .FirstOrDefaultAsync(b => b.Id == id && b.TutorId == userId);

        if (booking == null) return NotFound();

        return View(booking);
    }

    // POST: /Tutor/AcceptBooking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.BookingRequests
            .Include(b => b.Subject)
            .FirstOrDefaultAsync(b => b.Id == id && b.TutorId == userId && b.Status == BookingStatus.Pending);

        if (booking == null) return NotFound();

        booking.Status = BookingStatus.Accepted;
        booking.RespondedAt = DateTime.UtcNow;

        // Lấy hourly rate từ TutorSubject
        var tutorSubject = await _db.TutorSubjects
            .FirstOrDefaultAsync(ts => ts.TutorId == userId && ts.SubjectId == booking.SubjectId && ts.GradeLevelId == booking.GradeLevelId);

        var hourlyRate = tutorSubject?.HourlyRate ?? 0;

        // Tự động tạo hợp đồng
        var contract = new Contract
        {
            BookingRequestId = booking.Id,
            StudentId = booking.StudentId,
            TutorId = userId,
            SubjectId = booking.SubjectId,
            GradeLevelId = booking.GradeLevelId,
            HourlyRate = hourlyRate,
            TotalSessions = booking.SessionsPerWeek * booking.DurationWeeks,
            StartDate = booking.PreferredStartDate,
            EndDate = booking.PreferredStartDate.AddDays(booking.DurationWeeks * 7),
            Status = ContractStatus.Active
        };
        _db.Contracts.Add(contract);

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã chấp nhận booking và tạo hợp đồng thành công!";
        return RedirectToAction("Bookings");
    }

    // POST: /Tutor/RejectBooking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == id && b.TutorId == userId && b.Status == BookingStatus.Pending);

        if (booking == null) return NotFound();

        booking.Status = BookingStatus.Rejected;
        booking.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã từ chối booking.";
        return RedirectToAction("Bookings");
    }

    // ============================================================
    // Contract Management
    // ============================================================

    // GET: /Tutor/Contracts
    public async Task<IActionResult> Contracts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var contracts = await _db.Contracts
            .Include(c => c.Student)
            .Include(c => c.Subject)
            .Include(c => c.Sessions)
            .Where(c => c.TutorId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(contracts);
    }

    // GET: /Tutor/ContractDetail/{id}
    public async Task<IActionResult> ContractDetail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var contract = await _db.Contracts
            .Include(c => c.Student)
            .Include(c => c.Subject)
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == id && c.TutorId == userId);

        if (contract == null) return NotFound();

        return View(contract);
    }

    // POST: /Tutor/AddSession
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSession(int contractId, DateTime scheduledAt, int durationMinutes)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.TutorId == userId);
        if (contract == null) return NotFound();

        _db.Sessions.Add(new Session
        {
            ContractId = contractId,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Status = SessionStatus.Scheduled
        });
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã thêm buổi học thành công!";
        return RedirectToAction("ContractDetail", new { id = contractId });
    }

    // ============================================================
    // Wallet Management
    // ============================================================

    // GET: /Tutor/Wallet
    public async Task<IActionResult> Wallet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }

    // GET: /Tutor/WalletTransactions
    public async Task<IActionResult> WalletTransactions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        var transactions = await _db.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.Wallet = wallet;
        return View(transactions);
    }

    // GET: /Tutor/Withdraw
    public async Task<IActionResult> Withdraw()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        ViewBag.Balance = wallet?.Balance ?? 0;
        return View();
    }

    // POST: /Tutor/Withdraw
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(decimal amount, string bankAccount, string bankName)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        if (amount <= 0 || amount > wallet.Balance)
        {
            TempData["ErrorMessage"] = "Số tiền rút không hợp lệ hoặc vượt quá số dư";
            ViewBag.Balance = wallet.Balance;
            return View();
        }

        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Pending,
            Description = $"Rút tiền về {bankName} - {bankAccount}",
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance - amount
        };
        _db.Transactions.Add(transaction);

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Yêu cầu rút {amount:N0} VNĐ đã được ghi nhận. Tiền sẽ về trong 1-3 ngày làm việc.";
        return RedirectToAction("Wallet");
    }

    // ============================================================
    // Teaching Style Management
    // ============================================================

    // GET: /Tutor/TeachingStyles
    public async Task<IActionResult> TeachingStyles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var myStyleIds = await _db.TutorTeachingStyles
            .Where(ts => ts.TutorId == profile.Id)
            .Select(ts => ts.TeachingStyleId)
            .ToListAsync();

        ViewBag.AllStyles = await _db.TeachingStyles.OrderBy(s => s.Name).ToListAsync();
        ViewBag.MyStyleIds = myStyleIds;
        return View();
    }

    // POST: /Tutor/AddTeachingStyle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeachingStyle(int teachingStyleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var exists = await _db.TutorTeachingStyles
            .AnyAsync(ts => ts.TutorId == profile.Id && ts.TeachingStyleId == teachingStyleId);

        if (!exists)
        {
            _db.TutorTeachingStyles.Add(new TutorTeachingStyle
            {
                TutorId = profile.Id,
                TeachingStyleId = teachingStyleId
            });
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm phong cách dạy.";
        }
        return RedirectToAction("TeachingStyles");
    }

    // POST: /Tutor/RemoveTeachingStyle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeachingStyle(int teachingStyleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var item = await _db.TutorTeachingStyles
            .FirstOrDefaultAsync(ts => ts.TutorId == profile.Id && ts.TeachingStyleId == teachingStyleId);

        if (item != null)
        {
            _db.TutorTeachingStyles.Remove(item);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa phong cách dạy.";
        }
        return RedirectToAction("TeachingStyles");
    }

    // ============================================================
    // Review Complaints
    // ============================================================

    // GET: /Tutor/ComplainReview/{reviewId}
    [HttpGet]
    public async Task<IActionResult> ComplainReview(int reviewId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var review = await _db.Reviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.RevieweeId == userId);

        if (review == null) return NotFound();

        var alreadyComplained = await _db.ReviewComplaints
            .AnyAsync(c => c.ReviewId == reviewId && c.ComplainantId == userId);

        if (alreadyComplained)
        {
            TempData["ErrorMessage"] = "Bạn đã khiếu nại đánh giá này rồi.";
            return RedirectToAction("Dashboard");
        }

        ViewBag.Review = review;
        return View();
    }

    // POST: /Tutor/ComplainReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ComplainReview(int reviewId, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.RevieweeId == userId);

        if (review == null) return NotFound();

        var alreadyComplained = await _db.ReviewComplaints
            .AnyAsync(c => c.ReviewId == reviewId && c.ComplainantId == userId);

        if (alreadyComplained)
        {
            TempData["ErrorMessage"] = "Bạn đã khiếu nại đánh giá này rồi.";
            return RedirectToAction("Dashboard");
        }

        _db.ReviewComplaints.Add(new ReviewComplaint
        {
            ReviewId = reviewId,
            ComplainantId = userId,
            Reason = reason
        });
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Khiếu nại đã được gửi. Admin sẽ xem xét trong thời gian sớm nhất.";
        return RedirectToAction("Dashboard");
    }
}
