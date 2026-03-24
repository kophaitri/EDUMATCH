using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;

namespace EduMatch.Controllers;

public class StudentController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITutorService _tutorService;

    public StudentController(EduMatchDbContext db, UserManager<ApplicationUser> userManager, ITutorService tutorService)
    {
        _db = db;
        _userManager = userManager;
        _tutorService = tutorService;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // GET: /Student/Search
    public async Task<IActionResult> Search(string? keyword, int? subjectId, int? gradeLevelId, decimal? minRate, decimal? maxRate)
    {
        var query = _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.GradeLevel)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(t => t.User.FullName.Contains(keyword) || (t.Bio != null && t.Bio.Contains(keyword)));

        if (subjectId.HasValue)
            query = query.Where(t => t.TutorSubjects.Any(ts => ts.SubjectId == subjectId));

        if (gradeLevelId.HasValue)
            query = query.Where(t => t.TutorSubjects.Any(ts => ts.GradeLevelId == gradeLevelId));

        if (minRate.HasValue)
            query = query.Where(t => t.HourlyRateMin >= minRate);

        if (maxRate.HasValue)
            query = query.Where(t => t.HourlyRateMax <= maxRate);

        ViewBag.Tutors = await query.ToListAsync();
        ViewBag.Subjects = await _db.Subjects.Where(s => s.IsActive).ToListAsync();
        ViewBag.GradeLevels = await _db.GradeLevels.OrderBy(g => g.DisplayOrder).ToListAsync();
        ViewBag.Keyword = keyword;
        ViewBag.SubjectId = subjectId;
        ViewBag.GradeLevelId = gradeLevelId;
        ViewBag.MinRate = minRate;
        ViewBag.MaxRate = maxRate;

        return View();
    }

    // GET: /Student/TutorProfile/{id}
    public async Task<IActionResult> TutorProfile(string id)
    {
        var tutor = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.GradeLevel)
            .Include(t => t.Certificates)
            .FirstOrDefaultAsync(t => t.UserId == id);

        if (tutor == null) return NotFound();

        // Load reviews via contracts
        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.Contract.TutorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Reviews = reviews;

        // Load published posts
        var allPosts = await _tutorService.GetPostsByTutorAsync(id);
        ViewBag.Posts = allPosts.Where(p => p.IsPublished).ToList();

        // Stats
        ViewBag.TotalContracts = await _db.Contracts.CountAsync(c => c.TutorId == id);
        ViewBag.CompletedSessions = await _db.Sessions.CountAsync(s => s.Contract.TutorId == id && s.Status == SessionStatus.Completed);

        return View(tutor);
    }

    // GET: /Student/Subjects
    public async Task<IActionResult> Subjects()
    {
        var subjects = await _db.Subjects
            .Where(s => s.IsActive)
            .Include(s => s.TutorSubjects)
            .ToListAsync();

        return View(subjects);
    }

    // GET: /Student/SubjectTutors/{subjectId}
    public async Task<IActionResult> SubjectTutors(int subjectId)
    {
        var subject = await _db.Subjects.FindAsync(subjectId);
        if (subject == null) return NotFound();

        var tutors = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .Where(t => t.TutorSubjects.Any(ts => ts.SubjectId == subjectId))
            .ToListAsync();

        ViewBag.Subject = subject;
        return View(tutors);
    }

    // GET: /Student/CreateBooking?tutorId=xxx
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateBooking(string tutorId)
    {
        var tutor = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.GradeLevel)
            .FirstOrDefaultAsync(t => t.UserId == tutorId);

        if (tutor == null) return NotFound();

        ViewBag.Tutor = tutor;
        ViewBag.Subjects = tutor.TutorSubjects.Select(ts => ts.Subject).Distinct().ToList();
        ViewBag.GradeLevels = tutor.TutorSubjects.Select(ts => ts.GradeLevel).Distinct().ToList();

        return View();
    }

    // POST: /Student/CreateBooking
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateBooking(string tutorId, string? message, int subjectId, int gradeLevelId, DateTime preferredStartDate, int sessionsPerWeek, int durationWeeks)
    {
        var userId = GetUserId()!;

        var booking = new BookingRequest
        {
            StudentId = userId,
            TutorId = tutorId,
            SubjectId = subjectId,
            GradeLevelId = gradeLevelId,
            Message = message,
            PreferredStartDate = preferredStartDate,
            SessionsPerWeek = sessionsPerWeek,
            DurationWeeks = durationWeeks,
            Status = BookingStatus.Pending
        };

        _db.BookingRequests.Add(booking);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gửi yêu cầu thành công! Vui lòng chờ gia sư phản hồi.";
        return RedirectToAction("Bookings");
    }

    // GET: /Student/Bookings
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Bookings()
    {
        var userId = GetUserId()!;

        var bookings = await _db.BookingRequests
            .Include(b => b.Tutor)
            .Include(b => b.Subject)
            .Where(b => b.StudentId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    // GET: /Student/BookingDetail/{id}
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> BookingDetail(int id)
    {
        var userId = GetUserId()!;

        var booking = await _db.BookingRequests
            .Include(b => b.Tutor)
            .Include(b => b.Subject)
            .Include(b => b.GradeLevel)
            .FirstOrDefaultAsync(b => b.Id == id && b.StudentId == userId);

        if (booking == null) return NotFound();

        return View(booking);
    }

    // GET: /Student/Contracts
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Contracts()
    {
        var userId = GetUserId()!;

        var contracts = await _db.Contracts
            .Include(c => c.Tutor)
            .Include(c => c.Subject)
            .Where(c => c.StudentId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(contracts);
    }

    // GET: /Student/ContractDetail/{id}
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> ContractDetail(int id)
    {
        var userId = GetUserId()!;

        var contract = await _db.Contracts
            .Include(c => c.Tutor)
            .Include(c => c.Subject)
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == id && c.StudentId == userId);

        if (contract == null) return NotFound();

        return View(contract);
    }

    // GET: /Student/Schedule
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Schedule()
    {
        var userId = GetUserId()!;

        var sessions = await _db.Sessions
            .Include(s => s.Contract).ThenInclude(c => c.Tutor)
            .Include(s => s.Contract).ThenInclude(c => c.Subject)
            .Where(s => s.Contract.StudentId == userId)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return View(sessions);
    }

    // GET: /Student/SessionDetail/{id}
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> SessionDetail(int id)
    {
        var userId = GetUserId()!;

        var session = await _db.Sessions
            .Include(s => s.Contract).ThenInclude(c => c.Tutor)
            .Include(s => s.Contract).ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Contract.StudentId == userId);

        if (session == null) return NotFound();

        return View(session);
    }

    // GET: /Student/Wallet
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Wallet()
    {
        var userId = GetUserId()!;

        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }

    // GET: /Student/TopUp
    [Authorize(Policy = "StudentOnly")]
    public IActionResult TopUp() => View();

    // POST: /Student/TopUp
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> TopUp(decimal amount)
    {
        if (amount <= 0)
        {
            ModelState.AddModelError("", "Số tiền nạp phải lớn hơn 0");
            return View();
        }

        var userId = GetUserId()!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Description = "Nạp tiền vào ví",
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance + amount
        };
        _db.Transactions.Add(transaction);

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Nạp tiền thành công! Số dư hiện tại: {wallet.Balance:N0} VNĐ";
        return RedirectToAction("Wallet");
    }

    // GET: /Student/WalletTransactions
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> WalletTransactions()
    {
        var userId = GetUserId()!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        var transactions = await _db.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.Wallet = wallet;
        return View(transactions);
    }
}
