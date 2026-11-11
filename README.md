# School Admission System

A production-grade, concurrent, first-come first-served School Admission System built with **C# / ASP.NET Core (.NET 10)**, **PostgreSQL**, **React 18+ (TypeScript + Vite)**, and **Docker Compose**.

Developed for the **Onnorokom Projukti Limited** Take-Home Assignment.

---

## Architecture & Technology Stack

### Backend
- **Framework:** ASP.NET Core Web API (.NET 10)
- **Database & ORM:** PostgreSQL 16 + Entity Framework Core (Npgsql)
- **Architecture:** Clean Layered Architecture (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`)
- **Concurrency Control:** Atomic transactions with PostgreSQL row-level pessimistic locking (`SELECT ... FOR UPDATE`)
- **Port:** `http://localhost:8080`

### Frontend
- **Framework:** React 18+ with TypeScript
- **Tooling:** Vite + Tailwind CSS + Lucide Icons
- **HTTP Client:** Axios with structured error mapping and `X-Student-Id` injection
- **Port:** `http://localhost:3000` (via Docker Nginx) / `http://localhost:5173` (via Vite Dev Server)

### Infrastructure & Deployment
- **Containerization:** Multi-stage `backend/Dockerfile` and `frontend/Dockerfile` (Nginx Alpine)
- **Orchestration:** Root `docker-compose.yml` with persistent PostgreSQL volume and dependency health checks

---

## Core Business Invariants Enforced

1. **Strict First-Come, First-Served Capacity Invariant:**
   $$\text{heldSeats} + \text{confirmedSeats} \le \text{totalSeats}$$
   Regardless of how many concurrent applicants apply simultaneously, overbooking is strictly impossible.
2. **Instant Seat Return on Expiration:**
   Holds expire automatically after `HOLD_DURATION_SECONDS` (default: 600s). The moment a hold expires, its seat is released immediately for other students without needing any background sweeper or manual API trigger.
3. **Idempotent Confirmation:**
   Simulating payment multiple times for an already confirmed reservation never increments `confirmedSeats` more than once.
4. **Duplicate Prevention:**
   A student cannot hold multiple active reservations or confirmed admissions for the same class concurrently.
---

## Quick Start with Docker (Recommended)

To run the entire system with Docker:

```bash
docker compose up --build
```

No software installation is required on the evaluator's machine apart from Docker.

### Access Endpoints
- **Frontend UI:** [http://localhost:3000](http://localhost:3000)
- **Backend API:** [http://localhost:8080](http://localhost:8080)
- **PostgreSQL Database:** `localhost:5432` (Credentials: `postgres` / `postgres`)

### Stopping the Application
```bash
docker compose down
```
> Note: Application data persists across restarts via the Docker volume `postgres_data`.

---

## Running Locally for Development

### 1. Prerequisites
- [.NET 10 SDK]
- [Node.js 20+]
- PostgreSQL running locally on port 5432

### 2. Backend Setup
```bash
cd backend
dotnet restore
dotnet run --project SchoolAdmission.Api
```
The backend API will start at `http://localhost:8080`. EF Core automatically applies any pending database migrations on startup.

### 3. Frontend Setup
```bash
cd frontend
npm install
npm run dev
```
The frontend dev server will start at `http://localhost:5173`.

---

## Running Automated Tests

The test suite contains 10 comprehensive automated integration tests, including the mandatory concurrency stress test:

```bash
cd backend
dotnet test
```

### Key Tests Included
1. **Successful Seat Hold (`Test1_Can_Hold_Available_Seat_Successfully`):** Validates seat reservation and availability decrement.
2. **Expiration Behavior (`Test2_Expired_Hold_Frees_Seat_And_Cannot_Be_Confirmed`):** Advances time past 600s, verifies the seat is instantly available to another student, and verifies the expired hold cannot be confirmed (HTTP 409).
3. **Concurrency Stress Test (`Test3_Concurrency_Test_Parallel_Requests_Never_Overbook`):** Sends 35 simultaneous parallel requests for a class with only 5 seats. Verifies exactly 5 succeed with HTTP 201 and 30 receive HTTP 409 Conflict.
4. **Idempotency Test:** Confirms a hold multiple times, verifying that `confirmedSeats` is never double-counted.
5. **Cancellation Test:** Cancels an active hold and verifies the seat is released back to the class pool.
6. **Ownership Validation:** Prevents a student from confirming or cancelling another student's reservation.
7. **Duplicate Active Hold Prevention:** Prevents the same student from holding multiple seats in the same class simultaneously.
8. **17-Digit Birth Certificate Validation:** Verifies invalid student IDs return HTTP 400 Bad Request, never 500.

---

## API Specification Reference

| Method | Path | Required Headers | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/classes` | — | Create a new class (`name`, `totalSeats`) |
| `GET` | `/classes` | — | List all classes with seat statistics |
| `GET` | `/classes/{classId}` | — | Get class details with seat statistics |
| `POST` | `/classes/{classId}/holds` | `X-Student-Id` | Reserve a seat (First-Come, First-Served) |
| `GET` | `/holds/{holdId}` | — | Get reservation status (`HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`) |
| `POST` | `/holds/{holdId}/confirm` | `X-Student-Id` | Confirm admission (simulated payment, idempotent) |
| `DELETE` | `/holds/{holdId}` | `X-Student-Id` | Cancel active reservation |
| `GET` | `/me/holds` | `X-Student-Id` | List all applications for the student |
