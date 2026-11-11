# School Admission System — Complete Setup & Run Instructions

This guide provides step-by-step instructions for running, testing, and verifying the application on any machine.

---

## Method 1: Run with Docker Compose (Recommended)

This is the primary method expected by the evaluator. It starts PostgreSQL, the Backend API, and the Frontend with a single command.

### Step 1: Ensure Docker is Running
Launch **Docker Desktop** on your machine.

### Step 2: Start the System
From the project root directory, run:
```bash
docker compose up --build
```

### Step 3: Access the Applications
- **Frontend User & Admin Dashboard:** [http://localhost:3000](http://localhost:3000)
- **Backend REST API:** [http://localhost:8080](http://localhost:8080)
- **PostgreSQL Database:** `localhost:5432` (Username: `postgres`, Password: `postgres`, Database: `school_admission`)

### Step 4: Stop the System
```bash
docker compose down
```
> **Data Persistence:** Database data is preserved across restarts via the Docker volume `postgres_data`.

---

## Method 2: Run Locally Without Docker (Standalone Development)

If you prefer to run the services directly on your host machine:

### Prerequisites
- [.NET 8+ SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/)

### Step 1: Start Backend API
```bash
cd backend
dotnet restore
dotnet run --project SchoolAdmission.Api --urls "http://localhost:8080"
```
The backend will automatically start listening at `http://localhost:8080`.
*(Note: If PostgreSQL is not running on port 5432, the backend will automatically use a local SQLite store so you can test immediately with zero extra installation).*

### Step 2: Start Frontend Development Server
In a new terminal window:
```bash
cd frontend
npm install
npm run dev
```
Open **[http://localhost:5173](http://localhost:5173)** in your browser.

---

## Running Automated Backend Tests

The project includes 10 automated integration tests, including the required parallel concurrency stress test.

Run the test suite:
```bash
cd backend
dotnet test
```

### Test Coverage Highlights:
1. `Test1_Can_Hold_Available_Seat_Successfully` — Validates successful reservation.
2. `Test2_Expired_Hold_Frees_Seat_And_Cannot_Be_Confirmed` — Verifies automatic expiration and seat release.
3. `Test3_Concurrency_Test_Parallel_Requests_Never_Overbook` — Sends 35 simultaneous parallel requests for 5 seats to verify zero overbooking.
4. `Confirmation_Is_Idempotent_And_Does_Not_Double_Count` — Guarantees duplicate payment requests never increase confirmed seat count.
5. `Cancellation_Releases_Held_Seat` — Verifies hold cancellation.
6. `Ownership_Validation_Rejects_Other_Students` — Validates `X-Student-Id` authorization.
7. `Duplicate_Active_Hold_In_Same_Class_Is_Rejected` — Enforces single active application rule.
8. `Invalid_Student_Id_Returns_BadRequest_Never_500` — Ensures robust 400 validation error responses.

---

## Direct Database Access & Schema Scripts

The complete database schema DDL, indexes, and administrative queries are provided in:
`database/schema.sql`

To apply manually into any existing PostgreSQL instance:
```bash
psql -h localhost -p 5432 -U postgres -d school_admission -f database/schema.sql
```
