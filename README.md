# SmartBank Digital Banking Platform

A full-featured digital banking system built with **ASP.NET Core 9**, following layered architecture (MVC -> API -> Service -> Repository -> EF Core -> SQL Server).

## Technology Stack

| Layer          | Technology                                         |
|----------------|---------------------------------------------------|
| Frontend (MVC) | ASP.NET Core 9 MVC, Bootstrap 5.3.3, Bootstrap Icons |
| Backend (API)  | ASP.NET Core 9 Web API, RESTful                   |
| Auth           | JWT Bearer tokens, BCrypt password hashing         |
| ORM            | Entity Framework Core 9 (Database-First)          |
| Database       | SQL Server (Express / LocalDB)                    |
| Testing        | xUnit, Moq, FluentAssertions                      |
| CI/CD          | GitHub Actions                                     |

## Project Structure

```
SmartBank/
  SmartBank.Models/     # Entities, DTOs, View Models
  SmartBank.Data/       # DbContext, Repositories
  SmartBank.API/        # Web API controllers, Services, JWT
  SmartBank.MVC/        # MVC frontend (views, controllers)
  SmartBank.Tests/      # xUnit tests (Auth, Transaction, Loan)
  SmartBankDB_Sprint1.sql  # Database creation script
```

## Features by Sprint

### Sprint 1 - Authentication and Security
- User registration with BCrypt hashing
- JWT login with role-based claims (Customer, Admin, Manager, Teller, Auditor)
- Cookie-based JWT storage on MVC side
- Account lockout after 5 failed attempts
- Profile management (view and edit)
- Account opening (Savings / Current)

### Sprint 2 - Customer Dashboard and Accounts MVC
- Professional sidebar layout with role-aware navigation
- Account cards with balance display
- Open new accounts with account-type selection
- Account details page

### Sprint 3 - Transactions
- Deposit funds (with validation)
- Withdraw funds (minimum balance enforcement)
- Atomic fund transfers (database transaction with rollback)
- Paginated transaction history with account filter
- Reference number generation for all transactions

### Sprint 4 - Loans, Support and Notifications
- Loan application (Personal, Home, Vehicle, Education, Business)
- Live EMI calculator with interactive sliders
- Support ticket system (create, view, track)
- In-app notification system with mark-read
- Notifications triggered on loan review, ticket resolution, account freeze

### Sprint 5 - Admin Panel and Reports
- Admin dashboard with KPI cards and 7-day transaction chart
- User management (view all, freeze/unfreeze with reason)
- Loan review panel (approve with amount/rate, reject with reason)
- Ticket resolution panel
- Reports page with low-balance account detection

### Sprint 6 - Testing and DevOps
- xUnit tests for AuthService (4 tests), TransactionService (7 tests), LoanService (4 tests)
- Moq for repository mocking
- FluentAssertions for readable test assertions
- GitHub Actions CI (build and test on push)

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- SQL Server (Express or Developer)
- Visual Studio 2022+ or VS Code with C# extension

### 1. Create the Database

Open SQL Server Management Studio and execute the `SmartBankDB_Sprint1.sql` script. This creates the `SmartOnlineBankingDb` database with all tables, views, stored procedures, and seed data (roles).

### 2. Configure Connection String

Edit `SmartBank.API/appsettings.json` and set your SQL Server instance name:

```json
"ConnectionStrings": {
  "SmartBankDB": "Server=YOUR_SERVER;Database=SmartOnlineBankingDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Run the Application

Terminal 1 - Start the API:
```bash
cd SmartBank.API
dotnet run
```
API runs on https://localhost:7201, Swagger at https://localhost:7201/swagger

Terminal 2 - Start the MVC frontend:
```bash
cd SmartBank.MVC
dotnet run
```
MVC runs on https://localhost:7100

### 4. Run Tests
```bash
dotnet test SmartBank.Tests/SmartBank.Tests.csproj --verbosity normal
```

## Architecture Flow

```
Browser -> MVC Controller -> HttpClient -> API Controller -> Service -> Repository -> EF Core -> SQL Server
                |
        SecureControllerBase
        (auto-attaches JWT cookie)
```

## API Endpoints Summary

| Method | Endpoint                        | Auth    | Description                     |
|--------|--------------------------------|---------|--------------------------------|
| POST   | /api/auth/register             | No      | Register new user              |
| POST   | /api/auth/login                | No      | Login, returns JWT             |
| GET    | /api/auth/me                   | Yes     | Current user info              |
| GET    | /api/profile                   | Yes     | Get profile                    |
| PUT    | /api/profile                   | Yes     | Update profile                 |
| POST   | /api/accounts/create           | Yes     | Open new account               |
| GET    | /api/accounts                  | Yes     | List user accounts             |
| POST   | /api/transactions/deposit      | Yes     | Deposit funds                  |
| POST   | /api/transactions/withdraw     | Yes     | Withdraw funds                 |
| POST   | /api/transactions/transfer     | Yes     | Transfer funds (atomic)        |
| GET    | /api/transactions/history      | Yes     | Transaction history            |
| POST   | /api/loans/apply               | Yes     | Apply for loan                 |
| GET    | /api/loans/my                  | Yes     | User's loans                   |
| POST   | /api/tickets/create            | Yes     | Create support ticket          |
| GET    | /api/tickets/my                | Yes     | User's tickets                 |
| GET    | /api/notifications             | Yes     | List notifications             |
| POST   | /api/notifications/read-all    | Yes     | Mark all as read               |
| GET    | /api/admin/dashboard           | Admin   | Dashboard stats                |
| GET    | /api/admin/users               | Admin   | All users                      |
| POST   | /api/admin/freeze              | Admin   | Freeze/unfreeze user           |
| POST   | /api/admin/loan/approve        | Admin   | Approve/reject loan            |
| POST   | /api/admin/ticket/resolve      | Admin   | Resolve ticket                 |
| GET    | /api/admin/reports             | Admin   | Full reports                   |

## License

This project was built as a capstone project for academic purposes.
