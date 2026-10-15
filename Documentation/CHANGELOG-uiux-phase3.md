# BiDE UI/UX Refinement — Phase 3 Change Log

Branch: `feature/mapbox-livemap`
Scope: UI/UX refinement derived from the Phase 1 audit and Phase 2 proposals
(`.agents/tasks/bide-uiux-audit-2026-10-14/`). All changes are local to the
feature branch. Nothing has been pushed or merged to `main`.

Passwords remain plain text by prior accepted decision (audit finding F-22) and
were not changed in any of this work.

Each entry lists the commit, what changed, which audit finding it addresses, and
whether it changes server behavior. Every change was verified with
`dotnet build` (0 errors) and `dotnet test` (18/18 passing).

---

## Behavior-changing / security changes (the "big" ones)

These alter server behavior or global presentation and were reviewed by a second
model before merging.

### Expire past availability slots from student booking — `974abe7`
- File: `Backend/Controllers/InstructorsController.cs`
- Change: slots whose time has passed are now excluded from the instructor
  detail listing and the real-time next-slot lookup, and `Book` rejects a
  passed slot server-side. A slot stays bookable until its end time passes.
- Behavior change: yes. A student can no longer book a slot in the past, and
  passed slots no longer render as bookable.

### Prevent instructors from editing their experience — `10ee57d`
- Files: `Backend/Controllers/ProfileController.cs`,
  `Backend/Views/Profile/InstructorProfile.cshtml`
- Change: the Years of Experience field is now read-only in the profile view,
  and `UpdateInstructor` no longer accepts or writes an `experience` value, so
  it cannot be changed by a crafted request. Experience is treated as an
  admin-managed verified credential.
- Behavior change: yes. Experience is no longer self-editable by instructors.
- Note: there is currently no admin UI to set experience, so the value is
  effectively frozen (new instructors start at 0) until such a control is added.

### Reject reused password on reset — `92e0261` (also fixed logout, see below)
- File: `Backend/Controllers/AccountController.cs`
- Change: `ResetPassword` now rejects a new password that matches the current
  one, for Student, Instructor and Admin, with the message
  "Your new password cannot be the same as your old password."
- Behavior change: yes. Reusing the old password on reset is now blocked.

### Gate instructor profile view and realtime booking on approved + verified — `d3c1377`
- File: `Backend/Controllers/InstructorsController.cs`
- AP-1 (audit F-23): `Detail(int id)` now returns `NotFound()` when the
  instructor is not `Status == Approved && IsVerified`, matching the visibility
  rule the `Index` listing already enforces. This closes an id-guessing
  information disclosure where any logged-in user could view a
  Pending/Rejected/Suspended instructor's profile by guessing the id.
- AP-2 (audit F-24): `BookRealTime`'s guard changed from `Status != Approved` to
  `Status != Approved || !IsVerified`, so the real-time one-click booking path
  now enforces exactly the same approved-and-verified rule as the regular `Book`
  action (previously a just-approved-but-unverified instructor could be booked
  via the realtime path).
- Behavior change: yes. This changes who can view a profile and who can be
  booked via the realtime path. Anti-forgery, ownership checks and slot/past-slot
  checks were left intact.

### Darken `--fg-light` token to meet WCAG AA contrast — `c1fbcca`
- File: `Backend/wwwroot/css/site.css`
- AP-5 (audit F-19): the global muted-text token `--fg-light` changed from
  `#9ca3af` (~2.5:1 on white, fails AA body text) to `#6b7280`
  (~4.83:1, passes AA 4.5:1), the smallest darkening that clears the threshold
  while keeping the muted look.
- Behavior change: presentational only (global token used widely). Contrast
  improves at all usage sites; no layout change.
- Note: full WCAG conformance still requires manual assistive-technology testing
  and expert accessibility review. This is a targeted contrast fix, not a
  conformance claim.

---

## Presentational / view-only changes

### Shared UI foundations — `c892248`
Defined missing CSS referenced by markup (`.spinner`, `.stat-icon`, `.font-mono`
+ `--font-mono`), added a distinct `.alert-success` and `.toast-warning` with the
toast scan widened to all four alert types, de-duplicated toast vs inline banner,
fixed the submit-button loading state (removed the fixed 5s timer, added a
double-submit guard), added modal focus-trap + focus restore + dialog ARIA,
labeled decorative SVGs, and session-gated footer links. Addresses F-01, F-04,
F-06, F-13, F-14, F-15, F-18, F-20. Nothing was deleted (per the move-don't-delete
steer).

### Test project repair — `52e6e52`
Fixed a pre-existing compile failure in `Backend.Tests` (the `Register` action had
gained security-question parameters that the tests never passed) and registered
stub auth services on the test HttpContext so cookie sign-in works under test.
Restored the suite to 18/18.

### UC06 make-booking fix — `c1e2d26`
Availability slots on the instructor detail page are now clickable buttons that
preselect the time in the booking modal, with a client guard requiring both a
lesson and a slot, hover/focus affordances, and standardized empty states.
Addresses F-05, F-09, F-16. The server `Book` contract was unchanged.

### Account + student view polish — `b8a6ce1`
Label/`for` associations across the account and student forms, a shared auth-page
shell, success banners switched to `alert-success`, a helpful empty state and
rating guard on completed lessons, decorative card icons hidden from assistive
tech, and a client-side file size/type pre-check on the payment proof upload that
mirrors the server rule. Addresses F-03, F-04, F-09, F-12, F-16, F-17, F-21.

### Instructor + admin view polish — `56c0958`
Label associations, dialog ARIA + labeled close buttons, success styling,
standardized empty states, aria-labels on icon-only buttons, `rel="noopener"` on
proof links, always-rendered instructor booking-requests section (so the nav
anchor resolves), and a robust real-time comparison in the availability client
check. Addresses F-04, F-07, F-11, F-16, F-17, F-18, F-26.

### Find Instructors honest Sort dropdown — `a989d23`
Added a labeled Sort dropdown whose options map to the controller's existing
`filterBy` values and are honestly labeled as sorting ("Sort: Most experienced",
"Sort: Location A-Z"), rather than implying a type filter the action does not
perform. View-only; the controller query was unchanged.

### Live-map contrast + Go Live feedback — `0e21fc2`
Raised the live-map sidebar `.trust-note` muted text to meet AA, added an
aria-live status region announcing "you are now live/offline", labeled the Go
Live / Go Offline buttons, and marked decorative status icons hidden. SignalR
wiring, the hub, and the location payload shape were untouched.

### Supporting polish (not from the audit, requested during the session)
- Faster animations + `prefers-reduced-motion` support — `5a98dd9`
- SVG logo + favicon set wired up, navbar no longer loads a ~1MB PNG — `109e600`
- Simplified bold "B" favicon/app icons — `a002de5`
- Full-color logo with wordmark in the navbar — `b3464f9`
- Live-map instructor card: initials fallback for missing/invalid avatar, and
  "Price on request" instead of "R0" — `1d6f15d`, `d967578`
- Page-load progress bar, skeleton loaders, loading-overlay helpers — `ece4231`
- Password show/hide toggle + logout confirmation + asset cache-busting
  (`asp-append-version`) — `9d7066a`
- Profile avatar dropdown + desktop nav synced with the hamburger menu — `436e78b`

---

## Deferred (not implemented — await explicit approval)

These change controller signatures, query behavior, or add new routes, and were
intentionally held back:

_All three were subsequently implemented — see the next section._

---

## Phase 4 — formerly deferred AP items (now implemented)

Each of these changes server behavior, a controller contract, or routing, so each
was built in its own worktree and reviewed by a second model before merging.

### AP-4 — Lesson-type filter on Find Instructors — `ea65a23`
- Files: `Backend/Controllers/InstructorsController.cs`, `Backend/Views/Instructors/Index.cshtml`
- Change: `Index` gained a nullable `string? lessonType` parameter. When set (and
  not "all"), the query additionally filters to instructors who have a
  `LessonOffering` of that type, applied before the existing sort. A labeled
  lesson-type dropdown was added to the search form.
- Behavior change: yes — results now narrow by lesson type. The existing search
  and the `filterBy` sort are untouched; the new parameter is nullable so old
  links without it still work.

### AP-6 — Gate payment-proof downloads behind authorization — `0f1a8f1`
- Files: new `Backend/Controllers/PaymentController.cs`,
  `Backend/Controllers/StudentController.cs`, `Backend/Views/InstructorDashboard/Payments.cshtml`,
  `Backend/Views/Admin/ViewPayments.cshtml`, `.gitignore`
- AP-6 (audit F-26): payment proofs are no longer served as public static files.
  New uploads are written to `ContentRootPath/private-uploads/payments` (outside
  `wwwroot`, gitignored). A new `[Authorize]` action `GET /Payment/Proof/{id}`
  streams a proof only to the owning student, the booking's instructor, or an
  admin; everyone else gets NotFound. Legacy records still under
  `/uploads/payments` resolve via a wwwroot fallback so old data keeps working.
  Both payment views link to the gated action.
- Behavior change: yes — proof URLs are now access-controlled. No model or
  migration change. Added 6 authorization tests.
- Known residual (non-blocking): proofs uploaded before this change still sit
  under `wwwroot/uploads/payments` and remain publicly reachable by direct URL;
  disabling static serving of that legacy path (or migrating old files into the
  private dir) would close it.

### AP-3 — View models + validation for Account and Profile forms — `22c9d5c`, `3dec0d6`
- Files: new `Backend/ViewModels/` (RegisterViewModel, LoginViewModel,
  ResetPasswordViewModel, StudentProfileViewModel, InstructorProfileViewModel,
  and a custom `RequiredNotBlank` attribute), `Backend/Controllers/AccountController.cs`,
  `Backend/Controllers/ProfileController.cs`, the five matching views, and the
  updated `Backend.Tests`.
- AP-3 (audit F-09, F-10): the five forms now bind to view models with
  `asp-for` / `asp-validation-for` / validation summary, so all field errors show
  at once. Server-only rules that annotations can't express (duplicate email,
  security-answer check, old != new password, file validation) run after the
  `ModelState.IsValid` gate via `ModelState.AddModelError`. A `RequiredNotBlank`
  attribute preserves the old behavior of rejecting whitespace-only input (a
  regression the reviewer caught and that was fixed before merge).
- Behavior change: yes — controller action signatures now take view models.
  All prior behavior preserved: auto-login + role redirect + session (incl.
  UserPicture), the three-table login, security-answer verification, old != new
  password rejection, profile picture upload, and Experience remaining
  non-writable by instructors. No migration or schema change. Tests migrated to
  the new signatures.

---

## Verification

- `dotnet build Backend/BiDE.csproj` — 0 errors.
- `dotnet test Backend.Tests` — 37 passed, 0 failed, 0 skipped (grew from 18 as
  AP-6 and AP-3 added coverage).
- Behavior-changing sub-batches (4A security guards, 4B contrast token, AP-4,
  AP-6, AP-3) were each reviewed by a second model before merge; review records
  are under `.agents/tasks/bide-uiux-audit-2026-10-14/` (`b4a/`, `b4b/`, `ap4/`,
  `ap6/`, `ap3/`). AP-3 required a second iteration after the reviewer caught a
  whitespace-input regression, which was fixed before merge.
