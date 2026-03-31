using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

[Authorize(Policy = "TutorOnly")]
[Route("[controller]")]
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

    // ← ← ← METHOD HELPER LẤY USER ID
    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

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

    // ============================================================
    // Post Management
    // ============================================================

    // GET: /Tutor/Posts
    [HttpGet]
    public async Task<IActionResult> Posts()
    {
        var tutorId = GetUserId()!;
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

        if (!post.IsPublished)
        {
            var currentUserId = GetUserId();
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

        var tutorId = GetUserId()!;
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
        var tutorId = GetUserId()!;
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

        var tutorId = GetUserId()!;
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
        var tutorId = GetUserId()!;
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
        var userId = GetUserId()!;
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
        var userId = GetUserId()!;
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

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

        await UpdateHourlyRateRange(profile);

        TempData["SuccessMessage"] = "Thêm môn dạy thành công!";
        return RedirectToAction("ManageSubjects");
    }

    // POST: /Tutor/RemoveSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubject(int id)
    {
        var userId = GetUserId()!;
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
        var userId = GetUserId()!;
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
        var userId = GetUserId()!;

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
        var userId = GetUserId()!;

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
        var userId = GetUserId()!;

        var booking = await _db.BookingRequests
            .Include(b => b.Subject)
            .FirstOrDefaultAsync(b => b.Id == id && b.TutorId == userId && b.Status == BookingStatus.Pending);

        if (booking == null) return NotFound();

        booking.Status = BookingStatus.Accepted;
        booking.RespondedAt = DateTime.UtcNow;

        var tutorSubject = await _db.TutorSubjects
            .FirstOrDefaultAsync(ts => ts.TutorId == userId && ts.SubjectId == booking.SubjectId && ts.GradeLevelId == booking.GradeLevelId);

        var hourlyRate = tutorSubject?.HourlyRate ?? 0;

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
        var userId = GetUserId()!;

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
        var userId = GetUserId()!;

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
        var userId = GetUserId()!;

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
        var userId = GetUserId()!;

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
    // Exam Management
    // ============================================================

    // GET: /Tutor/Exams
    public async Task<IActionResult> Exams()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var exams = await _db.Exams
            .Include(e => e.Questions)
            .Include(e => e.Submissions)
            .Where(e => e.TutorId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var vm = exams.Select(e => new ExamListItemViewModel
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            DurationMinutes = e.DurationMinutes,
            PassingScore = e.PassingScore,
            Status = e.Status,
            IsOpen = e.IsOpen,
            OpenAt = e.OpenAt,
            CloseAt = e.CloseAt,
            ExamFileUrl = e.ExamFileUrl,
            CreatedAt = e.CreatedAt,
            TotalQuestions = e.Questions.Count,
            TotalSubmissions = e.Submissions.Count
        }).ToList();

        return View(vm);
    }

    // GET: /Tutor/CreateExam
    [HttpGet]
    public IActionResult CreateExam() => View(new CreateExamViewModel());

    // POST: /Tutor/CreateExam
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExam(CreateExamViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Nếu upload file .docx thì parse trực tiếp, bỏ qua form questions
        if (model.ExamFile != null && model.ExamFile.Length > 0
            && Path.GetExtension(model.ExamFile.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateExamFromDocx(model, userId);
        }

        if (!ModelState.IsValid) return View(model);

        var exam = new Exam
        {
            TutorId = userId,
            Title = model.Title,
            Description = model.Description,
            DurationMinutes = model.DurationMinutes,
            PassingScore = model.PassingScore,
            MaxRetakes = model.MaxRetakes,
            Status = ExamStatus.Draft
        };

        if (model.Questions != null)
        {
            for (int i = 0; i < model.Questions.Count; i++)
            {
                var q = model.Questions[i];
                if (string.IsNullOrWhiteSpace(q.QuestionText)) continue;

                var question = new ExamQuestion
                {
                    QuestionText = q.QuestionText,
                    Points = q.Points > 0 ? q.Points : 1,
                    DisplayOrder = i + 1,
                    QuestionType = "MultipleChoice"
                };

                for (int j = 0; j < q.Options.Count; j++)
                {
                    var opt = q.Options[j];
                    if (string.IsNullOrWhiteSpace(opt.OptionText)) continue;
                    question.AnswerOptions.Add(new ExamAnswerOption
                    {
                        OptionText = opt.OptionText,
                        IsCorrect = (j == q.CorrectOptionIndex),
                        DisplayOrder = j + 1
                    });
                }

                exam.Questions.Add(question);
            }
        }

        if (model.ExamFile != null && model.ExamFile.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ExamFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ExamFile.CopyToAsync(stream);
            exam.ExamFileUrl = $"/uploads/exams/{fileName}";
        }

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tạo bài kiểm tra thành công!";
        return RedirectToAction(nameof(Exams));
    }

    private async Task<IActionResult> CreateExamFromDocx(CreateExamViewModel model, string userId)
    {
        var parsed = DocxExamParser.ParseFromFile(model.ExamFile!);

        if (!parsed.Success)
        {
            foreach (var err in parsed.Errors)
                ModelState.AddModelError(string.Empty, err);
            return View("CreateExam", model);
        }

        // Form values override file tags if user filled them in
        var exam = new Exam
        {
            TutorId = userId,
            Title = !string.IsNullOrWhiteSpace(model.Title) ? model.Title : parsed.Title,
            Description = !string.IsNullOrWhiteSpace(model.Description) ? model.Description : parsed.Description,
            DurationMinutes = model.DurationMinutes > 0 ? model.DurationMinutes : parsed.DurationMinutes,
            PassingScore = model.PassingScore > 0 ? model.PassingScore : parsed.PassingScore,
            MaxRetakes = model.MaxRetakes >= 0 ? model.MaxRetakes : parsed.MaxRetakes,
            Status = ExamStatus.Draft
        };

        // Lưu file docx
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}.docx";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await model.ExamFile!.CopyToAsync(stream);
        exam.ExamFileUrl = $"/uploads/exams/{fileName}";

        // Thêm câu hỏi từ file
        foreach (var q in parsed.Questions)
        {
            var question = new ExamQuestion
            {
                QuestionText = q.QuestionText,
                PassageText = q.PassageText,
                PartNumber = q.PartNumber,
                Points = q.Points,
                DisplayOrder = q.Number,
                QuestionType = "MultipleChoice"
            };

            foreach (var opt in q.Options.OrderBy(o => o.Label))
            {
                question.AnswerOptions.Add(new ExamAnswerOption
                {
                    OptionText = opt.Text,
                    IsCorrect = opt.Label == q.CorrectKey,
                    DisplayOrder = opt.Label switch { "A" => 1, "B" => 2, "C" => 3, _ => 4 }
                });
            }

            exam.Questions.Add(question);
        }

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        if (parsed.Warnings.Any())
            TempData["WarningMessage"] = string.Join("; ", parsed.Warnings);

        TempData["SuccessMessage"] = $"Import thành công từ file docx! Đã tạo {parsed.Questions.Count} câu hỏi.";
        return RedirectToAction(nameof(EditExam), new { id = exam.Id });
    }

    // GET: /Tutor/EditExam/5
    [HttpGet]
    public async Task<IActionResult> EditExam(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams
            .Include(e => e.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.Id == id && e.TutorId == userId);

        if (exam == null) return NotFound();

        var vm = new EditExamViewModel
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            DurationMinutes = exam.DurationMinutes,
            PassingScore = exam.PassingScore,
            MaxRetakes = exam.MaxRetakes,
            ExistingFileUrl = exam.ExamFileUrl
        };

        ViewBag.ExistingQuestions = exam.Questions.OrderBy(q => q.DisplayOrder).ToList();
        return View(vm);
    }

    // POST: /Tutor/EditExam/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExam(EditExamViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams
            .Include(e => e.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.Id == model.Id && e.TutorId == userId);

        if (exam == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.ExistingQuestions = exam.Questions.OrderBy(q => q.DisplayOrder).ToList();
            return View(model);
        }

        exam.Title = model.Title;
        exam.Description = model.Description;
        exam.DurationMinutes = model.DurationMinutes;
        exam.PassingScore = model.PassingScore;
        exam.MaxRetakes = model.MaxRetakes;

        if (model.ExamFile != null && model.ExamFile.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ExamFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ExamFile.CopyToAsync(stream);
            exam.ExamFileUrl = $"/uploads/exams/{fileName}";
        }

        if (model.NewQuestions != null)
        {
            int nextOrder = exam.Questions.Count + 1;
            foreach (var q in model.NewQuestions)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText)) continue;

                var question = new ExamQuestion
                {
                    ExamId = exam.Id,
                    QuestionText = q.QuestionText,
                    Points = q.Points > 0 ? q.Points : 1,
                    DisplayOrder = nextOrder++,
                    QuestionType = "MultipleChoice"
                };

                for (int j = 0; j < q.Options.Count; j++)
                {
                    var opt = q.Options[j];
                    if (string.IsNullOrWhiteSpace(opt.OptionText)) continue;
                    question.AnswerOptions.Add(new ExamAnswerOption
                    {
                        OptionText = opt.OptionText,
                        IsCorrect = (j == q.CorrectOptionIndex),
                        DisplayOrder = j + 1
                    });
                }

                _db.ExamQuestions.Add(question);
            }
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật bài thi thành công!";
        return RedirectToAction(nameof(EditExam), new { id = model.Id });
    }

    // POST: /Tutor/UpdateExamQuestion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExamQuestion(UpdateExamQuestionViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == model.ExamId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var question = await _db.ExamQuestions
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == model.QuestionId && q.ExamId == model.ExamId);
        if (question == null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(EditExam), new { id = model.ExamId });
        }

        question.QuestionText = model.QuestionText;
        question.PassageText = string.IsNullOrWhiteSpace(model.PassageText) ? null : model.PassageText;
        question.Points = model.Points;

        // Map đáp án theo thứ tự A=1, B=2, C=3, D=4
        var optionMap = new Dictionary<string, (int order, string text)>
        {
            ["A"] = (1, model.OptionA),
            ["B"] = (2, model.OptionB),
            ["C"] = (3, model.OptionC),
            ["D"] = (4, model.OptionD),
        };

        foreach (var opt in question.AnswerOptions)
        {
            var letter = opt.DisplayOrder switch { 1 => "A", 2 => "B", 3 => "C", _ => "D" };
            if (optionMap.TryGetValue(letter, out var data))
            {
                opt.OptionText = data.text;
                opt.IsCorrect = letter == model.CorrectOption.ToUpper();
            }
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật câu {question.DisplayOrder}.";
        return RedirectToAction(nameof(EditExam), new { id = model.ExamId });
    }

    // POST: /Tutor/DeleteExamQuestion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExamQuestion(int questionId, int examId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var question = await _db.ExamQuestions
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == examId);

        if (question == null) return NotFound();

        _db.ExamAnswerOptions.RemoveRange(question.AnswerOptions);
        _db.ExamQuestions.Remove(question);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa câu hỏi.";
        return RedirectToAction(nameof(EditExam), new { id = examId });
    }

    // POST: /Tutor/ToggleExam/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleExam(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id && e.TutorId == userId);
        if (exam == null) return NotFound();

        if (exam.IsOpen)
        {
            exam.IsOpen = false;
            exam.CloseAt = DateTime.UtcNow;
            TempData["SuccessMessage"] = "Đã đóng bài thi.";
        }
        else
        {
            exam.IsOpen = true;
            exam.OpenAt = DateTime.UtcNow;
            exam.CloseAt = null;
            exam.Status = ExamStatus.Published;
            exam.PublishedAt ??= DateTime.UtcNow;
            TempData["SuccessMessage"] = "Đã mở bài thi cho học viên.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Exams));
    }

    // GET: /Tutor/ExamSubmissions/5
    public async Task<IActionResult> ExamSubmissions(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submissions = await _db.ExamSubmissions
            .Include(s => s.Student)
            .Include(s => s.FraudWarnings)
            .Where(s => s.ExamId == id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        ViewBag.Exam = exam;
        return View(submissions);
    }

    // GET: /Tutor/SubmissionDetail/5/3
    public async Task<IActionResult> SubmissionDetail(int examId, int subId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submission = await _db.ExamSubmissions
            .Include(s => s.Student)
            .Include(s => s.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);

        if (submission == null) return NotFound();

        ViewBag.Exam = exam;
        return View(submission);
    }

    // GET: /Tutor/GradeSubmission/5/3
    [HttpGet]
    public async Task<IActionResult> GradeSubmission(int examId, int subId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submission = await _db.ExamSubmissions
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);
        if (submission == null) return NotFound();

        var maxScore = exam.Questions.Sum(q => q.Points);

        var vm = new GradeSubmissionViewModel
        {
            ExamId = examId,
            SubmissionId = subId,
            StudentName = submission.Student.FullName,
            CurrentScore = submission.TotalScore,
            MaxScore = maxScore,
            TutorComment = submission.TutorComment
        };

        ViewBag.Exam = exam;
        ViewBag.Submission = submission;
        return View(vm);
    }

    // POST: /Tutor/GradeSubmission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeSubmission(GradeSubmissionViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == model.ExamId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submission = await _db.ExamSubmissions
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == model.SubmissionId && s.ExamId == model.ExamId);
        if (submission == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.StudentName = submission.Student.FullName;
            model.CurrentScore = submission.TotalScore;
            model.MaxScore = exam.Questions.Sum(q => q.Points);
            ViewBag.Exam = exam;
            ViewBag.Submission = submission;
            return View(model);
        }

        submission.TutorComment = model.TutorComment;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;

        if (model.OverrideScore.HasValue)
        {
            var maxScore = exam.Questions.Sum(q => q.Points);
            submission.TotalScore = model.OverrideScore.Value;
            submission.Percentage = maxScore > 0 ? (model.OverrideScore.Value / maxScore) * 100 : 0;
            submission.IsPassed = submission.TotalScore >= exam.PassingScore;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã chấm điểm bài làm!";
        return RedirectToAction(nameof(ExamSubmissions), new { id = model.ExamId });
    }

    // GET: /Tutor/FraudWarning/5/3
    public async Task<IActionResult> FraudWarning(int examId, int subId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submission = await _db.ExamSubmissions
            .Include(s => s.Student)
            .Include(s => s.FraudWarnings)
            .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);
        if (submission == null) return NotFound();

        ViewBag.Exam = exam;
        return View(submission);
    }

    // GET: /Tutor/RetakeRequest/5/3
    [HttpGet]
    public async Task<IActionResult> RetakeRequest(int examId, int subId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var submission = await _db.ExamSubmissions
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);
        if (submission == null) return NotFound();

        var request = await _db.RetakeRequests
            .Where(r => r.SubmissionId == subId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();

        ViewBag.Exam = exam;
        ViewBag.Submission = submission;
        return View(request);
    }

    // POST: /Tutor/RetakeRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetakeRequest(int examId, int subId, bool isApproved, string? responseNote)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId && e.TutorId == userId);
        if (exam == null) return NotFound();

        var request = await _db.RetakeRequests
            .Where(r => r.SubmissionId == subId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();
        if (request == null) return NotFound();

        request.IsApproved = isApproved;
        request.ResponseNote = responseNote;
        request.RespondedAt = DateTime.UtcNow;

        if (isApproved)
        {
            var submission = await _db.ExamSubmissions.FindAsync(subId);
            if (submission != null)
                submission.RetakeNumber++;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = isApproved ? "Đã chấp thuận yêu cầu làm lại." : "Đã từ chối yêu cầu làm lại.";
        return RedirectToAction(nameof(ExamSubmissions), new { id = examId });
    }

    // ============================================================
    // Wallet Management
    // ============================================================

    // GET: /Tutor/Wallet
    public async Task<IActionResult> Wallet()
    {
        var userId = GetUserId()!;

        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }

    // GET: /Tutor/WalletTransactions
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

    // GET: /Tutor/Withdraw
    public async Task<IActionResult> Withdraw()
    {
        var userId = GetUserId()!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        ViewBag.Balance = wallet?.Balance ?? 0;
        return View();
    }

    // POST: /Tutor/Withdraw
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(decimal amount, string accountNumber, string bankName, string accountName)
    {
        var userId = GetUserId()!;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        if (amount <= 0 || amount > wallet.Balance)
        {
            TempData["ErrorMessage"] = "Số tiền rút không hợp lệ hoặc vượt quá số dư";
            ViewBag.Balance = wallet.Balance;
            return View();
        }

        // Giữ tiền (trừ tạm) + tạo yêu cầu rút
        var withdrawalRequest = new WithdrawalRequest
        {
            TutorId = userId,
            Amount = amount,
            BankName = bankName,
            AccountNumber = accountNumber,
            AccountName = accountName,
            Status = WithdrawalStatus.Pending
        };
        _db.WithdrawalRequests.Add(withdrawalRequest);

        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Pending,
            Description = $"Yêu cầu rút tiền về {bankName} - {accountNumber}",
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance - amount
        };
        _db.Transactions.Add(transaction);

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Yêu cầu rút {amount:N0} VNĐ đã gửi. Admin sẽ xử lý trong 1-3 ngày làm việc.";
        return RedirectToAction("Wallet");
    }

    // POST: /Tutor/CompleteSession
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSession(int sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _db.Sessions
            .Include(s => s.Contract)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Contract.TutorId == userId);

        if (session == null) return NotFound();
        if (session.Status != SessionStatus.Scheduled && session.Status != SessionStatus.InProgress)
            return BadRequest();

        session.Status = SessionStatus.PendingConfirmation;
        session.TutorCompletedAt = DateTime.UtcNow;
        session.EndedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã đánh dấu hoàn thành. Chờ học viên xác nhận (tự động sau 24h).";
        return RedirectToAction("ContractDetail", new { id = session.ContractId });
    }


    // ============================================================
    // 🎯 REVIEW & COMPLAINT FEATURES (Items 10-13)
    // ============================================================

    #region 📊 10. Tất cả đánh giá nhận được
    [HttpGet("Reviews")]
    public async Task<IActionResult> Reviews()
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Contract)
                .ThenInclude(c => c.Subject)
            .Include(r => r.Reply)
            .Where(r => r.RevieweeId == tutorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.TutorId = tutorId;
        return View(reviews);
    }
    #endregion

    #region 💬 11. Trả lời đánh giá
    [HttpPost("Reviews/{id}/Reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyToReview(int id, string replyText)
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        var review = await _db.Reviews
            .Include(r => r.Reply)
            .FirstOrDefaultAsync(r => r.Id == id && r.RevieweeId == tutorId);

        if (review == null)
        {
            TempData["Error"] = "❌ Không tìm thấy đánh giá";
            return RedirectToAction("Reviews");
        }

        if (string.IsNullOrWhiteSpace(replyText))
        {
            TempData["Error"] = "⚠️ Nội dung trả lời không được để trống";
            return RedirectToAction("Reviews");
        }

        if (review.Reply == null)
        {
            review.Reply = new ReviewReply
            {
                ReviewId = review.Id,
                ReplyText = replyText,
                CreatedAt = DateTime.UtcNow,
                RepliedAt = DateTime.UtcNow
            };
            _db.ReviewReplies.Add(review.Reply);
        }
        else
        {
            review.Reply.ReplyText = replyText;
            review.Reply.RepliedAt = DateTime.UtcNow;
            _db.ReviewReplies.Update(review.Reply);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "✅ Trả lời đánh giá thành công!";
        return RedirectToAction("Reviews");
    }
    #endregion

    #region ⚠️ 12. Khiếu nại review
    [HttpGet("Reviews/{id}/Complaint")]
    public async Task<IActionResult> CreateComplaint(int id)
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        var review = await _db.Reviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id && r.RevieweeId == tutorId);

        if (review == null)
        {
            TempData["Error"] = "❌ Không tìm thấy đánh giá";
            return RedirectToAction("Reviews");
        }

        var existingComplaint = await _db.ReviewComplaints
            .FirstOrDefaultAsync(c => c.ReviewId == id && c.ComplainantId == tutorId);

        if (existingComplaint != null)
        {
            TempData["Error"] = "⚠️ Bạn đã khiếu nại đánh giá này rồi";
            return RedirectToAction("Complaints");
        }

        ViewBag.ReviewId = id;
        ViewBag.ReviewerName = review.Reviewer.FullName;
        ViewBag.ReviewComment = review.Comment;

        return View();
    }

    [HttpPost("Reviews/{id}/Complaint")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComplaint(int id, string reason, string details)
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == id && r.RevieweeId == tutorId);

        if (review == null)
        {
            TempData["Error"] = "❌ Không tìm thấy đánh giá";
            return RedirectToAction("Reviews");
        }

        var complaint = new ReviewComplaint
        {
            ReviewId = id,
            ComplainantId = tutorId,
            Reason = reason,
            Details = details,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.ReviewComplaints.Add(complaint);
        await _db.SaveChangesAsync();

        TempData["Success"] = "✅ Gửi khiếu nại thành công! Admin sẽ xem xét.";
        return RedirectToAction("Complaints");
    }
    #endregion

    #region 📜 13. Lịch sử khiếu nại
    [HttpGet("Reviews/Complaints")]
    public async Task<IActionResult> Complaints()
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        var complaints = await _db.ReviewComplaints
            .Include(c => c.Review)
                .ThenInclude(r => r.Reviewer)
            .Include(c => c.Review)
                .ThenInclude(r => r.Contract)
            .Where(c => c.ComplainantId == tutorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(complaints);
    }
    #endregion

    // ============================================================
    // TIN NHẮN & THÔNG BÁO (Items 21-23)
    // ============================================================

    #region 💬 21. Hộp thư gia sư
    [HttpGet("Messages")]
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

        // Tất cả học viên (không cần hợp đồng)
        var contacts = (await _userManager.GetUsersInRoleAsync("Student")).ToList();

        ViewBag.CurrentUserId = userId;
        ViewBag.Contacts = contacts;
        return View(conversations);
    }

    [HttpPost("Messages/Start")]
    [ValidateAntiForgeryToken]
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

    #region 💬 22. Cuộc trò chuyện gia sư
    [HttpGet("Messages/{conversationId}")]
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

    #region 🔔 23. Thông báo gia sư
    [HttpGet("Notifications")]
    public async Task<IActionResult> Notifications()
    {
        var userId = GetUserId()!;

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        // Học sinh có hợp đồng (để gia sư gửi thông báo)
        var students = await _db.Contracts
            .Include(c => c.Student)
            .Where(c => c.TutorId == userId)
            .Select(c => c.Student)
            .Distinct()
            .ToListAsync();

        ViewBag.Students = students;
        return View(notifications);
    }

    [HttpPost("Notifications/Send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNotification(string studentId, string title, string message)
    {
        var tutorId = GetUserId()!;

        var hasContract = await _db.Contracts
            .AnyAsync(c => c.TutorId == tutorId && c.StudentId == studentId);

        if (!hasContract)
        {
            TempData["Error"] = "❌ Bạn không có hợp đồng với học viên này";
            return RedirectToAction("Notifications");
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "❌ Tiêu đề và nội dung không được để trống";
            return RedirectToAction("Notifications");
        }

        _db.Notifications.Add(new Notification
        {
            UserId = studentId,
            Type = NotificationType.System,
            Title = title.Trim(),
            Message = message.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "✅ Đã gửi thông báo cho học viên!";
        return RedirectToAction("Notifications");
    }

    [HttpPost("Notifications/MarkRead/{id}")]
    [ValidateAntiForgeryToken]
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

