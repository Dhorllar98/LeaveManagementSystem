# 🏢 Leave Management System API

A production-grade RESTful API built with **.NET 9** and **Clean Architecture** to streamline corporate employee leave requests, role-based workflow approvals, organization onboarding, and HR provisioning.

---

## 🌐 Live Demos & Links

* 🎨 **Frontend Application (Vercel):** [https://new-leave-management-system-qszg.vercel.app/](https://new-leave-management-system-qszg.vercel.app/)
* ⚙️ **Backend Swagger API (Render):** [https://leavemanagementsystem-3sja.onrender.com/swagger](https://leavemanagementsystem-3sja.onrender.com/swagger)

---

## 👥 Role Hierarchy & Access Rules

1. **Employee (`1`)**
   * Submit, update, or cancel personal leave requests.
   * View personal request history and remaining leave balances.
2. **Team Lead (`2`)**
   * All Employee permissions.
   * Review, approve, or reject pending leave requests for team members.
3. **HR (`3`)**
   * All Team Lead permissions.
   * Onboard organizations, upload company logos, provision employee accounts, and dispatch automated setup emails.
   * Access company-wide total leave request overviews (`/api/LeaveRequests/all`).

---

## ⚡ Key System Capabilities

* 🔒 **Request Idempotency:** Safe replay handling for mutating HTTP calls (`POST`, `PUT`, `DELETE`) via the `X-Idempotency-Key` request header to prevent duplicate submissions on network retries or rapid UI double-clicks.
* 📜 **Automated Audit Trail:** System-wide Entity Framework Core `SaveChangesAsync` interceptor that records JSON diffs (`Old` vs `New` values) across insertions, updates, and deletions into an `AuditLogs` dataset.
* 🛑 **Global Exception Handling & Tracing:** Unified API exception middleware that standardizes 4xx/5xx payloads with ASP.NET Core `TraceId` identifiers and handles `504 Gateway Timeout` cancellations cleanly.
* 🛡️ **Rate Limiting & Security:** Partitioned fixed-window rate limiter (20 requests per 10s per IP) and thread-safe isolated email dispatching.

---

## 🛠️ Tech Stack & Architecture

* **Framework:** .NET 9 Web API
* **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Api)
* **Database & ORM:** PostgreSQL with Entity Framework Core
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
