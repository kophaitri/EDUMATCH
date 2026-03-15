# EDUMATCH - Database Setup Guide

## Yêu cầu hệ thống

- .NET 10 SDK
- SQL Server (LocalDB, Express, hoặc Docker)
- Entity Framework Core Tools

## Cài đặt SQL Server trên macOS

### Option 1: Sử dụng Docker (Khuyến nghị)

```bash
# Pull SQL Server image
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Chạy SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name edumatch-sql \
   -d mcr.microsoft.com/mssql/server:2022-latest

# Kiểm tra container đang chạy
docker ps
```

### Option 2: Sử dụng Azure SQL hoặc Remote SQL Server

Cập nhật connection string trong `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server.database.windows.net;Database=EduMatchDb;User Id=your-username;Password=your-password;TrustServerCertificate=True"
}
```

## Cấu trúc Database

### Identity Tables (ASP.NET Core Identity)
- AspNetUsers
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims

### EduMatch Tables

**Profile & Authentication:**
- RefreshTokens
- TutorProfiles
- StudentProfiles
- TutorSubjects
- TutorCertificates
- TutorAvailabilities
- TutorMediaFiles
- TutorTeachingStyles

**Master Data:**
- Subjects
- GradeLevels
- TeachingStyles

**Booking & Contract:**
- BookingRequests
- Contracts
- Sessions

**Exam System:**
- Exams
- ExamQuestions
- ExamAnswerOptions
- ExamSubmissions
- SubmissionAnswers
- FraudWarnings
- RetakeRequests

**Review & Payment:**
- Reviews
- ReviewReplies
- ReviewComplaints
- Wallets
- Transactions
- PaymentOrders
- TutorRevenueStats
- ReputationLogs

**Communication:**
- Notifications
- Conversations
- ConversationParticipants
- Messages
- SupportTickets
- Reports
- AuditLogs

**Email:**
- EmailTemplates
- EmailQueue
- EmailLogs
- UserEmailPreferences

## Các bước Setup Database

### 1. Cài đặt EF Core Tools (nếu chưa có)

```bash
dotnet tool install --global dotnet-ef
# hoặc update
dotnet tool update --global dotnet-ef
```

### 2. Restore packages

```bash
cd /Users/phamminhvu/EDUMATCH/Desktop/EDUMATCH/project
dotnet restore
```

### 3. Tạo Migration

```bash
dotnet ef migrations add InitialCreate
```

### 4. Apply Migration (Tạo Database)

```bash
dotnet ef database update
```

### 5. Chạy ứng dụng

```bash
dotnet run
```

## Seed Data

Khi ứng dụng chạy lần đầu, hệ thống sẽ tự động seed:

### Roles:
- Admin
- Tutor
- Student

### Admin Account:
- Email: admin@edumatch.vn
- Password: Admin@123456
- ⚠️ **QUAN TRỌNG**: Đổi password này trong production!

### Master Data:
- 9 Subjects (Toán, Lý, Hóa, Sinh, Anh, Văn, Sử, Địa, Tin)
- 12 Grade Levels (Lớp 1-12)
- 5 Teaching Styles

## Connection String Configuration

### Development (appsettings.json)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=EduMatchDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### Production (Environment Variables)
```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=EduMatchDb;User Id=prod-user;Password=prod-password;TrustServerCertificate=True"
```

## Các lệnh EF Core hữu ích

```bash
# Xem danh sách migrations
dotnet ef migrations list

# Tạo migration mới
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback về migration trước
dotnet ef database update PreviousMigrationName

# Xóa migration chưa apply
dotnet ef migrations remove

# Drop database
dotnet ef database drop

# Tạo SQL script từ migrations
dotnet ef migrations script
```

## Troubleshooting

### Lỗi: Cannot connect to SQL Server

1. Kiểm tra SQL Server đang chạy:
```bash
docker ps  # Nếu dùng Docker
```

2. Test connection:
```bash
docker exec -it edumatch-sql /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P "YourStrong@Passw0rd" -C
```

### Lỗi: Migration already exists

```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
```

### Lỗi: Database already exists

```bash
dotnet ef database drop --force
dotnet ef database update
```

## Security Notes

⚠️ **QUAN TRỌNG cho Production:**

1. Đổi JWT SecretKey trong appsettings.json
2. Đổi Admin password mặc định
3. Đặt `RequireConfirmedEmail = true` trong Identity options
4. Sử dụng Environment Variables cho sensitive data
5. Enable HTTPS
6. Cấu hình CORS đúng cách
7. Implement rate limiting
8. Hash refresh tokens trước khi lưu DB

## Next Steps

Sau khi setup database thành công:

1. Tạo AuthController với Register/Login endpoints
2. Implement JWT token generation
3. Tạo các Controllers cho Tutor, Student, Booking, etc.
4. Setup SignalR cho real-time chat
5. Implement email service
6. Setup background jobs cho email queue
