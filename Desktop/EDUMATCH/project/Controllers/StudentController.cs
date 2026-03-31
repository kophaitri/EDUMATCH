using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using Microsoft.EntityFrameworkCore;
using EduMatch.Models;
using EduMatch.Models.Enums;

namespace EduMatch.Controllers;

[Route("[controller]")]
public class StudentController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITutorService _tutorService;
    private readonly IConfiguration _config;

    public StudentController(EduMatchDbContext db, UserManager<ApplicationUser> userManager, ITutorService tutorService, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _tutorService = tutorService;
        _config = config;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ============================================================
    // SEARCH & PROFILE
    // ============================================================

    [HttpGet]
    [HttpGet("Search")]  // ← Thêm route rõ ràng
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

    [HttpGet("TutorProfile/{id}")]  // ← Thêm route rõ ràng
    public async Task<IActionResult> TutorProfile(string id)
    {
        var tutor = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.GradeLevel)
            .Include(t => t.Certificates)
            .FirstOrDefaultAsync(t => t.UserId == id);

        if (tutor == null) return NotFound();

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.Contract.TutorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Reviews = reviews;

        var allPosts = await _tutorService.GetPostsByTutorAsync(id);
        ViewBag.Posts = allPosts.Where(p => p.IsPublished).ToList();

        ViewBag.TotalContracts = await _db.Contracts.CountAsync(c => c.TutorId == id);
        ViewBag.CompletedSessions = await _db.Sessions.CountAsync(s => s.Contract.TutorId == id && s.Status == SessionStatus.Completed);

        return View(tutor);
    }

    [HttpGet("Subjects")]  // ← Thêm route rõ ràng
    public async Task<IActionResult> Subjects()
    {
        var subjects = await _db.Subjects
            .Where(s => s.IsActive)
            .Include(s => s.TutorSubjects)
            .ToListAsync();

        return View(subjects);
    }

    [HttpGet("SubjectTutors/{subjectId}")]  // ← Thêm route rõ ràng
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

    // ============================================================
    // BOOKING
    // ============================================================

    [HttpGet("CreateBooking")]  // ← Thêm route rõ ràng
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

    [HttpPost("CreateBooking")]  // ← Thêm route rõ ràng
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateBooking(string tutorId, string? message, int subjectId, int gradeLevelId, DateTime preferredStartDate, int sessionsPerWeek, int durationWeeks, decimal sessionDurationHours = 1.5m)
    {
        var userId = GetUserId()!;

        // Lấy giá theo môn/khối của gia sư
        var tutorProfile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == tutorId);
        var tutorSubject = await _db.TutorSubjects.FirstOrDefaultAsync(ts =>
            ts.TutorId == tutorProfile!.Id && ts.SubjectId == subjectId && ts.GradeLevelId == gradeLevelId);

        var hourlyRate = tutorSubject?.HourlyRate ?? 0;
        var totalSessions = sessionsPerWeek * durationWeeks;
        var totalAmount = hourlyRate * sessionDurationHours * totalSessions;

        var refCode = "BK" + Guid.NewGuid().ToString("N")[..8].ToUpper();

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
            SessionDurationHours = sessionDurationHours,
            HourlyRate = hourlyRate,
            TotalAmount = totalAmount,
            PaymentOrderId = refCode,
            Status = BookingStatus.PendingPayment
        };

        _db.BookingRequests.Add(booking);
        await _db.SaveChangesAsync();

        return RedirectToAction("BookingPaymentQR", new { bookingId = booking.Id });
    }

    // GET: /Student/BookingPaymentQR
    [HttpGet("[action]")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> BookingPaymentQR(int bookingId)
    {
        var userId = GetUserId()!;
        var booking = await _db.BookingRequests
            .Include(b => b.Tutor)
            .Include(b => b.Subject)
            .Include(b => b.GradeLevel)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == userId);

        if (booking == null) return NotFound();

        ViewBag.BankCode = _config["SePay:BankCode"];
        ViewBag.AccountNumber = _config["SePay:AccountNumber"];
        ViewBag.AccountName = _config["SePay:AccountName"];
        return View(booking);
    }

    // GET: /Student/CheckBookingPayment
    [HttpGet("[action]")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CheckBookingPayment(int bookingId)
    {
        var userId = GetUserId()!;
        var booking = await _db.BookingRequests.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == userId);
        if (booking == null) return NotFound();
        return Json(new { status = booking.Status.ToString() });
    }

    // POST: /Student/ConfirmSession
    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> ConfirmSession(int sessionId)
    {
        var userId = GetUserId()!;
        var session = await _db.Sessions
            .Include(s => s.Contract)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Contract.StudentId == userId);

        if (session == null) return NotFound();
        if (session.Status != SessionStatus.PendingConfirmation) return BadRequest();

        session.Status = SessionStatus.Completed;
        session.StudentConfirmedAt = DateTime.UtcNow;

        await ReleaseEarningAsync(session);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xác nhận buổi học hoàn thành!";
        return RedirectToAction("Schedule");
    }

    private async Task ReleaseEarningAsync(Session session)
    {
        if (session.EarningReleased) return;

        var contract = session.Contract ?? await _db.Contracts.FindAsync(session.ContractId);
        if (contract == null) return;

        var tutorWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == contract.TutorId);
        if (tutorWallet == null) return;

        const decimal platformFee = 0.15m;
        var sessionEarning = contract.HourlyRate * (session.DurationMinutes / 60m);
        var tutorEarning = sessionEarning * (1 - platformFee);

        var transaction = new Transaction
        {
            WalletId = tutorWallet.Id,
            Amount = tutorEarning,
            Type = TransactionType.Earning,
            Status = TransactionStatus.Completed,
            Description = $"Thu nhập buổi học #{session.Id} (sau chiết khấu 15%)",
            ReferenceId = session.Id.ToString(),
            BalanceBefore = tutorWallet.Balance,
            BalanceAfter = tutorWallet.Balance + tutorEarning
        };
        _db.Transactions.Add(transaction);

        tutorWallet.Balance += tutorEarning;
        tutorWallet.TotalEarned += tutorEarning;
        tutorWallet.UpdatedAt = DateTime.UtcNow;

        session.EarningReleased = true;
    }

    [HttpGet("Bookings")]  // ← Thêm route rõ ràng
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

    [HttpGet("BookingDetail/{id}")]  // ← Thêm route rõ ràng
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

    // ============================================================
    // CONTRACTS (Default Route)
    // ============================================================

    [HttpGet("")]  // ← Default route cho /Student
    [HttpGet("Contracts")]  // ← Explicit route
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

        // Truyền danh sách contract đã review để view hiển thị nút đúng
        var reviewedContractIds = await _db.Reviews
            .Where(r => r.ReviewerId == userId)
            .Select(r => r.ContractId)
            .ToListAsync();
        ViewBag.ReviewedContractIds = reviewedContractIds;

        return View(contracts);
    }

    [HttpGet("ContractDetail/{id}")]  // ← Thêm route rõ ràng
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

    // ============================================================
    // SCHEDULE & SESSIONS
    // ============================================================

    [HttpGet("Schedule")]  // ← Thêm route rõ ràng
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

    [HttpGet("SessionDetail/{id}")]  // ← Thêm route rõ ràng
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

    // ============================================================
    // WALLET
    // ============================================================

    [HttpGet("Wallet")]  // ← Thêm route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Wallet()
    {
        var userId = GetUserId()!;

        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }

    [HttpGet("TopUp")]  // ← Thêm route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public IActionResult TopUp() => View();

    [HttpPost("TopUp")]  // ← Thêm route rõ ràng
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> TopUp(decimal amount)
    {
        if (amount < 10000)
        {
            ModelState.AddModelError("", "Số tiền nạp tối thiểu 10,000 VNĐ");
            return View();
        }

        var userId = GetUserId()!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        var refCode = "DT" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        var order = new PaymentOrder
        {
            UserId = userId,
            Amount = amount,
            PaymentMethod = PaymentMethod.BankTransfer,
            Status = TransactionStatus.Pending,
            PaymentGatewayOrderId = refCode
        };
        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync();

        return RedirectToAction("TopUpQR", new { orderId = order.Id });
    }

    // GET: /Student/TopUpQR
    [HttpGet("[action]")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> TopUpQR(int orderId)
    {
        var userId = GetUserId()!;
        var order = await _db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return NotFound();

        ViewBag.BankCode = _config["SePay:BankCode"];
        ViewBag.AccountNumber = _config["SePay:AccountNumber"];
        ViewBag.AccountName = _config["SePay:AccountName"];
        return View(order);
    }

    // GET: /Student/CheckPaymentStatus
    [HttpGet("[action]")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CheckPaymentStatus(int orderId)
    {
        var userId = GetUserId()!;
        var order = await _db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return NotFound();
        return Json(new { status = order.Status.ToString() });
    }

    [HttpGet("WalletTransactions")]  // ← Thêm route rõ ràng
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

    // ============================================================
    // EXAM FEATURES (Items 14-17)
    // ============================================================

    #region 📋 14. Danh sách bài thi có thể làm
    [HttpGet("Exams")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Exams()
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();
        
        var exams = await _db.Exams
            .Include(e => e.Session)
                .ThenInclude(s => s.Contract)
                    .ThenInclude(c => c.Subject)
            .Include(e => e.Submissions)
            .Where(e => e.Status == ExamStatus.Published)
            .Select(e => new StudentExamViewModel
            {
                Id = e.Id,
                Title = e.Title,
                SubjectName = e.Session.Contract.Subject.Name,
                DurationMinutes = e.DurationMinutes,
                PassingScore = e.PassingScore,
                MaxRetakes = e.MaxRetakes,
                CreatedAt = e.CreatedAt,
                HasTaken = e.Submissions.Any(s => s.StudentId == studentId),
                TakenCount = e.Submissions.Count(s => s.StudentId == studentId),
                CanRetake = e.Submissions.Count(s => s.StudentId == studentId) < e.MaxRetakes
            })
            .ToListAsync();
        
        return View(exams);
    }
    #endregion

    #region 📝 15. Làm bài kiểm tra
    [HttpGet("Exams/Take/{examId}")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> TakeExam(int examId)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();
        
        var exam = await _db.Exams
            .Include(e => e.Session)
                .ThenInclude(s => s.Contract)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null || exam.Status != ExamStatus.Published)
            return NotFound();

        var submissionCount = await _db.ExamSubmissions
            .CountAsync(s => s.ExamId == examId && s.StudentId == studentId);

        if (submissionCount >= exam.MaxRetakes)
        {
            TempData["Error"] = "❌ Bạn đã làm quá số lần cho phép";
            return RedirectToAction("Exams");
        }

        var questions = exam.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new ExamQuestionViewModel
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Points = q.Points,
                DisplayOrder = q.DisplayOrder,
                AnswerOptions = q.AnswerOptions
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new ExamAnswerOptionViewModel
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        DisplayOrder = o.DisplayOrder
                    }).ToList()
            }).ToList();

        ViewBag.ExamId = examId;
        ViewBag.ExamTitle = exam.Title;
        ViewBag.Duration = exam.DurationMinutes;
        ViewBag.QuestionCount = questions.Count;

        return View(questions);
    }

    [HttpPost("Exams/Submit/{examId}")]  // ← Route rõ ràng
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> SubmitExam(int examId, List<SubmissionAnswerViewModel> answers)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();
        
        var exam = await _db.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
            return NotFound();

        var submission = new ExamSubmission
        {
            ExamId = examId,
            StudentId = studentId,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Submitted,
            RetakeNumber = await _db.ExamSubmissions
                .CountAsync(s => s.ExamId == examId && s.StudentId == studentId) + 1
        };

        decimal totalScore = 0;
        var submissionAnswers = new List<SubmissionAnswer>();

        foreach (var answerVm in answers)
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == answerVm.QuestionId);
            if (question == null) continue;

            var correctOption = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
            var isCorrect = answerVm.SelectedOptionId == correctOption?.Id;

            var submissionAnswer = new SubmissionAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = answerVm.SelectedOptionId,
                IsCorrect = isCorrect,
                PointsEarned = isCorrect ? question.Points : 0
            };

            submissionAnswers.Add(submissionAnswer);
            totalScore += submissionAnswer.PointsEarned;
        }

        submission.Answers = submissionAnswers;
        submission.TotalScore = totalScore;
        submission.Percentage = exam.Questions.Sum(q => q.Points) > 0 
            ? (totalScore / exam.Questions.Sum(q => q.Points)) * 100m  // ← Fix decimal literal
            : 0m;
        submission.IsPassed = submission.Percentage >= exam.PassingScore;

        _db.ExamSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        TempData["Success"] = "✅ Nộp bài thành công!";
        return RedirectToAction("ExamResult", new { examId, subId = submission.Id });
    }
    #endregion

    #region 📊 16. Xem kết quả bài làm
    [HttpGet("Exams/Result/{examId}/{subId}")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> ExamResult(int examId, int subId)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();
        
        var submission = await _db.ExamSubmissions
            .Include(s => s.Exam)
                .ThenInclude(e => e.Questions)
            .Include(s => s.Answers)
                .ThenInclude(a => a.Question)
            .Include(s => s.Answers)
                .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(s => s.Id == subId && s.StudentId == studentId);

        if (submission == null)
            return NotFound();

        ViewBag.ExamTitle = submission.Exam.Title;
        ViewBag.MaxScore = submission.Exam.Questions.Sum(q => q.Points);

        return View(submission);
    }
    #endregion

    #region 📜 17. Lịch sử tất cả bài làm
    [HttpGet("Exams/History")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> ExamHistory()
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();
        
        var submissions = await _db.ExamSubmissions
            .Include(s => s.Exam)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Subject)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new StudentSubmissionHistoryViewModel
            {
                Id = s.Id,
                ExamTitle = s.Exam.Title,
                SubjectName = s.Exam.Session.Contract.Subject.Name,
                SubmittedAt = s.SubmittedAt,
                TotalScore = s.TotalScore,
                Percentage = s.Percentage,
                IsPassed = s.IsPassed,
                Status = s.Status,
                RetakeNumber = s.RetakeNumber
            })
            .ToListAsync();

        return View(submissions);
    }
    #endregion

    // ============================================================
    // REVIEW FEATURES (Items 18-20)
    // ============================================================

    #region 📝 18. Viết đánh giá gia sư
    [HttpGet("Reviews/Create/{contractId}")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateReview(int contractId)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();

        var contract = await _db.Contracts
            .Include(c => c.Tutor)
            .Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == contractId && c.StudentId == studentId);

        if (contract == null)
        {
            TempData["Error"] = "❌ Không tìm thấy hợp đồng";
            return RedirectToAction("Contracts");
        }

        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ContractId == contractId && r.ReviewerId == studentId);

        if (existingReview != null)
        {
            TempData["Error"] = "⚠️ Bạn đã đánh giá hợp đồng này rồi";
            return RedirectToAction("EditReview", new { id = existingReview.Id });
        }

        ViewBag.ContractId = contractId;
        ViewBag.TutorName = contract.Tutor.FullName;
        ViewBag.SubjectName = contract.Subject.Name;

        return View();
    }

    [HttpPost("Reviews/Create/{contractId}")]  // ← Route rõ ràng
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateReview(int contractId, string comment, int rating)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();

        if (rating < 1 || rating > 5)
        {
            ModelState.AddModelError("", "Đánh giá phải từ 1-5 sao");
            return View();
        }

        var contract = await _db.Contracts
            .Include(c => c.Tutor)
            .FirstOrDefaultAsync(c => c.Id == contractId && c.StudentId == studentId);

        if (contract == null)
        {
            TempData["Error"] = "❌ Không tìm thấy hợp đồng";
            return RedirectToAction("Contracts");
        }

        var review = new Review
        {
            ContractId = contractId,
            ReviewerId = studentId,
            RevieweeId = contract.TutorId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
            IsApproved = true
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        await UpdateTutorRatingAsync(contract.TutorId);

        TempData["Success"] = "✅ Cảm ơn bạn đã đánh giá!";
        return RedirectToAction("Contracts");
    }
    #endregion

    #region 📊 19. Quản lý đánh giá của tôi
    [HttpGet("Reviews")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> MyReviews()
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();

        var reviews = await _db.Reviews
            .Include(r => r.Contract)
                .ThenInclude(c => c.Tutor)
            .Include(r => r.Contract)
                .ThenInclude(c => c.Subject)
            .Include(r => r.Reply)
            .Where(r => r.ReviewerId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(reviews);
    }
    #endregion

    #region ✏️ 20. Chỉnh sửa đánh giá
    [HttpGet("Reviews/Edit/{id}")]  // ← Route rõ ràng
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> EditReview(int id)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();

        var review = await _db.Reviews
            .Include(r => r.Contract)
                .ThenInclude(c => c.Tutor)
            .FirstOrDefaultAsync(r => r.Id == id && r.ReviewerId == studentId);

        if (review == null)
        {
            TempData["Error"] = "❌ Không tìm thấy đánh giá";
            return RedirectToAction("MyReviews");
        }

        ViewBag.ContractId = review.ContractId;
        ViewBag.TutorName = review.Contract.Tutor.FullName;

        return View(review);
    }

    [HttpPost("Reviews/Edit/{id}")]  // ← Route rõ ràng
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> EditReview(int id, string comment, int rating)
    {
        var studentId = GetUserId();
        if (string.IsNullOrEmpty(studentId)) return Challenge();

        if (rating < 1 || rating > 5)
        {
            ModelState.AddModelError("", "Đánh giá phải từ 1-5 sao");
            return View();
        }

        var review = await _db.Reviews
            .Include(r => r.Contract)
            .FirstOrDefaultAsync(r => r.Id == id && r.ReviewerId == studentId);

        if (review == null)
        {
            TempData["Error"] = "❌ Không tìm thấy đánh giá";
            return RedirectToAction("MyReviews");
        }

        review.Rating = rating;
        review.Comment = comment;
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await UpdateTutorRatingAsync(review.Contract.TutorId);

        TempData["Success"] = "✅ Cập nhật đánh giá thành công!";
        return RedirectToAction("MyReviews");
    }
    #endregion

    // Helper method: Update tutor rating
    private async Task UpdateTutorRatingAsync(string tutorId)
    {
        var tutorProfile = await _db.TutorProfiles
            .FirstOrDefaultAsync(tp => tp.UserId == tutorId);

        if (tutorProfile != null)
        {
            var reviews = await _db.Reviews
                .Where(r => r.RevieweeId == tutorId && r.IsApproved)
                .ToListAsync();

            if (reviews.Any())
            {
                tutorProfile.AvgRating = (decimal)reviews.Average(r => r.Rating);  // ← Fix cast
                tutorProfile.TotalReviews = reviews.Count;
                await _db.SaveChangesAsync();
            }
        }
    }

    // ============================================================
    // VIEWMODELS
    // ============================================================
    public class StudentExamViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int PassingScore { get; set; }
        public int MaxRetakes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasTaken { get; set; }
        public int TakenCount { get; set; }
        public bool CanRetake { get; set; }
    }

    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Points { get; set; }
        public int DisplayOrder { get; set; }
        public List<ExamAnswerOptionViewModel> AnswerOptions { get; set; } = new();
    }

    public class ExamAnswerOptionViewModel
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class SubmissionAnswerViewModel
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
    }

    public class StudentSubmissionHistoryViewModel
    {
        public int Id { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public SubmissionStatus Status { get; set; }
        public int RetakeNumber { get; set; }
    }

    // ============================================================
    // TIN NHẮN & THÔNG BÁO (Items 24-26)
    // ============================================================

    #region 💬 24. Hộp thư học viên
    [HttpGet("Messages")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Messages()
    {
        var userId = GetUserId()!;

        var conversations = await _db.ConversationParticipants
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Messages)
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.Conversation)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        // Tất cả gia sư (không cần hợp đồng)
        var contacts = (await _userManager.GetUsersInRoleAsync("Tutor")).ToList();

        ViewBag.CurrentUserId = userId;
        ViewBag.Contacts = contacts;
        return View(conversations);
    }

    [HttpPost("Messages/Start")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> StartConversation(string otherUserId)
    {
        var userId = GetUserId()!;

        var myConvIds = await _db.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToListAsync();

        var existingConvId = await _db.ConversationParticipants
            .Where(cp => cp.UserId == otherUserId && myConvIds.Contains(cp.ConversationId))
            .Select(cp => cp.ConversationId)
            .FirstOrDefaultAsync();

        if (existingConvId != 0)
            return RedirectToAction("Conversation", new { conversationId = existingConvId });

        var otherUser = await _db.Users.FindAsync(otherUserId);
        if (otherUser == null) return NotFound();

        var conversation = new Conversation
        {
            Title = otherUser.FullName,
            IsGroup = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        _db.ConversationParticipants.Add(new ConversationParticipant { ConversationId = conversation.Id, UserId = userId });
        _db.ConversationParticipants.Add(new ConversationParticipant { ConversationId = conversation.Id, UserId = otherUserId });
        await _db.SaveChangesAsync();

        return RedirectToAction("Conversation", new { conversationId = conversation.Id });
    }
    #endregion

    #region 💬 25. Cuộc trò chuyện học viên
    [HttpGet("Messages/{conversationId}")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Conversation(int conversationId)
    {
        var userId = GetUserId()!;

        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!isParticipant) return Forbid();

        var conversation = await _db.Conversations
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return NotFound();

        var participant = await _db.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        if (participant != null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Sidebar: tất cả cuộc trò chuyện
        var allConversations = await _db.ConversationParticipants
            .Include(cp => cp.Conversation).ThenInclude(c => c.Participants).ThenInclude(p => p.User)
            .Include(cp => cp.Conversation).ThenInclude(c => c.Messages)
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.Conversation)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        ViewBag.CurrentUserId = userId;
        ViewBag.AllConversations = allConversations;
        return View(conversation);
    }

    [HttpPost("Messages/{conversationId}/Send")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> SendMessage(int conversationId, string content)
    {
        var userId = GetUserId()!;

        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!isParticipant) return Forbid();

        if (!string.IsNullOrWhiteSpace(content))
        {
            _db.Messages.Add(new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            var conv = await _db.Conversations.FindAsync(conversationId);
            if (conv != null) conv.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Conversation", new { conversationId });
    }
    #endregion

    #region 🔔 26. Thông báo học viên
    [HttpGet("Notifications")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Notifications()
    {
        var userId = GetUserId()!;

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost("Notifications/MarkRead/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = GetUserId()!;
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Notifications");
    }

    [HttpPost("Notifications/MarkAllRead")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var userId = GetUserId()!;
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = "✅ Đã đánh dấu tất cả là đã đọc";
        return RedirectToAction("Notifications");
    }
    #endregion
}