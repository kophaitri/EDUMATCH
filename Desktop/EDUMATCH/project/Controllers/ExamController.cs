using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduMatch.Controllers.Tutor
{
    [Authorize(Roles = "Tutor")]
    [Route("tutor/exams")]
    public class ExamController : Controller
    {
        private readonly EduMatchDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ExamController> _logger;

        public ExamController(
            EduMatchDbContext context, 
            IWebHostEnvironment environment,
            ILogger<ExamController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        #region 📋 1. Danh sách bài thi
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var exams = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Subject)
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Tutor)
                .Include(e => e.Questions)
                .Include(e => e.Submissions)
                .Where(e => e.Session.Contract.TutorId == tutorId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ExamListViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    SessionName = $"{e.Session.Contract.Subject.Name} - {e.Session.Contract.Tutor.FullName}",
                    ScheduledAt = e.Session.ScheduledAt,
                    DurationMinutes = e.DurationMinutes,
                    PassingScore = e.PassingScore,
                    Status = e.Status,
                    PublishedAt = e.PublishedAt,
                    QuestionCount = e.Questions.Count,
                    SubmissionCount = e.Submissions.Count
                })
                .ToListAsync();
            
            return View(exams);
        }
        #endregion

        #region ➕ 2. Tạo bài kiểm tra mới
        [HttpGet("create")] 
        public async Task<IActionResult> Create()
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await GetTutorSessionsAsync(tutorId);
            
            var sessionList = sessions.Select(s => new 
                { 
                    Id = s.Id, 
                    DisplayName = $"{s.Contract?.Subject?.Name ?? "Unknown"} | {s.ScheduledAt:dd/MM HH:mm}" 
                }).ToList();

                ViewBag.SessionId = new SelectList(sessionList, "Id", "DisplayName");
            
            return View(new Exam());
        }

        [HttpPost("create")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam exam, IFormFile examWordFile)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Validate session belongs to tutor
            var session = await _context.Sessions
                .Include(s => s.Contract)
                .FirstOrDefaultAsync(s => s.Id == exam.SessionId);
                
            if (session == null || session.Contract.TutorId != tutorId)
            {
                ModelState.AddModelError("", "Session không thuộc quyền quản lý của bạn");
                var sessions = await GetTutorSessionsAsync(tutorId);

                var sessionList = sessions.Select(s => new 
                    { 
                        Id = s.Id, 
                        DisplayName = $"{s.Contract?.Subject?.Name ?? "Unknown"} | {s.ScheduledAt:dd/MM HH:mm}" 
                    }).ToList();

                    ViewBag.SessionId = new SelectList(sessionList, "Id", "DisplayName");
                return View(exam);
            }

            // 📄 Xử lý upload file Word đề thi
            if (examWordFile != null)
            {
                var allowedExtensions = new[] { ".doc", ".docx" };
                var ext = Path.GetExtension(examWordFile.FileName).ToLower();
                
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("examWordFile", "Chỉ chấp nhận file Word (.doc, .docx)");
                }
                else if (examWordFile.Length > 50 * 1024 * 1024) // 50MB
                {
                    ModelState.AddModelError("examWordFile", "File không vượt quá 50MB");
                }
                else
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "exams", tutorId);
                    Directory.CreateDirectory(uploadPath);
                    
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await examWordFile.CopyToAsync(stream);
                    }
                    
                    exam.Description = $"/uploads/exams/{tutorId}/{fileName}";
                    await ScanAndExtractExamContentAsync(filePath, exam);
                }
            }

            if (!ModelState.IsValid)
            {
                var sessions = await GetTutorSessionsAsync(tutorId);
                var sessionList = sessions.Select(s => new 
                    { 
                        Id = s.Id, 
                        DisplayName = $"{s.Contract?.Subject?.Name ?? "Unknown"} | {s.ScheduledAt:dd/MM HH:mm}" 
                    }).ToList();

                    ViewBag.SessionId = new SelectList(sessionList, "Id", "DisplayName");
                return View(exam);
            }

            exam.CreatedAt = DateTime.UtcNow;
            exam.Status = ExamStatus.Draft;
            
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "✅ Tạo bài thi thành công! Bạn có thể thêm câu hỏi ở bước tiếp theo.";
            return RedirectToAction(nameof(Edit), new { id = exam.Id });
        }
        

        // 🔍 Placeholder: Hàm quét và trích xuất nội dung từ file Word
        private async Task ScanAndExtractExamContentAsync(string filePath, Exam exam)
        {
            // TODO: Tích hợp thư viện như DocX, OpenXML để đọc file Word
            // và tự động tạo câu hỏi từ nội dung file
            
            _logger.LogInformation("Scanning exam file: {FilePath} for exam {ExamId}", filePath, exam.Id);
            await Task.CompletedTask;
        }

        private async Task<List<Session>> GetTutorSessionsAsync(string tutorId)
        {
            return await _context.Sessions
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Subject)
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Tutor)
                .Where(s => s.Contract.TutorId == tutorId)
                .OrderByDescending(s => s.ScheduledAt)
                .ToListAsync();
        }
        #endregion

        #region ✏️ 3. Chỉnh sửa bài thi + upload đề
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Subject)
                .Include(e => e.Questions.OrderBy(q => q.DisplayOrder))
                    .ThenInclude(q => q.AnswerOptions.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            var sessions = await GetTutorSessionsAsync(tutorId);
            var sessionList = sessions.Select(s => new 
                { 
                    Id = s.Id, 
                    DisplayName = $"{s.Contract?.Subject?.Name ?? "Unknown"} | {s.ScheduledAt:dd/MM HH:mm}" 
                }).ToList();

                ViewBag.SessionId = new SelectList(sessionList, "Id", "DisplayName");
            
            ViewBag.QuestionTypes = new SelectList(
                new[] { "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay" }, 
                "Value", "Value");

            return View(exam);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam, IFormFile examWordFile)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var existingExam = await _context.Exams
                .Include(e => e.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingExam == null || existingExam.Session.Contract.TutorId != tutorId)
                return NotFound();

            existingExam.Title = exam.Title;
            existingExam.DurationMinutes = exam.DurationMinutes;
            existingExam.PassingScore = exam.PassingScore;
            existingExam.MaxRetakes = exam.MaxRetakes;

            // Upload file Word mới nếu có
            if (examWordFile != null)
            {
                var ext = Path.GetExtension(examWordFile.FileName).ToLower();
                if (new[] { ".doc", ".docx" }.Contains(ext) && examWordFile.Length <= 50 * 1024 * 1024)
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "exams", tutorId);
                    Directory.CreateDirectory(uploadPath);
                    
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await examWordFile.CopyToAsync(stream);
                    }
                    
                    existingExam.Description = $"/uploads/exams/{tutorId}/{fileName}";
                    await ScanAndExtractExamContentAsync(filePath, existingExam);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "💾 Cập nhật thông tin bài thi thành công";
            
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ➕ Thêm câu hỏi mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int examId, ExamQuestion question, List<IFormFile> questionImages)
        {
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == examId);
            
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return BadRequest(new { error = "Không tìm thấy bài thi" });

            // Xử lý upload ảnh câu hỏi (nếu có)
            if (questionImages?.Any() == true)
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "questions");
                Directory.CreateDirectory(uploadPath);
                
                var imageUrls = new List<string>();
                foreach (var image in questionImages)
                {
                    if (image.Length > 10 * 1024 * 1024) continue;
                    
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    imageUrls.Add($"/uploads/questions/{fileName}");
                }
                
                if (imageUrls.Any())
                {
                    question.QuestionText += $"\n![image]({string.Join(", ", imageUrls)})";
                }
            }

            question.ExamId = examId;
            question.DisplayOrder = await _context.ExamQuestions
                .Where(q => q.ExamId == examId)
                .CountAsync() + 1;
            
            if (question.QuestionType == "MultipleChoice" && !question.AnswerOptions.Any())
            {
                for (int i = 0; i < 4; i++)
                {
                    question.AnswerOptions.Add(new ExamAnswerOption
                    {
                        OptionText = $"Đáp án {(char)('A' + i)}",
                        DisplayOrder = i + 1,
                        IsCorrect = i == 0
                    });
                }
            }

            _context.ExamQuestions.Add(question);
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true, questionId = question.Id });
        }

        // ✏️ Cập nhật câu hỏi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuestion(int questionId, ExamQuestion updatedQuestion)
        {
            var question = await _context.ExamQuestions
                .Include(q => q.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (question == null || question.Exam.Session.Contract.TutorId != tutorId)
                return BadRequest(new { error = "Không có quyền sửa câu hỏi này" });

            question.QuestionText = updatedQuestion.QuestionText;
            question.QuestionType = updatedQuestion.QuestionType;
            question.Points = updatedQuestion.Points;
            question.DisplayOrder = updatedQuestion.DisplayOrder;

            foreach (var updatedOption in updatedQuestion.AnswerOptions)
            {
                var existingOption = question.AnswerOptions.FirstOrDefault(o => o.Id == updatedOption.Id);
                if (existingOption != null)
                {
                    existingOption.OptionText = updatedOption.OptionText;
                    existingOption.IsCorrect = updatedOption.IsCorrect;
                    existingOption.DisplayOrder = updatedOption.DisplayOrder;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // 🗑️ Xóa câu hỏi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var question = await _context.ExamQuestions
                .Include(q => q.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (question == null || question.Exam.Session.Contract.TutorId != tutorId)
                return BadRequest(new { error = "Không có quyền xóa" });

            _context.ExamQuestions.Remove(question);
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true });
        }

        // 🔄 Sắp xếp lại thứ tự câu hỏi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderQuestions(int examId, List<int> questionIds)
        {
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == examId);
            
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return BadRequest(new { error = "Invalid exam" });

            for (int i = 0; i < questionIds.Count; i++)
            {
                var question = await _context.ExamQuestions.FindAsync(questionIds[i]);
                if (question != null && question.ExamId == examId)
                {
                    question.DisplayOrder = i + 1;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        #endregion

        #region 🚀 4. Mở / Đóng bài thi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            if (exam.Status == ExamStatus.Draft)
            {
                if (!exam.Questions.Any())
                    return BadRequest(new { error = "Bài thi chưa có câu hỏi" });
                
                if (exam.Questions.Any(q => !q.AnswerOptions.Any(o => o.IsCorrect)))
                    return BadRequest(new { error = "Tất cả câu hỏi phải có ít nhất 1 đáp án đúng" });

                exam.Status = ExamStatus.Published;
                exam.PublishedAt = DateTime.UtcNow;
                TempData["Success"] = "🎉 Bài thi đã được công khai!";
            }
            else if (exam.Status == ExamStatus.Published)
            {
                exam.Status = ExamStatus.Closed;
                TempData["Info"] = "⏸️ Bài thi đã tạm dừng";
            }
            else if (exam.Status == ExamStatus.Closed)
            {
                exam.Status = ExamStatus.Published;
                TempData["Success"] = "▶️ Bài thi đã được mở lại";
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = exam.Status.ToString(), publishedAt = exam.PublishedAt });
        }
        #endregion

        #region 📊 5. Danh sách bài làm học viên
        public async Task<IActionResult> Submissions(int id)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            var submissions = await _context.ExamSubmissions
                .Include(s => s.Student)
                .Where(s => s.ExamId == id)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new SubmissionListViewModel
                {
                    Id = s.Id,
                    StudentName = s.Student.FullName,
                    StudentEmail = s.Student.Email,
                    StartedAt = s.StartedAt,
                    SubmittedAt = s.SubmittedAt,
                    TotalScore = s.TotalScore,
                    Percentage = s.Percentage,
                    IsPassed = s.IsPassed,
                    Status = s.Status,
                    IsFlagged = s.IsFlagged,
                    FraudScore = s.FraudScore,
                    RetakeNumber = s.RetakeNumber
                })
                .ToListAsync();

            ViewBag.ExamTitle = exam.Title;
            ViewBag.ExamId = exam.Id;
            return View(submissions);
        }
        #endregion

        #region 👁️ 6. Xem bài làm chi tiết
        public async Task<IActionResult> SubmissionDetail(int examId, int subId)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            var submission = await _context.ExamSubmissions
                .Include(s => s.Student)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.SelectedOption)
                .Include(s => s.FraudWarnings)
                .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);

            if (submission == null)
                return NotFound();

            ViewBag.MaxScore = exam.Questions.Sum(q => q.Points);
            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int examId, int subId, decimal totalScore, string tutorComment)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var submission = await _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);

            if (submission == null || submission.Exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            var maxScore = submission.Exam.Questions.Sum(q => q.Points);
            submission.TotalScore = Math.Min(totalScore, maxScore);
            submission.Percentage = maxScore > 0 ? (submission.TotalScore / maxScore) * 100 : 0;
            submission.IsPassed = submission.Percentage >= submission.Exam.PassingScore;
            submission.Status = SubmissionStatus.Graded;
            submission.SubmittedAt = submission.SubmittedAt ?? DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "📝 Chấm điểm thành công!";
            
            return RedirectToAction(nameof(SubmissionDetail), new { examId, subId });
        }
        #endregion

        #region ⚠️ 7. Xem cảnh báo gian lận
        public async Task<IActionResult> FraudWarnings(int examId, int subId)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var submission = await _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .Include(s => s.FraudWarnings)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);

            if (submission == null || submission.Exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveFraudWarning(int warningId, string resolutionNote)
        {
            var warning = await _context.FraudWarnings
                .Include(w => w.Submission)
                    .ThenInclude(s => s.Exam)
                        .ThenInclude(e => e.Session)
                            .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(w => w.Id == warningId);

            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (warning == null || warning.Submission.Exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            warning.Details += $"\n[Resolved by tutor {tutorId} at {DateTime.UtcNow}]: {resolutionNote}";
            
            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Đã xử lý cảnh báo gian lận";
            
            return RedirectToAction(nameof(FraudWarnings), new { 
                examId = warning.Submission.ExamId, 
                submissionId = warning.SubmissionId 
            });
        }
        #endregion

        #region 🔄 8. Yêu cầu làm lại bài
        public async Task<IActionResult> RetakeRequests(int examId, int subId)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var submission = await _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .Include(s => s.RetakeRequests)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == subId && s.ExamId == examId);

            if (submission == null || submission.Exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondRetakeRequest(int requestId, bool isApproved, string responseNote)
        {
            var request = await _context.RetakeRequests.FindAsync(requestId);
            if (request == null)
                return NotFound();

            var submission = await _context.ExamSubmissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Session)
                        .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId);

            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (submission == null || submission.Exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            request.IsApproved = isApproved;
            request.ResponseNote = responseNote;
            request.RespondedAt = DateTime.UtcNow;

            if (isApproved)
            {
                var newSubmission = new ExamSubmission
                {
                    ExamId = submission.ExamId,
                    StudentId = submission.StudentId,
                    RetakeNumber = submission.RetakeNumber + 1,
                    Status = SubmissionStatus.InProgress,
                    StartedAt = DateTime.UtcNow
                };
                _context.ExamSubmissions.Add(newSubmission);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = isApproved 
                ? "✅ Đã duyệt yêu cầu làm lại." 
                : "❌ Đã từ chối yêu cầu làm lại.";
            
            return RedirectToAction(nameof(RetakeRequests), new { 
                examId = submission.ExamId, 
                submissionId = submission.Id 
            });
        }
        #endregion

        #region 🗑️ Xóa bài thi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tutorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exam = await _context.Exams
                .Include(e => e.Session)
                    .ThenInclude(s => s.Contract)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null || exam.Session.Contract.TutorId != tutorId)
                return NotFound();

            if (exam.Status != ExamStatus.Draft)
            {
                TempData["Error"] = "⚠️ Chỉ có thể xóa bài thi ở trạng thái Draft";
                return RedirectToAction(nameof(Edit), new { id });
            }

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "🗑️ Đã xóa bài thi";
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }

    #region 📦 ViewModels
    public class ExamListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SessionName { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public int PassingScore { get; set; }
        public ExamStatus Status { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int QuestionCount { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class SubmissionListViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public SubmissionStatus Status { get; set; }
        public bool IsFlagged { get; set; }
        public decimal FraudScore { get; set; }
        public int RetakeNumber { get; set; }
    }
    #endregion
}