
namespace EduMatch.Models;

public class Review
{
     public int Id { get; set; }
    public int ContractId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public string RevieweeId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ← ← ← BẮT BUỘC PHẢI CÓ 2 DÒNG NÀY:
    public bool IsApproved { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }

    public Contract Contract { get; set; } = null!;
    public ApplicationUser Reviewer { get; set; } = null!;
    public ApplicationUser Reviewee { get; set; } = null!;
    public ReviewReply? Reply { get; set; }
    public ICollection<ReviewComplaint> Complaints { get; set; } = new List<ReviewComplaint>();
}

public class ReviewReply
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string ReplyText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RepliedAt { get; set; }

    public Review Review { get; set; } = null!;
}

public class ReviewComplaint
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string ComplainantId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ReporterId { get; set; } = string.Empty;
    
    // ← ← ← BẮT BUỘC PHẢI CÓ 2 DÒNG NÀY:
    public string? Details { get; set; }
    public string? AdminResponse { get; set; }

    public Review Review { get; set; } = null!;
    public ApplicationUser Complainant { get; set; } = null!;
}

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal TotalEarned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; } = null!;
}

public class PaymentOrder
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? PaymentGatewayOrderId { get; set; }
    public string? PaymentGatewayResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

public class TutorRevenueStat
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public int TotalStudents { get; set; }

    public ApplicationUser Tutor { get; set; } = null!;
}

public class ReputationLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public decimal PointsChange { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
