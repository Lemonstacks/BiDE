# Lethabo Mathole - Use Cases & Implementation

This document covers the seven use cases allocated to Lethabo Mathole, how each is implemented in the BiDE platform, and the improvements added to each one.

Legend: **S** = Student, **I** = Instructor, **A** = Admin

| Use Case Id | Use Case Name |
|-------------|---------------|
| UC06_S | Make Booking Request |
| UC15_I | Verify Payment |
| UC08_S | Upload Proof of Payment |
| UC10_S | Leave Review |
| UC07_S | View Booking Status |
| UC13_I | View Booking Requests |
| UC18_A | View Payments |

---

## UC06_S - Make Booking Request

**Actor:** Student
**Goal:** Book a lesson with an approved instructor for a chosen offering and time slot.

### Flow
1. Student browses instructors and opens an instructor's profile.
2. Student clicks "Book Now" on an offering, picks an available time slot.
3. System creates a booking with status `Pending` and notifies the instructor.

### Implementation
- Controller: `InstructorsController.Book` (standard) and `InstructorsController.BookRealTime` (from the live map).
- View: `Views/Instructors/Detail.cshtml` booking modal.

### Validations
- Student must be logged in.
- Offering and schedule must both belong to the selected instructor.
- Slot must still be `Available`.
- Instructor must be `Approved` and verified.
- No duplicate booking for the same slot.
- Only future slots are shown for booking.

### Improvements Added
- **Price snapshot (`AgreedPrice`)**: the offering price is locked onto the booking at creation time. If the instructor later changes the offering price, the student is still charged the price they agreed to.

---

## UC08_S - Upload Proof of Payment

**Actor:** Student
**Goal:** Submit payment for an accepted booking.

### Flow
1. Once a booking is `Accepted`, the student clicks "Upload Proof of Payment".
2. Student selects a payment method (EFT, Cash, or Card).
3. For EFT/Card, student uploads a screenshot or PDF. For EFT, they also enter a payment reference.
4. Payment record is created with status `Pending` (or `Verified` immediately for Cash).

### Implementation
- Controller: `StudentController.SubmitPayment`.
- View: `Views/Student/Bookings.cshtml` payment modal.

### Validations
- Payment method must be one of EFT, Cash, Card.
- Cash requires no file upload (verified in person).
- EFT and Card require a proof file (max 5MB, only jpg/png/pdf).
- EFT requires a payment reference so the deposit can be matched.

### Improvements Added
- **Payment reference field** for EFT so instructors can match the bank deposit.
- **Cash exemption**: no upload required for cash payments.
- **Resubmission**: if a payment was rejected, resubmitting clears the old rejection reason.

---

## UC07_S - View Booking Status

**Actor:** Student
**Goal:** See the current state of each booking.

### Flow
1. Student opens "My Bookings".
2. Active and past bookings are listed with status badges and a visual progress timeline.

### Implementation
- Controller: `StudentController.Bookings`.
- View: `Views/Student/Bookings.cshtml`.

### Improvements Added
- **Status timeline**: each active booking shows a visual tracker: Requested -> Accepted -> Paid -> Completed, with completed steps highlighted.
- **Payment status badges**: Paid, Payment Pending, Payment Rejected.
- **Rejection reason surfaced**: when a payment is rejected, the student sees the instructor's reason and a prompt to resubmit.

---

## UC10_S - Leave Review

**Actor:** Student
**Goal:** Rate and comment on a completed lesson.

### Flow
1. On a completed lesson, the student opens the review form.
2. Student picks a 1-5 rating and an optional comment.
3. Review is saved and shown on the instructor's profile.

### Implementation
- Controller: `StudentController.LeaveReview`.
- View: `Views/Student/CompletedLessons.cshtml`.

### Validations
- Rating must be between 1 and 5 (enforced by a dropdown and server-side).
- Only completed bookings can be reviewed.
- One review per booking.

---

## UC13_I - View Booking Requests

**Actor:** Instructor
**Goal:** Review incoming booking requests and accept or reject them.

### Flow
1. Instructor opens the dashboard.
2. Pending requests are listed with student, lesson, and schedule details.
3. Instructor accepts or rejects (with a reason) each request.

### Implementation
- Controller: `InstructorDashboardController.Index`, `AcceptBooking`, `RejectBooking`.
- View: `Views/InstructorDashboard/Index.cshtml`.

### Improvements Added
- **Sorting** of pending requests by: Newest request, Oldest request, Lesson date, or Student name.

---

## UC15_I - Verify Payment

**Actor:** Instructor
**Goal:** Confirm or reject a student's submitted payment.

### Flow
1. Instructor opens the Payments page.
2. Pending payments show amount, method, reference, and proof.
3. Instructor verifies or rejects (with a reason).

### Implementation
- Controller: `InstructorDashboardController.Payments`, `VerifyPayment`, `RejectPayment`.
- View: `Views/InstructorDashboard/Payments.cshtml`.

### Improvements Added
- **Proof thumbnail preview**: image proofs render inline instead of forcing a new tab.
- **Payment reference displayed** so the instructor can match the bank deposit.
- **Rejection reason** captured via a modal and shown to the student.
- **Verify guard**: payment cannot be verified for a cancelled or rejected booking.
- **Completion gate**: a lesson can only be marked complete once payment is verified.

---

## UC18_A - View Payments

**Actor:** Admin
**Goal:** Oversee all payments across the platform.

### Flow
1. Admin opens the Payments page.
2. Summary stats and a filterable list of all payments are shown.

### Implementation
- Controller: `AdminController.ViewPayments`, `ExportPayments`.
- View: `Views/Admin/ViewPayments.cshtml`.

### Improvements Added
- **Summary stats**: total payments, awaiting verification, verified revenue, and pending revenue.
- **Date range filter** (from/to) in addition to status and search.
- **CSV export** of the filtered payment list for accounting.
- **Reference and rejection reason** shown on each payment.

---

## Data Model Changes

| Model | Field | Type | Purpose |
|-------|-------|------|---------|
| Booking | AgreedPrice | decimal(18,2) | Price locked at booking time (UC06) |
| Payment | PaymentReference | string(100) | EFT reference for matching deposits (UC08/UC15) |
| Payment | RejectionReason | string(500) | Why a payment was rejected (UC15/UC07) |

Migrations:
- `AddPaymentRejectionReason`
- `AddPaymentReferenceAndAgreedPrice`

---

## Payment Lifecycle (with these improvements)

```
Student books           -> Booking: Pending (AgreedPrice locked)
Instructor accepts      -> Booking: Accepted
Student submits payment -> Payment: Pending (+ reference for EFT)
                           (Cash -> auto Verified)
Instructor verifies     -> Payment: Verified
   or rejects           -> Payment: Rejected (+ reason shown to student)
                           Student resubmits -> back to Pending
Instructor completes    -> Booking: Completed (only if payment Verified)
Student reviews         -> Review saved
```
