namespace EduMatch.Models.Enums;

public enum BookingStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled,
    Expired
}

public enum ContractStatus
{
    Active,
    Completed,
    Cancelled,
    Disputed
}

public enum SessionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}

public enum ExamStatus
{
    Draft,
    Published,
    Archived
}

public enum SubmissionStatus
{
    InProgress,
    Submitted,
    Graded,
    Flagged
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Payment,
    Refund,
    Earning,
    PlatformFee
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public enum PaymentMethod
{
    Wallet,
    BankTransfer,
    CreditCard,
    Momo,
    ZaloPay
}

public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum ReportStatus
{
    Pending,
    UnderReview,
    Resolved,
    Rejected
}

public enum NotificationType
{
    System,
    Booking,
    Session,
    Payment,
    Review,
    Message
}
