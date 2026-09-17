# University Research Management System (URMS)

**URMS** is a role-based web platform for coordinating university research supervision — from proposal submission and supervisor matching, through progress tracking and review, to publication verification. Built with **ASP.NET Core MVC 8**, **C#**, and **Entity Framework Core**, it also integrates a **GitHub commit importer** for automatic progress tracking and a **Gemini-powered AI research assistant**.

---

## Table of Contents

- [Features](#features)
- [Technologies Used](#technologies-used)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Database Schema](#database-schema)
- [Future Enhancements](#future-enhancements)
- [License](#license)

---

## Features

### 1. User Roles
- **Admin:** Manages user accounts, searches and filters all proposals, and manually assigns supervisors.
- **Student:** Submits research proposals, tracks progress, uploads documents, and records publications.
- **Supervisor:** Browses open proposals, accepts students up to a configured capacity, reviews submitted work, and verifies publications.

### 2. Research Proposal Workflow
- Students **submit proposals** with a title, abstract, and keywords.
- Proposals move through **Pending → Ongoing → Completed / Rejected** status.
- Supervisors can **accept open proposals**, subject to their availability and maximum student capacity.
- Admins can **manually assign a supervisor** to any proposal, with live capacity checks.

### 3. Supervisor Management
- Supervisors maintain a profile: research areas, designation, office room, maximum student capacity, and availability toggle.
- Current supervision load is checked against capacity before any new student is accepted.

### 4. Progress Tracking & GitHub Sync
- Students post manual progress updates against a proposal.
- A linked **public GitHub repository** can be synced to automatically import commits as progress updates (deduplicated by commit hash).

### 5. Review & Feedback
- Supervisors record feedback, change a proposal's status, and optionally set a **response deadline** with a specific task.
- Every review is kept as a permanent, timestamped record.

### 6. Document Management
- Students and supervisors upload supporting documents (`.pdf`, `.doc`, `.docx`, `.pptx`, `.zip`, up to 20 MB).
- Access is restricted server-side to the proposal's own student, its supervisor, and Admin.

### 7. Publications
- Students record publications (title, authors, venue, type, year, DOI/URL) and link them to a proposal.
- Supervisors **verify** publications belonging to their own students.

### 8. Messaging
- Direct messaging between a student and their supervisor only (contacts are derived from linked proposals).
- Unread message counts per contact, with read-state tracked per message.

### 9. Notifications
- In-app notifications are generated for assignments, reviews, messages, and publication events.
- A live unread-count badge appears in the navigation bar; opening a notification marks it read and deep-links to the relevant page.

### 10. AI Research Assistant
- A chat interface (powered by the Gemini API) that answers research-writing, methodology, and system-usage questions.
- Responses are context-aware of the signed-in user's own proposals or supervision load.

---

## Technologies Used

- **Backend:** ASP.NET Core MVC 8, C#, Entity Framework Core
- **Frontend:** Razor Views, Bootstrap 5, HTML5, CSS3, jQuery
- **Database:** SQL Server / LocalDB
- **External Services:** GitHub REST API (commit import), Google Gemini API (AI assistant)
- **Security:** PBKDF2 (HMAC-SHA256) password hashing, session-based authentication, anti-forgery tokens on all state-changing requests
- **Tools:** Visual Studio 2022, Git & GitHub

---

## Project Structure

```
├── Controllers/        # Admin, Student, Supervisor, User, Document, Message, Notification, Assistant
├── Models/              # User, ResearchProposal, ProgressUpdate, Review, Publication,
│                        # ResearchDocument, SupervisorProfile, Message, Notification
├── Data/                # ApplicationDbContext (EF Core)
├── Services/            # PasswordHasher, NotificationService, GitHubService, GeminiService
├── Migrations/          # EF Core migration history
├── Views/               # Razor views, organized by controller
├── wwwroot/             # Static assets (css, js, bootstrap, uploaded files)
└── Program.cs           # App startup and middleware configuration
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server LocalDB
- A [Gemini API key](https://ai.google.dev/) (for the AI Assistant feature)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Anay-Ghosh/University-Research-Management-System.git
   cd University-Research-Management-System
   ```

2. **Configure `appsettings.json`**

   Create an `appsettings.json` file in the project root (it is not committed to source control) with your connection string and API key:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=URMSDb;Trusted_Connection=True;MultipleActiveResultSets=true"
     },
     "Gemini": {
       "ApiKey": "YOUR_GEMINI_API_KEY"
     },
     "Logging": {
       "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
     },
     "AllowedHosts": "*"
   }
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. Open `https://localhost:{port}` in your browser (the port is shown in the console output or `Properties/launchSettings.json`).

---

## Usage

1. **Register** an account and select a role (Student or Supervisor). Admin accounts are created directly in the database.
2. **Students** submit a proposal, then track its status, add progress updates, upload documents, and record publications from their dashboard.
3. **Supervisors** browse open proposals under *Browse Proposals*, accept students within their capacity, and leave feedback under *Review*.
4. **Admins** oversee the whole system from the *Dashboard*, manage accounts under *Users*, and search/assign proposals under *Proposals*.
5. Use the **AI Assistant** link (visible once logged in) for research-writing guidance, and check **Alerts** for real-time notifications.

---

## Database Schema

Core entities and their relationships (see `Data/ApplicationDbContext.cs`):

| Entity | Key Relationships |
|---|---|
| `User` | Has many `ResearchProposal`s (as Student); optional one-to-one `SupervisorProfile` |
| `ResearchProposal` | Belongs to a Student and (optionally) a Supervisor; has many `ProgressUpdate`s, `Review`s, `ResearchDocument`s |
| `Publication` | Belongs to a Student; optionally linked to a `ResearchProposal` |
| `Review` | Belongs to a `ResearchProposal` and the reviewing Supervisor |
| `Message` | Sender/Receiver `User`s; optionally linked to a `ResearchProposal` |
| `Notification` | Belongs to a `User`; cascades on user deletion |

---

## Future Enhancements

- Automated plagiarism/similarity checking for uploaded documents and publications.
- Email notifications alongside in-app alerts.
- Calendar integration for review deadlines.
- Analytics dashboard for department-wide research output.

---

## License

This project is provided for academic purposes. Add a license of your choice (e.g. MIT) if you plan to distribute or open-source it.
