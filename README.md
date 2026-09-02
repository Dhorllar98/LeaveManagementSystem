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

* 🔔 **Real-Time SignalR Push Notifications:** Instant WebSockets event dispatches notifying employees, managers, and handover peers on submission, approval, rejection, and coverage assignments.
* 📅 **Smart Business Day Engine & Public Holidays:** Dynamic working-day calculation engine that excludes weekends and organization-specific public holidays (`/api/PublicHolidays`) when deducting leave balances.
* 🤝 **Colleague Handover Workflow:** Enforces department-level coverage by allowing employees to select verified department peers as handover contacts during leave submission.
* 🩺 **Automated Health Monitoring:** Built-in EF Core liveness probing (`/health`) that continuously checks application state and PostgreSQL connectivity to guarantee zero-downtime deployments on Render.
* 🔒 **Request Idempotency:** Safe replay handling for mutating HTTP calls (`POST`, `PUT`, `DELETE`) via the `X-Idempotency-Key` request header to prevent duplicate submissions on network retries or rapid UI double-clicks.
* 📜 **Automated Audit Trail:** System-wide Entity Framework Core `SaveChangesAsync` interceptor that records JSON diffs (`Old` vs `New` values) across insertions, updates, and deletions into an `AuditLogs` dataset.
* 🛑 **Global Exception Handling & Tracing:** Unified API exception middleware that standardizes 4xx/5xx payloads with ASP.NET Core `TraceId` identifiers and handles `504 Gateway Timeout` cancellations cleanly.
* 🛡️ **Rate Limiting & Security:** Partitioned fixed-window rate limiter (20 requests per 10s per IP) and thread-safe isolated background dispatches for emails and real-time alerts.

---

## 🛠️ Tech Stack & Architecture

* **Framework:** .NET 9 Web API
* **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Api)
* **Real-Time Messaging:** ASP.NET Core SignalR (WebSockets)
* **Database & ORM:** PostgreSQL with Entity Framework Core
* **Diagnostics & Health:** `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
* **Validation:** FluentValidation pipeline filters
* **Caching & Resilience:** `.NET MemoryCache` for Idempotency evaluation
* **Authentication:** JWT (JSON Web Tokens) & BCrypt Password Hashing
* **Media Storage:** Cloudinary SDK (Organization logos)
* **Email Service:** Thread-Safe Brevo HTTP API Integration
* **Deployment:** Render (Backend) & Vercel (Frontend)

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
