# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EduMatch is an ASP.NET Core MVC web application (.NET 10) that connects students with tutors. It uses SQL Server via Entity Framework Core, ASP.NET Core Identity for authentication, and cookie-based sessions for the web UI. JWT infrastructure is configured but not yet used for web routes (intended for future API endpoints).

## Common Commands

All commands run from the `project/` directory:

```bash
cd project/

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Build
dotnet build

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations list
dotnet ef migrations remove
dotnet ef database drop --force
```

## Database Setup

Requires SQL Server running on `localhost:1433`. Use Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name edumatch-sql \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

Update the connection string in `project/appsettings.json` under `ConnectionStrings:DefaultConnection` to match your SQL Server credentials.

On first run, `DatabaseSeeder` auto-seeds: 3 roles (Admin, Tutor, Student), an admin account, 9 subjects, 12 grade levels, and 5 teaching styles.

## Architecture

### Solution Structure
```
EDUMATCH/
├── project/              # Single ASP.NET Core MVC project
│   ├── Controllers/      # MVC controllers
│   ├── Models/
│   │   ├── Entities/     # EF Core entity classes (grouped by domain)
│   │   ├── Enums/        # All enums in one file
│   │   ├── ApplicationUser.cs   # extends IdentityUser
│   │   └── ApplicationRole.cs   # extends IdentityRole
│   ├── Data/
│   │   ├── EduMatchDbContext.cs  # extends IdentityDbContext
│   │   └── DatabaseSeeder.cs
│   ├── ViewModels/       # View-specific models
│   ├── DTOs/             # (empty, for future API DTOs)
│   ├── Services/         # (empty, for future service layer)
│   ├── Views/            # Razor views (.cshtml)
│   ├── GlobalUsings.cs   # Global using statements for EduMatch namespaces
│   └── Program.cs        # App configuration and startup
```

### Domain Model (Entity Groups)

- **ProfileEntities.cs** — `TutorProfile`, `StudentProfile`, `TutorSubject`, `TutorCertificate`, `TutorAvailability`, `TutorMediaFile`, `TutorTeachingStyle`, `Subject`, `GradeLevel`, `TeachingStyle`, `RefreshToken`
- **BookingEntities.cs** — `BookingRequest`, `Contract`, `Session`
- **ExamEntities.cs** — `Exam`, `ExamQuestion`, `ExamAnswerOption`, `ExamSubmission`, `SubmissionAnswer`, `FraudWarning`, `RetakeRequest`
- **ReviewWalletEntities.cs** — `Review`, `ReviewReply`, `ReviewComplaint`, `Wallet`, `Transaction`, `PaymentOrder`, `TutorRevenueStat`, `ReputationLog`
- **CommunicationEntities.cs** — `Notification`, `Conversation`, `ConversationParticipant`, `Message`, `SupportTicket`, `Report`, `AuditLog`
- **EmailEntities.cs** — `EmailTemplate`, `EmailQueue`, `EmailLog`, `UserEmailPreference`

### Authorization

Three roles: `Admin`, `Tutor`, `Student`. Policies defined in `Program.cs`:
- `AdminOnly`, `TutorOnly`, `StudentOnly`, `TutorOrStudent`

Use `[Authorize(Policy = "...")]` on controllers/actions.

### Global Usings

`GlobalUsings.cs` imports `EduMatch.Models`, `EduMatch.Models.Enums`, `EduMatch.Data`, and `Microsoft.EntityFrameworkCore` project-wide — no need to add these using statements in individual files.

### CSS Convention

All custom CSS must go in `project/wwwroot/css/edumatch.css`. When creating or editing `.cshtml` or `.html` files, **never write inline `<style>` blocks** — instead, add new CSS classes/rules to `edumatch.css` and reference them via class names in the markup.

### Key Patterns

- Controllers inject `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, and `EduMatchDbContext` directly (no service layer yet)
- Cookie-based auth for web UI; login path is `/Account/Login`
- Avatar uploads stored in `wwwroot/uploads/avatars/`
- `TempData["SuccessMessage"]` used for flash messages across redirects
- Composite keys and unique indexes configured via Fluent API in `EduMatchDbContext.OnModelCreating`
- `DeleteBehavior.Restrict` used on most FK relationships to prevent cascade issues; `Cascade` only for direct user-owned data (profile, wallet, tokens)
