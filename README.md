# 🏢 Leave Management System API

A production-grade RESTful API built with **.NET 9** and **Clean Architecture** to streamline corporate employee leave requests, role-based workflow approvals, organization onboarding, public holiday tracking, and HR provisioning.

---

## 🌐 Live Demos & Endpoints

* 🎨 **Frontend Application (Vercel):** [https://new-leave-management-system-qszg.vercel.app/](https://new-leave-management-system-qszg.vercel.app/)
* ⚙️ **Backend Swagger API (Render):** [https://leavemanagementsystem-3sja.onrender.com/swagger](https://leavemanagementsystem-3sja.onrender.com/swagger)
* 🩺 **Health Check Endpoint:** [https://leavemanagementsystem-3sja.onrender.com/health](https://leavemanagementsystem-3sja.onrender.com/health)
* 🔔 **SignalR Notification Hub:** `wss://leavemanagementsystem-3sja.onrender.com/hubs/notifications`

---

## 👥 Role Hierarchy & Access Rules

1. **Employee (`1`)**
   * Submit, update, or cancel personal leave requests with designated department handover colleagues.
   * View personal request history, real-time alerts, and remaining leave balances.
2. **Team Lead (`2`)**
   * All Employee permissions.
   * Review, approve, or reject pending leave requests for team members with real-time push notifications.
3. **HR (`3`)**
   * All Team Lead permissions.
   * Onboard organizations, upload company logos, manage company public holidays, provision employee accounts, and dispatch automated setup emails.
   * Access company-wide total leave request overviews (`/api/LeaveRequests/all`).

---

## ⚡ Key System Capabilities

* 🔄 **Automated Annual Leave Reset Engine:** Background worker execution (`AnnualLeaveResetBackgroundService`) that automatically recalculates and resets annual leave balances across organizations based on configurable `DefaultAnnualLeaveDays`, complete with audit tracking.
* 🔔 **Real-Time SignalR Push Notifications:** Instant WebSockets event dispatches notifying employees, managers, and handover peers on submission, approval, rejection, and coverage assignments.
* 📅 **Smart Business Day Engine & Public Holidays:** Dynamic working-day calculation engine that excludes weekends and organization-specific public holidays (`/api/PublicHolidays`) when deducting leave balances.
* 🤝 **Colleague Handover Workflow:** Enforces department-level coverage by allowing employees to select verified department peers as handover contacts during leave submission.
* 🩺 **Automated Health Monitoring:** Built-in EF Core liveness probing (`/health`) that continuously checks application state and PostgreSQL connectivity to guarantee zero-downtime deployments on Render.
* 🔒 **Request Idempotency:** Safe replay handling for mutating HTTP calls (`POST`, `PUT`, `DELETE`) via the `X-Idempotency-Key` request header to prevent duplicate submissions on network retries or rapid UI double-clicks.
* 📜 **Automated Audit Trail:** System-wide Entity Framework Core `SaveChangesAsync` interceptor that records JSON diffs (`Old` vs `New` values) across insertions, updates, and deletions into an `AuditLogs` dataset.
* 🛑 **Global Exception Handling & Tracing:** Unified API exception middleware that standardizes 4xx/5xx payloads with ASP.NET Core `TraceId` identifiers and handles `504 Gateway Timeout` cancellations cleanly.
* 🛡️ **Rate Limiting & Security:** Partitioned fixed-window rate limiter (20 requests per 10s per IP) and thread-safe isolated background dispatches for emails and real-time alerts.

---

## 🏗️ System Architecture & Layer Structure

The solution follows strict **Clean Architecture** principles to maintain decoupling, testability, and separation of concerns.

```text
               ┌────────────────────────────────────────┐
               │          LeaveManagement.Api           │
               │   (Controllers, Middlewares, Hubs)    │
               └───────────────────┬────────────────────┘
                                   │
                                   ▼
               ┌────────────────────────────────────────┐
               │       LeaveManagement.Application      │
               │    (Services, DTOs, Interfaces)        │
               └───────────┬────────────────┬───────────┘
                           │                │
                           ▼                ▼
┌────────────────────────────────────┐    ┌────────────────────────────────────┐
│   LeaveManagement.Infrastructure   │    │      LeaveManagement.Domain        │
│ (EF Core, PostgreSQL, Background)  │───►│  (Entities, Enums, Domain Events)  │
└────────────────────────────────────┘    └────────────────────────────────────┘
```

### 📂 Directory & Project Layout

```text
LeaveManagementSystem/
├── LeaveManagement.Api/                  # Presentation Layer
│   ├── Controllers/                      # REST API endpoints
│   ├── Extensions/                       # Service registration & DI setup
│   ├── Hubs/                             # SignalR WebSockets hubs
│   ├── Middlewares/                      # Exception & Idempotency handlers
│   └── Program.cs                        # Middleware pipeline configuration
│
├── LeaveManagement.Application/          # Application Business Logic Layer
│   ├── Common/                           # Result wrappers & helper utilities
│   ├── DTOs/                             # Data transfer objects & payload models
│   ├── Interfaces/                       # Contract definitions for services/repos
│   └── Services/                         # Core domain logic & handlers
│
├── LeaveManagement.Infrastructure/       # External Infrastructure & Data Access
│   ├── BackgroundServices/               # Scheduled workers (Annual Reset Engine)
│   ├── Data/                             # EF Core AppDbContext & Audit Interceptors
│   ├── Migrations/                       # Entity Framework Core database snapshots
│   └── Services/                         # Third-party integrations (Brevo, Cloudinary)
│
└── LeaveManagement.Domain/               # Core Domain Layer
    ├── Common/                           # Base entities & auditing metadata
    ├── Entities/                         # Domain models (User, LeaveRequest, etc.)
    └── Enums/                            # Domain enumerations (LeaveStatus, Role, etc.)
```

---

## 🛠️ Tech Stack

* **Framework:** .NET 9 Web API
* **Architecture:** Clean Architecture
* **Real-Time Messaging:** ASP.NET Core SignalR (WebSockets)
* **Database & ORM:** PostgreSQL with Entity Framework Core
* **Diagnostics & Health:** `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
* **Validation:** FluentValidation pipeline filters
* **Caching & Resilience:** `.NET MemoryCache` for Idempotency evaluation
* **Authentication:** JWT (JSON Web Tokens) & BCrypt Password Hashing
* **Media Storage:** Cloudinary SDK
* **Email Service:** Thread-Safe Brevo HTTP API Integration
* **Deployment:** Render (Backend) & Vercel (Frontend)

---

## 🔑 Environment Configuration

Configure the following key-value pairs in `appsettings.Development.json` or your hosting environment variables:

| Setting Category | Key Variable | Description |
| :--- | :--- | :--- |
| **Database** | `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| **Authentication** | `JwtSettings:Secret` | Key used for JWT signing |
| **Authentication** | `JwtSettings:Issuer` | JWT Issuer domain |
| **Authentication** | `JwtSettings:Audience` | JWT Audience domain |
| **Media Storage** | `Cloudinary:CloudName` | Cloudinary cloud identifier |
| **Media Storage** | `Cloudinary:ApiKey` | Cloudinary API Key |
| **Media Storage** | `Cloudinary:ApiSecret` | Cloudinary API Secret |
| **Email Delivery** | `Brevo:ApiKey` | Brevo REST API Key |

---

## 🚀 Getting Started Locally

```bash
# Clone the repository
git clone [https://github.com/Dhorllar98/LeaveManagementSystem.git](https://github.com/Dhorllar98/LeaveManagementSystem.git)

# Navigate to project directory
cd LeaveManagementSystem

# Restore dependencies & build solution
dotnet restore
dotnet build

# Apply database migrations
dotnet ef database update --project LeaveManagement.Infrastructure --startup-project LeaveManagement.Api

# Run the API project
dotnet run --project LeaveManagement.Api
```
