# AI Usage Report

In adherence to Section 2.7 and Section 9 of the assignment specification, this document describes the utilization of AI tools during the development of the School Admission System.

---

## 1. AI Tools Utilized
- **AI Coding Assistant:** Google Antigravity (Advanced Agentic Pair Programming Assistant by Google DeepMind)

---

## 2. Assisted Components & Responsibilities
- **Domain & Architecture Structuring:** Designing the Clean Layered Architecture (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`) and establishing relational entities (`classes`, `admission_holds`).
- **PostgreSQL & EF Core Schema:** Setting up table configurations, foreign key relationships, indexes, and partial unique index constraints.
- **Concurrency & Pessimistic Locking:** Formulating the PostgreSQL `SELECT ... FOR UPDATE` row lock strategy to enforce the `heldSeats + confirmedSeats <= totalSeats` invariant across single and multi-instance deployments.
- **Automated Integration & Concurrency Testing:** Crafting a 10-test automated suite using `WebApplicationFactory` and FluentAssertions, including parallel load tests.
- **Frontend SPA Construction:** Developing responsive React 18+ TypeScript pages (School Admin & Parent/Student portal) with real-time countdown and polling mechanisms.
- **Containerization & Docker Orchestration:** Writing multi-stage Dockerfiles for backend and frontend (Nginx) alongside `docker-compose.yml`.

---

## 3. AI Mistakes Identified & Corrections Applied

| # | Initial AI Suggestion / Issue | Why It Was Deficient | Correction Applied |
| :--- | :--- | :--- | :--- |
| **1** | Defaulting to `.NET 8` target while host environment only had `.NET 10` installed | The local host environment was provisioned with .NET SDK 10.0.401 without the legacy .NET 8 runtime packs, causing runtime launch failures. | Adjusted project configuration to target `net10.0` with `<RollForward>Major</RollForward>`. This fully aligns with the assignment specification (`.NET 8 বা নতুন` / `.NET 8 or newer`) while running natively on the machine and in Docker. |
| **2** | Controller auto-discovery omitted in `WebApplicationFactory` | When `WebApplicationFactory<Program>` ran tests from the test assembly, ASP.NET Core controller discovery by default looked in the executing assembly, returning 404 for API endpoints. | Added `.AddApplicationPart(typeof(Program).Assembly)` to `builder.Services.AddControllers()` in `Program.cs`, guaranteeing reliable endpoint discovery during integration testing. |
| **3** | Single SQLite In-Memory connection for parallel test execution | When 35 simultaneous threads hammered the test server during the concurrency stress test, sharing a single SQLite connection caused `SQLite Error 5: unable to delete/modify user-function due to active statements`. | Refactored the test database provider to use SQLite Shared Cache (`Data Source=AdmissionTestDb;Mode=Memory;Cache=Shared`) and guarded non-PostgreSQL in-memory fallback with a serialization semaphore, allowing parallel requests to cleanly yield 5 successful holds and 30 conflict rejections. |
