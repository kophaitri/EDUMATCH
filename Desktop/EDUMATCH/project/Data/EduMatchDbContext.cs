using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduMatch.Data;

public class EduMatchDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public EduMatchDbContext(DbContextOptions<EduMatchDbContext> options) : base(options) { }

    // Auth & Profile
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TutorProfile> TutorProfiles { get; set; }
    public DbSet<StudentProfile> StudentProfiles { get; set; }
    public DbSet<TutorSubject> TutorSubjects { get; set; }
    public DbSet<TutorCertificate> TutorCertificates { get; set; }
    public DbSet<TutorAvailability> TutorAvailabilities { get; set; }
    public DbSet<TutorMediaFile> TutorMediaFiles { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<GradeLevel> GradeLevels { get; set; }
    public DbSet<TeachingStyle> TeachingStyles { get; set; }
    public DbSet<TutorTeachingStyle> TutorTeachingStyles { get; set; }

    // Booking & Contract
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Session> Sessions { get; set; }

    // Exam
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamAnswerOption> ExamAnswerOptions { get; set; }
    public DbSet<ExamSubmission> ExamSubmissions { get; set; }
    public DbSet<SubmissionAnswer> SubmissionAnswers { get; set; }
    public DbSet<FraudWarning> FraudWarnings { get; set; }
    public DbSet<RetakeRequest> RetakeRequests { get; set; }

    // Review & Wallet
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewReply> ReviewReplies { get; set; }
    public DbSet<ReviewComplaint> ReviewComplaints { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PaymentOrder> PaymentOrders { get; set; }
    public DbSet<TutorRevenueStat> TutorRevenueStats { get; set; }
    public DbSet<ReputationLog> ReputationLogs { get; set; }

    // Communication
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Email
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailQueue> EmailQueue { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<UserEmailPreference> UserEmailPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Composite Keys
        builder.Entity<TutorTeachingStyle>()
            .HasKey(x => new { x.TutorId, x.TeachingStyleId });

        builder.Entity<ConversationParticipant>()
            .HasKey(x => new { x.ConversationId, x.UserId });

        builder.Entity<UserEmailPreference>()
            .HasKey(x => new { x.UserId, x.TemplateCode });

        // Unique Constraints
        builder.Entity<TutorSubject>()
            .HasIndex(x => new { x.TutorId, x.SubjectId, x.GradeLevelId })
            .IsUnique();

        builder.Entity<Review>()
            .HasIndex(x => new { x.ContractId, x.ReviewerId })
            .IsUnique();

        builder.Entity<TutorRevenueStat>()
            .HasIndex(x => new { x.TutorId, x.Year, x.Month })
            .IsUnique();

        // Decimal Precision
        builder.Entity<TutorProfile>(e =>
        {
            e.Property(x => x.AvgRating).HasPrecision(4, 2);
            e.Property(x => x.HourlyRateMin).HasPrecision(12, 2);
            e.Property(x => x.HourlyRateMax).HasPrecision(12, 2);
            e.Property(x => x.ReputationScore).HasPrecision(5, 2);
        });

        builder.Entity<TutorSubject>()
            .Property(x => x.HourlyRate).HasPrecision(12, 2);

        builder.Entity<Contract>()
            .Property(x => x.HourlyRate).HasPrecision(12, 2);

        builder.Entity<ExamSubmission>(e =>
        {
            e.Property(x => x.FraudScore).HasPrecision(5, 2);
            e.Property(x => x.TotalScore).HasPrecision(7, 2);
            e.Property(x => x.Percentage).HasPrecision(5, 2);
        });

        builder.Entity<Wallet>(e =>
        {
            e.Property(x => x.Balance).HasPrecision(15, 2);
            e.Property(x => x.TotalEarned).HasPrecision(15, 2);
        });

        builder.Entity<Transaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(15, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(15, 2);
        });

        builder.Entity<PaymentOrder>()
            .Property(x => x.Amount).HasPrecision(15, 2);

        builder.Entity<TutorRevenueStat>()
            .Property(x => x.TotalRevenue).HasPrecision(15, 2);

        builder.Entity<ReputationLog>()
            .Property(x => x.PointsChange).HasPrecision(5, 2);

        // Check Constraints
        builder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));

        // Relationships with DeleteBehavior
        builder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TutorProfile>()
            .HasOne(x => x.User)
            .WithOne(x => x.TutorProfile)
            .HasForeignKey<TutorProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasOne(x => x.User)
            .WithOne(x => x.StudentProfile)
            .HasForeignKey<StudentProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Wallet>()
            .HasOne(x => x.User)
            .WithOne(x => x.Wallet)
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BookingRequest>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BookingRequest>()
            .HasOne(x => x.Tutor)
            .WithMany()
            .HasForeignKey(x => x.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BookingRequest>()
            .HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BookingRequest>()
            .HasOne(x => x.GradeLevel)
            .WithMany()
            .HasForeignKey(x => x.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Contract>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Contract>()
            .HasOne(x => x.Tutor)
            .WithMany()
            .HasForeignKey(x => x.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Contract>()
            .HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Contract>()
            .HasOne(x => x.GradeLevel)
            .WithMany()
            .HasForeignKey(x => x.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(x => x.Reviewee)
            .WithMany()
            .HasForeignKey(x => x.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewReply>()
            .HasOne(x => x.Review)
            .WithOne(x => x.Reply)
            .HasForeignKey<ReviewReply>(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Report>()
            .HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Report>()
            .HasOne(x => x.ReportedUser)
            .WithMany()
            .HasForeignKey(x => x.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionAnswer>()
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
