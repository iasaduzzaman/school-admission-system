# Architectural & Domain Assumptions

This document outlines key technical decisions and assumptions made in accordance with the assignment brief.

---

## 1. Identifier (ID) Format
- **Class ID:** Formatted as `c_` followed by an 8-character hexadecimal unique identifier (e.g., `c_a1b2c3d4`).
- **Hold ID:** Formatted as `h_` followed by an 8-character hexadecimal unique identifier (e.g., `h_e5f6a7b8`).
- **Reasoning:** The assignment notes state that `c_123` and `h_789` are illustrative examples and the exact format is left to the developer. Using prefixed unique alphanumeric strings ensures zero collision risks across horizontal backend instances while preserving clean, human-readable URLs.

---

## 2. Reservation Expiration Strategy
- **Instant Release:** The very millisecond a reservation's `expires_at` timestamp passes, it ceases to count towards occupied seats. The next incoming applicant can immediately reserve that seat without delay.
- **Lazy Status Transition:** When an individual hold is queried via `GET /holds/{holdId}` or `/me/holds`, its effective status dynamically returns `EXPIRED` if `status == 'HELD'` and `expiresAt <= now`.

---

## 3. Concurrency Implementation & Safety Invariant
- **Rule:** $\text{heldSeats} + \text{confirmedSeats} \le \text{totalSeats}$ must hold at all times under any degree of concurrent requests.
- **Mechanism:** PostgreSQL row-level pessimistic locking (`SELECT 1 FROM classes WHERE "Id" = @classId FOR UPDATE`) inside an atomic database transaction.
- **Why this is superior:**
  1. It serializes reservation requests *only* for that specific class; applicants to other classes are not blocked.
  2. The lock is managed entirely by the PostgreSQL database engine, ensuring concurrency safety even when **multiple backend instances** run concurrently on different ports against the same database (Bonus Requirement 1).
  3. No distributed lock manager (e.g., Redis) is required, keeping infrastructure lean and reliable.

---

## 4. Duplicate Application Policy
- A student identified by a 17-digit birth certificate number cannot have more than one active application (`HELD` and not expired) or confirmed admission (`CONFIRMED`) for the same class at any given time.
- If a student's prior hold for a class has `EXPIRED` or been `CANCELLED`, they are allowed to apply again for that class provided seats remain available.
- A student is permitted to hold or confirm seats in different classes (e.g., morning and day shifts) if they wish to apply to multiple offerings.

---

## 5. Idempotent Admission Confirmation
- Calling `POST /holds/{holdId}/confirm` multiple times for an already confirmed reservation returns `HTTP 200 OK` with `{"holdId": "...", "status": "CONFIRMED"}`.
- It will **never** increment `confirmedSeats` more than once, nor modify already allocated capacity.

---

## 6. Authentication & Roles
- In accordance with the assignment specifications, parents and students do not register or log in; identity is established purely via the `X-Student-Id` header (17-digit birth registration number).
- Admin management actions (creating classes, viewing aggregate statistics) do not require credentials for the scope of this evaluation.

---

## 7. Error Handling & HTTP Status Standards
- Client and domain errors always return standard HTTP 4xx codes:
  - `400 Bad Request`: Missing or invalid input (e.g. non-17-digit Student ID, negative seat count).
  - `403 Forbidden`: Attempting to confirm or cancel a reservation belonging to another student.
  - `404 Not Found`: Class or hold identifier does not exist.
  - `409 Conflict`: Class is fully booked, student already has an active application, or reservation has expired.
- The system returns structured JSON error bodies (`{"message": "...", "code": "..."}`) and guarantees that client validation errors never trigger an unhandled HTTP 500 error.
