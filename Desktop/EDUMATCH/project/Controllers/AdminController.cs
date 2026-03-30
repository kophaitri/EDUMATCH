using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduMatch.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

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
}
