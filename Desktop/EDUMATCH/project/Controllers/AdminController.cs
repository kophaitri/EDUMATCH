using EduMatch.DTOs.Admin;
using EduMatch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduMatch.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminDashboardService _dashboardService;
    private readonly IAdminUserService _adminUserService;
    private readonly IAdminContentService _adminContentService;
    private readonly IAdminComplaintService _adminComplaintService;
    private readonly IAdminTicketService _adminTicketService;
    private readonly IAdminEmailService _adminEmailService;
    private readonly IAdminAuditService _adminAuditService;

    public AdminController(
        EduMatchDbContext db,
        UserManager<ApplicationUser> userManager,
        IAdminDashboardService dashboardService,
        IAdminUserService adminUserService,
        IAdminContentService adminContentService,
        IAdminComplaintService adminComplaintService,
        IAdminTicketService adminTicketService,
        IAdminEmailService adminEmailService,
        IAdminAuditService adminAuditService)
    {
        _db = db;
        _userManager = userManager;
        _dashboardService = dashboardService;
        _adminUserService = adminUserService;
        _adminContentService = adminContentService;
        _adminComplaintService = adminComplaintService;
        _adminTicketService = adminTicketService;
        _adminEmailService = adminEmailService;
        _adminAuditService = adminAuditService;
    }

    // GET: /Admin/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var dto = await _dashboardService.GetDashboardAsync();
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải dữ liệu dashboard: " + ex.Message;
            return View(new AdminDashboardDto());
        }
    }

    // GET: /Admin/Stats?year=2025
    public async Task<IActionResult> Stats(int? year)
    {
        try
        {
            var dto = await _dashboardService.GetStatsAsync(year);
            ViewBag.SelectedYear = year ?? DateTime.UtcNow.Year;
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải thống kê: " + ex.Message;
            return View(new AdminStatsDto());
        }
    }

    // ===================== USER MANAGEMENT =====================

    // GET: /Admin/Users?search=&role=
    public async Task<IActionResult> Users(string? search, string? role)
    {
        try
        {
            var users = await _adminUserService.GetAllUsersAsync(search, role);
            ViewBag.Search = search;
            ViewBag.Role = role;
            return View(users);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách người dùng: " + ex.Message;
            return View(new List<AdminUserListDto>());
        }
    }

    // GET: /Admin/UserDetail/{userId}
    public async Task<IActionResult> UserDetail(string userId)
    {
        try
        {
            var dto = await _adminUserService.GetUserDetailAsync(userId);
            if (dto == null) return NotFound();
            return View(dto);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải thông tin người dùng: " + ex.Message;
            return RedirectToAction("Users");
        }
    }

    // POST: /Admin/ToggleUserActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserActive(string userId)
    {
        var (success, message) = await _adminUserService.ToggleUserActiveAsync(userId);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction("UserDetail", new { userId });
    }

    // GET: /Admin/PendingTutors
    public async Task<IActionResult> PendingTutors()
    {
        try
        {
            var list = await _adminUserService.GetPendingTutorsAsync();
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách gia sư chờ duyệt: " + ex.Message;
            return View(new List<PendingTutorDto>());
        }
    }

    // POST: /Admin/VerifyTutor
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTutor(string tutorUserId, bool approve, List<int> approvedCertIds, string? note)
    {
        var request = new VerifyTutorRequest
        {
            Approve = approve,
            ApprovedCertificateIds = approvedCertIds ?? new List<int>(),
            Note = note
        };

        var (success, message) = await _adminUserService.VerifyTutorAsync(tutorUserId, request);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction("PendingTutors");
    }

    // GET: /Admin/Students?search=
    public async Task<IActionResult> Students(string? search)
    {
        try
        {
            var list = await _adminUserService.GetAllStudentsAsync(search);
            ViewBag.Search = search;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách học viên: " + ex.Message;
            return View(new List<AdminStudentListDto>());
        }
    }

    // ===================== CONTENT MANAGEMENT =====================

    // --- SUBJECTS ---

    // GET: /Admin/Subjects
    public async Task<IActionResult> Subjects()
    {
        var list = await _adminContentService.GetSubjectsAsync();
        return View(list);
    }

    // POST: /Admin/CreateSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(CreateSubjectRequest request)
    {
        var (success, message) = await _adminContentService.CreateSubjectAsync(request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Subjects");
    }

    // POST: /Admin/UpdateSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSubject(int id, UpdateSubjectRequest request)
    {
        var (success, message) = await _adminContentService.UpdateSubjectAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Subjects");
    }

    // POST: /Admin/DeleteSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var (success, message) = await _adminContentService.DeleteSubjectAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Subjects");
    }

    // --- GRADE LEVELS ---

    // GET: /Admin/GradeLevels
    public async Task<IActionResult> GradeLevels()
    {
        var list = await _adminContentService.GetGradeLevelsAsync();
        return View(list);
    }

    // POST: /Admin/CreateGradeLevel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGradeLevel(CreateGradeLevelRequest request)
    {
        var (success, message) = await _adminContentService.CreateGradeLevelAsync(request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("GradeLevels");
    }

    // POST: /Admin/UpdateGradeLevel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGradeLevel(int id, UpdateGradeLevelRequest request)
    {
        var (success, message) = await _adminContentService.UpdateGradeLevelAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("GradeLevels");
    }

    // POST: /Admin/DeleteGradeLevel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGradeLevel(int id)
    {
        var (success, message) = await _adminContentService.DeleteGradeLevelAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("GradeLevels");
    }

    // --- TEACHING STYLES ---

    // GET: /Admin/TeachingStyles
    public async Task<IActionResult> TeachingStyles()
    {
        var list = await _adminContentService.GetTeachingStylesAsync();
        return View(list);
    }

    // POST: /Admin/CreateTeachingStyle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeachingStyle(CreateTeachingStyleRequest request)
    {
        var (success, message) = await _adminContentService.CreateTeachingStyleAsync(request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TeachingStyles");
    }

    // POST: /Admin/UpdateTeachingStyle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTeachingStyle(int id, UpdateTeachingStyleRequest request)
    {
        var (success, message) = await _adminContentService.UpdateTeachingStyleAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TeachingStyles");
    }

    // POST: /Admin/DeleteTeachingStyle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeachingStyle(int id)
    {
        var (success, message) = await _adminContentService.DeleteTeachingStyleAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TeachingStyles");
    }

    // --- BANNERS ---

    // GET: /Admin/Banners
    public async Task<IActionResult> Banners()
    {
        var list = await _adminContentService.GetBannersAsync();
        return View(list);
    }

    // GET: /Admin/BannerDetail/{id}
    public async Task<IActionResult> BannerDetail(int id)
    {
        var banner = await _adminContentService.GetBannerByIdAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    // POST: /Admin/CreateBanner
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBanner(CreateBannerRequest request)
    {
        var (success, message) = await _adminContentService.CreateBannerAsync(request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Banners");
    }

    // POST: /Admin/UpdateBanner
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBanner(int id, UpdateBannerRequest request)
    {
        var (success, message) = await _adminContentService.UpdateBannerAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Banners");
    }

    // POST: /Admin/DeleteBanner
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        var (success, message) = await _adminContentService.DeleteBannerAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Banners");
    }

    // POST: /Admin/ToggleBannerActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBannerActive(int id)
    {
        var (success, message) = await _adminContentService.ToggleBannerActiveAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("Banners");
    }

    // ===================== SUPPORT TICKETS =====================

    // GET: /Admin/Tickets?status=&priority=&search=
    public async Task<IActionResult> Tickets(string? status, string? priority, string? search)
    {
        try
        {
            TicketStatus? ticketStatus = Enum.TryParse<TicketStatus>(status, out var s) ? s : null;
            TicketPriority? ticketPriority = Enum.TryParse<TicketPriority>(priority, out var p) ? p : null;

            var list = await _adminTicketService.GetTicketsAsync(ticketStatus, ticketPriority, search);
            ViewBag.FilterStatus = status;
            ViewBag.FilterPriority = priority;
            ViewBag.Search = search;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách ticket: " + ex.Message;
            return View(new List<TicketListDto>());
        }
    }

    // GET: /Admin/TicketDetail/{id}
    public async Task<IActionResult> TicketDetail(int id)
    {
        var dto = await _adminTicketService.GetTicketDetailAsync(id);
        if (dto == null) return NotFound();

        // Truyền danh sách staff để phân công
        var staffList = await _adminTicketService.GetStaffMembersAsync();
        ViewBag.StaffList = staffList;

        return View(dto);
    }

    // POST: /Admin/ReplyTicket
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyTicket(int id, ReplyTicketRequest request)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _adminTicketService.ReplyToTicketAsync(id, staffId, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TicketDetail", new { id });
    }

    // POST: /Admin/AssignTicket
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTicket(int id, AssignTicketRequest request)
    {
        var (success, message) = await _adminTicketService.AssignTicketAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TicketDetail", new { id });
    }

    // POST: /Admin/UpdateTicketStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTicketStatus(int id, UpdateTicketStatusRequest request)
    {
        var (success, message) = await _adminTicketService.UpdateTicketStatusAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("TicketDetail", new { id });
    }

    // ===================== COMPLAINTS & VIOLATIONS =====================

    // --- REVIEW COMPLAINTS ---

    // GET: /Admin/ReviewComplaints?status=Pending
    public async Task<IActionResult> ReviewComplaints(string? status)
    {
        try
        {
            var list = await _adminComplaintService.GetReviewComplaintsAsync(status);
            ViewBag.FilterStatus = status;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách khiếu nại: " + ex.Message;
            return View(new List<ReviewComplaintListDto>());
        }
    }

    // GET: /Admin/ReviewComplaintDetail/{id}
    public async Task<IActionResult> ReviewComplaintDetail(int id)
    {
        var dto = await _adminComplaintService.GetReviewComplaintDetailAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    // POST: /Admin/HandleReviewComplaint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleReviewComplaint(int id, HandleReviewComplaintRequest request)
    {
        var (success, message) = await _adminComplaintService.HandleReviewComplaintAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("ReviewComplaints");
    }

    // --- REPORTS ---

    // GET: /Admin/Reports?status=Pending
    public async Task<IActionResult> Reports(string? status)
    {
        try
        {
            ReportStatus? reportStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, out var parsed))
                reportStatus = parsed;

            var list = await _adminComplaintService.GetReportsAsync(reportStatus);
            ViewBag.FilterStatus = status;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách báo cáo: " + ex.Message;
            return View(new List<ReportListDto>());
        }
    }

    // GET: /Admin/ReportDetail/{id}
    public async Task<IActionResult> ReportDetail(int id)
    {
        var dto = await _adminComplaintService.GetReportDetailAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    // POST: /Admin/HandleReport
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleReport(int id, HandleReportRequest request)
    {
        var (success, message) = await _adminComplaintService.HandleReportAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("ReportDetail", new { id });
    }

    // --- FRAUD WARNINGS ---

    // GET: /Admin/FraudWarnings?flaggedOnly=true
    public async Task<IActionResult> FraudWarnings(bool? flaggedOnly)
    {
        try
        {
            var list = await _adminComplaintService.GetFraudWarningsAsync(flaggedOnly);
            ViewBag.FlaggedOnly = flaggedOnly;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách cảnh báo gian lận: " + ex.Message;
            return View(new List<FraudWarningListDto>());
        }
    }

    // ===================== TRANSACTIONS (existing) =====================

    // GET: /Admin/Transactions
    public async Task<IActionResult> Transactions(string? type, string? status)
    {
        var query = _db.Transactions
            .Include(t => t.Wallet).ThenInclude(w => w.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var tType))
            query = query.Where(t => t.Type == tType);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, out var tStatus))
            query = query.Where(t => t.Status == tStatus);

        var transactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        ViewBag.TotalAmount = transactions.Where(t => t.Status == TransactionStatus.Completed).Sum(t => t.Amount);
        ViewBag.FilterType = type;
        ViewBag.FilterStatus = status;

        return View(transactions);
    }

    // GET: /Admin/TransactionDetail/{id}
    public async Task<IActionResult> TransactionDetail(int id)
    {
        var transaction = await _db.Transactions
            .Include(t => t.Wallet).ThenInclude(w => w.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null) return NotFound();

        return View(transaction);
    }

    // POST: /Admin/UpdateTransactionStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTransactionStatus(int id, TransactionStatus status)
    {
        var transaction = await _db.Transactions
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null) return NotFound();

        // Nếu từ chối rút tiền (Pending -> Failed) => hoàn tiền
        if (transaction.Type == TransactionType.Withdrawal &&
            transaction.Status == TransactionStatus.Pending &&
            status == TransactionStatus.Failed)
        {
            transaction.Wallet.Balance += transaction.Amount;
        }

        transaction.Status = status;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
        return RedirectToAction("TransactionDetail", new { id });
    }

    // GET: /Admin/Payments
    public async Task<IActionResult> Payments()
    {
        var payments = await _db.PaymentOrders
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(payments);
    }

    // GET: /Admin/Tutors
    public async Task<IActionResult> Tutors()
    {
        var tutors = await _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.TutorSubjects).ThenInclude(ts => ts.Subject)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(tutors);
    }

    // POST: /Admin/ToggleVerify
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVerify(string id)
    {
        var tutor = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == id);
        if (tutor == null) return NotFound();

        tutor.IsVerified = !tutor.IsVerified;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = tutor.IsVerified
            ? "Đã xác minh gia sư thành công!"
            : "Đã huỷ xác minh gia sư.";

        return RedirectToAction("Tutors");
    }

    // ===================== EMAIL & NOTIFICATIONS =====================

    // --- EMAIL TEMPLATES ---

    // GET: /Admin/EmailTemplates
    public async Task<IActionResult> EmailTemplates()
    {
        try
        {
            var list = await _adminEmailService.GetEmailTemplatesAsync();
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải danh sách email templates: " + ex.Message;
            return View(new List<EmailTemplateListDto>());
        }
    }

    // GET: /Admin/EmailTemplateDetail/{id}
    public async Task<IActionResult> EmailTemplateDetail(int id)
    {
        var dto = await _adminEmailService.GetEmailTemplateByIdAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    // POST: /Admin/CreateEmailTemplate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmailTemplate(CreateEmailTemplateRequest request)
    {
        var (success, message) = await _adminEmailService.CreateEmailTemplateAsync(request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailTemplates");
    }

    // POST: /Admin/UpdateEmailTemplate/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmailTemplate(int id, UpdateEmailTemplateRequest request)
    {
        var (success, message) = await _adminEmailService.UpdateEmailTemplateAsync(id, request);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailTemplateDetail", new { id });
    }

    // POST: /Admin/DeleteEmailTemplate/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmailTemplate(int id)
    {
        var (success, message) = await _adminEmailService.DeleteEmailTemplateAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailTemplates");
    }

    // POST: /Admin/ToggleEmailTemplateActive/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmailTemplateActive(int id)
    {
        var (success, message) = await _adminEmailService.ToggleEmailTemplateActiveAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailTemplates");
    }

    // --- EMAIL QUEUE ---

    // GET: /Admin/EmailQueue
    public async Task<IActionResult> EmailQueue()
    {
        try
        {
            var list = await _adminEmailService.GetEmailQueueAsync();
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải hàng đợi email: " + ex.Message;
            return View(new List<EmailQueueListDto>());
        }
    }

    // POST: /Admin/DeleteEmailQueueItem/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmailQueueItem(int id)
    {
        var (success, message) = await _adminEmailService.DeleteEmailQueueItemAsync(id);
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailQueue");
    }

    // POST: /Admin/ClearEmailQueue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearEmailQueue()
    {
        var (success, message) = await _adminEmailService.ClearEmailQueueAsync();
        if (success) TempData["SuccessMessage"] = message;
        else TempData["ErrorMessage"] = message;
        return RedirectToAction("EmailQueue");
    }

    // --- EMAIL LOG ---

    // GET: /Admin/EmailLogs?success=&search=&page=1
    public async Task<IActionResult> EmailLogs(bool? success, string? search, int page = 1)
    {
        try
        {
            var list = await _adminEmailService.GetEmailLogsAsync(success, search, page);
            var total = await _adminEmailService.GetEmailLogCountAsync();
            ViewBag.FilterSuccess = success;
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalCount = total;
            return View(list);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải email logs: " + ex.Message;
            return View(new List<EmailLogListDto>());
        }
    }

    // --- BROADCAST NOTIFICATION ---

    // GET: /Admin/BroadcastNotification
    public IActionResult BroadcastNotification()
    {
        return View();
    }

    // POST: /Admin/BroadcastNotification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BroadcastNotification(BroadcastNotificationRequest request)
    {
        var (success, message, sentCount) = await _adminEmailService.BroadcastNotificationAsync(request);
        if (success)
        {
            TempData["SuccessMessage"] = message;
            ViewBag.SentCount = sentCount;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }
        return View(request);
    }

    // ===================== AUDIT LOGS =====================

    // GET: /Admin/AuditLogs?action=&entityType=&userId=&page=1
    public async Task<IActionResult> AuditLogs(string? action, string? entityType, string? userId, int page = 1)
    {
        try
        {
            var (items, total) = await _adminAuditService.GetAuditLogsAsync(action, entityType, userId, page);
            ViewBag.FilterAction = action;
            ViewBag.FilterEntityType = entityType;
            ViewBag.FilterUserId = userId;
            ViewBag.Page = page;
            ViewBag.TotalCount = total;
            ViewBag.PageSize = 50;
            return View(items);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải audit logs: " + ex.Message;
            return View(new List<AuditLogListDto>());
        }
    }

    // GET: /Admin/AuditLogDetail/{id}
    public async Task<IActionResult> AuditLogDetail(int id)
    {
        var dto = await _adminAuditService.GetAuditLogDetailAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    // GET: /Admin/Wallets
    public async Task<IActionResult> Wallets(string? search)
    {
        var query = _db.Wallets
            .Include(w => w.User).ThenInclude(u => u.TutorProfile)
            .Include(w => w.User).ThenInclude(u => u.StudentProfile)
            .Include(w => w.Transactions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(w => w.User.FullName.Contains(search) || w.User.Email!.Contains(search));

        var wallets = await query.OrderByDescending(w => w.Balance).ToListAsync();

        ViewBag.TotalBalance = wallets.Sum(w => w.Balance);
        ViewBag.Search = search;

        return View(wallets);
    }

    // GET: /Admin/WithdrawalRequests
    public async Task<IActionResult> WithdrawalRequests()
    {
        var requests = await _db.WithdrawalRequests
            .Include(w => w.Tutor)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    // POST: /Admin/ApproveWithdrawal
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveWithdrawal(int id, string? adminNote)
    {
        var request = await _db.WithdrawalRequests.Include(w => w.Tutor).FirstOrDefaultAsync(w => w.Id == id);
        if (request == null) return NotFound();
        if (request.Status != WithdrawalStatus.Pending) return BadRequest();

        request.Status = WithdrawalStatus.Approved;
        request.AdminNote = adminNote;
        request.ProcessedAt = DateTime.UtcNow;

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.TutorId);
        if (wallet != null)
        {
            var transaction = await _db.Transactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.Type == TransactionType.Withdrawal &&
                            t.Status == TransactionStatus.Pending &&
                            t.Amount == request.Amount)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (transaction != null)
                transaction.Status = TransactionStatus.Completed;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã duyệt rút tiền {request.Amount:N0} VNĐ cho {request.Tutor?.FullName}.";
        return RedirectToAction("WithdrawalRequests");
    }

    // POST: /Admin/RejectWithdrawal
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectWithdrawal(int id, string? adminNote)
    {
        var request = await _db.WithdrawalRequests.Include(w => w.Tutor).FirstOrDefaultAsync(w => w.Id == id);
        if (request == null) return NotFound();
        if (request.Status != WithdrawalStatus.Pending) return BadRequest();

        request.Status = WithdrawalStatus.Rejected;
        request.AdminNote = adminNote;
        request.ProcessedAt = DateTime.UtcNow;

        // Hoàn tiền lại vào ví tutor
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.TutorId);
        if (wallet != null)
        {
            wallet.Balance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = await _db.Transactions
                .Where(t => t.WalletId == wallet.Id &&
                            t.Type == TransactionType.Withdrawal &&
                            t.Status == TransactionStatus.Pending &&
                            t.Amount == request.Amount)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (transaction != null)
                transaction.Status = TransactionStatus.Cancelled;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã từ chối và hoàn tiền vào ví gia sư.";
        return RedirectToAction("WithdrawalRequests");
    }

    // ===================== ENTRY EXAMS =====================

    // GET: /Admin/EntryExams
    public async Task<IActionResult> EntryExams()
    {
        var exams = await _db.Exams
            .Include(e => e.Subject)
            .Include(e => e.Questions)
            .Where(e => e.IsEntryExam)
            .OrderBy(e => e.Subject!.Name)
            .ToListAsync();

        var subjects = await _db.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.Subjects = subjects;
        return View(exams);
    }

    // POST: /Admin/EntryExams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEntryExam(IFormFile examFile, int subjectId, string title,
        string? description, int durationMinutes, int passingScore)
    {
        if (examFile == null || examFile.Length == 0 || subjectId == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn môn học và file đề thi.";
            return RedirectToAction(nameof(EntryExams));
        }

        var parsed = Services.DocxExamParser.ParseFromFile(examFile);
        if (!parsed.Success)
        {
            TempData["ErrorMessage"] = "Lỗi đọc file: " + string.Join(", ", parsed.Errors);
            return RedirectToAction(nameof(EntryExams));
        }

        var adminId = _userManager.GetUserId(User)!;
        var exam = new Exam
        {
            TutorId         = adminId,
            SubjectId       = subjectId,
            IsEntryExam     = true,
            Title           = !string.IsNullOrWhiteSpace(title) ? title : parsed.Title,
            Description     = !string.IsNullOrWhiteSpace(description) ? description : parsed.Description,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : parsed.DurationMinutes,
            PassingScore    = passingScore > 0 ? passingScore : parsed.PassingScore,
            MaxRetakes      = 99,
            IsOpen          = true,
            Status          = ExamStatus.Published,
            PublishedAt     = DateTime.UtcNow,
            CreatedAt       = DateTime.UtcNow
        };

        // Save docx file
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exams");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}.docx";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await examFile.CopyToAsync(stream);
        exam.ExamFileUrl = $"/uploads/exams/{fileName}";

        foreach (var q in parsed.Questions)
        {
            var question = new ExamQuestion
            {
                QuestionText = q.QuestionText,
                PassageText  = q.PassageText,
                PartNumber   = q.PartNumber,
                Points       = q.Points,
                DisplayOrder = q.Number,
                QuestionType = q.QuestionType,
                TopicTag     = q.TopicTag
            };

            bool isEssay = q.QuestionType == "Essay" || q.QuestionType == "ShortAnswer";
            if (isEssay)
            {
                if (!string.IsNullOrWhiteSpace(q.ModelAnswer))
                    question.AnswerOptions.Add(new ExamAnswerOption { OptionText = q.ModelAnswer, IsCorrect = true, DisplayOrder = 1 });
            }
            else
            {
                foreach (var opt in q.Options.OrderBy(o => o.Label))
                    question.AnswerOptions.Add(new ExamAnswerOption
                    {
                        OptionText   = opt.Text,
                        IsCorrect    = opt.Label == q.CorrectKey,
                        DisplayOrder = opt.Label switch { "A" => 1, "B" => 2, "C" => 3, _ => 4 }
                    });
            }
            exam.Questions.Add(question);
        }

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã tạo bài kiểm tra đầu vào \"{exam.Title}\" thành công.";
        return RedirectToAction(nameof(EntryExams));
    }

    // POST: /Admin/EntryExams/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEntryExam(int id)
    {
        var exam = await _db.Exams
            .Include(e => e.Questions).ThenInclude(q => q.AnswerOptions)
            .Include(e => e.Submissions)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsEntryExam);

        if (exam == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bài kiểm tra.";
            return RedirectToAction(nameof(EntryExams));
        }

        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa bài kiểm tra đầu vào.";
        return RedirectToAction(nameof(EntryExams));
    }
}
