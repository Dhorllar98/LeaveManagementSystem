# 🏢 Leave Management System API

A production-grade RESTful API built with **.NET 9** and **Clean Architecture** to streamline corporate employee leave requests, role-based workflow approvals, and HR provisioning.

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
   * Provision new employee accounts and dispatch automated setup emails.
   * Access company-wide total leave request overviews (`/api/LeaveRequests/all`).

---

## 🛠️ Tech Stack & Architecture

* **Framework:** .NET 9 Web API
* **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Api)
* **Database:** PostgreSQL (with Entity Framework Core)
* **Authentication:** JWT (JSON Web Tokens) & BCrypt Password Hashing
* **Email Service:** Brevo HTTP API Integration
* **Deployment:** Render (Backend) & Vercel (Frontend)

---

## 🚀 Getting Started Locally

```bash
# Clone the repository
git clone [https://github.com/Dhorllar98/LeaveManagementSystem.git](https://github.com/Dhorllar98/LeaveManagementSystem.git)

# Navigate to project directory
cd LeaveManagementSystem

# Build the solution
dotnet build

# Run the API project
dotnet run --project LeaveManagement.Api
