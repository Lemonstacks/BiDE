# BiDE - Architecture & Technical Specification

## Overview

BiDE (Book Instructor Driving Education) is a web platform that connects driving students with verified instructors. Students can browse, book, pay, and track lesson progress. Instructors manage availability, offerings, and payments. Admins oversee the entire system.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8.0 (MVC) |
| Language | C# 12 |
| Database | SQL Server (LocalDB) via Entity Framework Core 8 |
| ORM | EF Core with Code-First migrations |
| Real-Time | SignalR (WebSocket) |
| Frontend | Razor Views, Leaflet/OpenStreetMap, vanilla CSS, vanilla JS |
| Auth | Session-based (server-side, 60-min idle timeout) |
| File Storage | Local filesystem (wwwroot/uploads/) |

---

## Project Structure

```
BiDE/
├── Backend/
│   ├── Controllers/
│   │   ├── AccountController.cs          # Login, Register, Logout
│   │   ├── AdminController.cs            # Approval, user mgmt, monitoring
│   │   ├── HomeController.cs             # Landing page
│   │   ├── InstructorDashboardController.cs  # Instructor operations
│   │   ├── InstructorsController.cs      # Listing, detail, booking
│   │   ├── ProfileController.cs          # Profile CRUD for all roles
│   │   └── StudentController.cs          # Bookings, payments, reviews
│   ├── Hubs/
│   │   └── InstructorHub.cs              # SignalR real-time location hub
│   ├── Data/
│   │   └── ApplicationDbContext.cs       # EF Core context + seed data
│   ├── Migrations/
│   ├── Models/
│   │   ├── Admin.cs
│   │   ├── Availability.cs
│   │   ├── Booking.cs
│   │   ├── Instructor.cs
│   │   ├── InstructorLocation.cs         # Real-time map DTO
│   │   ├── LessonOffering.cs
│   │   ├── LessonProgress.cs
│   │   ├── Payment.cs
│   │   ├── Review.cs
│   │   └── Student.cs
│   ├── Views/
│   │   ├── Account/                      # Login, Register
│   │   ├── Admin/                        # Index, ManageUsers, MonitorBookings, ViewPayments
│   │   ├── Home/                         # Landing page, Error
│   │   ├── InstructorDashboard/          # Index, Offerings, Availability, LessonProgress, Payments, GoLive
│   │   ├── Instructors/                  # Index, Detail, LiveMap
│   │   ├── Profile/                      # StudentProfile, InstructorProfile, AdminProfile
│   │   ├── Shared/                       # _Layout.cshtml
│   │   └── Student/                      # Bookings, CompletedLessons, LessonProgress
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   ├── js/site.js
│   │   ├── js/livemap.js                 # SignalR + Leaflet map logic
│   │   ├── images/
│   │   └── uploads/                      # Profile pics, payment proofs
│   ├── Program.cs
│   ├── appsettings.json
│   └── BiDE.csproj
├── docs/
│   ├── ARCHITECTURE.md                   # This file
│   └── REALTIME-MAP-SPEC.md             # Live map feature spec
└── BIDE.sln
```

---

## Database Schema

```
Student (1) ──< (M) Booking (M) >── (1) Instructor
                     |
                     ├── (1) Availability (Schedule slot)
                     ├── (1) LessonOffering
                     ├── (0..1) Payment
                     ├── (0..1) Review
                     └── (M) LessonProgress

Instructor (1) ──< (M) Availability
Instructor (1) ──< (M) LessonOffering
Instructor (1) ──< (M) Payment

Admin (1) ── approves ──> Instructor
```

---

## User Roles

### Student
- Register, Login, Logout
- Browse and search approved instructors
- View instructor detail (offerings, availability, reviews)
- Book lessons (standard or real-time via live map)
- Cancel pending/accepted bookings
- Submit proof of payment (file upload)
- View lesson progress
- Leave reviews (1-5 stars)
- Manage profile + picture

### Instructor
- Register, Login, Logout
- Dashboard with booking stats
- Manage lesson offerings (CRUD)
- Manage availability slots
- Accept / Reject / Complete bookings
- Record lesson progress
- Verify or reject student payments
- Go Live on map (broadcast GPS location)
- Manage profile + picture
- Delete own profile

### Admin
- Login, Logout (seed: admin@bide.com / Admin123)
- Approve / Reject / Suspend / Reinstate instructors
- Monitor all bookings (filtered)
- View all payments (filtered)
- Manage users (students, instructors, admins)
- Deactivate student accounts

---

## Validation Rules

| Area | Rules |
|------|-------|
| Registration | All fields required, email must contain @ and ., password min 6 chars, passwords must match, email unique across tables |
| Availability | End time > start time, not in past, no overlapping slots |
| Offerings | Title required, price >= 0 |
| Bookings | Offering + schedule same instructor, slot still available, instructor still approved, no duplicates |
| Status Changes | Only Accepted can be Completed, only Pending/Accepted can be Cancelled |
| Payments | Method in (EFT, Cash, Card), proof file required, max 5MB, only jpg/png/pdf |
| Reviews | Rating 1-5, one per booking |
| Progress | Feedback required, duration > 0, booking must be Accepted |
| Profile | Name + contact required, profile pic max 3MB jpg/png/gif only |
| Delete Profile | Cannot delete with active bookings, cascades related data |

---

## Application Flows

### Booking Lifecycle (Standard)

```
Student books slot
    -> Booking: "Pending"
        -> Instructor accepts -> "Accepted"
            -> Student submits payment -> Payment: "Pending"
                -> Instructor verifies -> Payment: "Verified"
            -> Instructor records progress
            -> Instructor marks complete -> "Completed"
                -> Student leaves review
        -> Instructor rejects -> "Rejected" (slot freed)
        -> Student cancels -> "Cancelled" (slot freed)
```

### Booking Lifecycle (Real-Time Map)

```
Instructor goes live (broadcasts GPS)
    -> Student sees marker on live map
        -> Student clicks marker, clicks "Book Now"
            -> Booking created: "Pending"
            -> Instructor marker removed from all maps instantly
            -> Normal flow continues (accept/reject/complete)
```

### Instructor Approval

```
Instructor registers -> "Pending"
    -> Admin approves -> "Approved" (visible to students)
    -> Admin rejects -> "Rejected" (with reason)
    -> Admin suspends -> "Suspended" (hidden)
    -> Admin reinstates -> "Approved"
```

---

## Real-Time Communication (SignalR)

| Hub | Endpoint | Purpose |
|-----|----------|---------|
| InstructorHub | /instructorHub | Location streaming + booking events |

### Hub Methods

| Method | Caller | Event Fired | Effect |
|--------|--------|-------------|--------|
| UpdateLocation | Instructor | InstructorLocationUpdated | Marker appears/moves on student maps |
| GoOffline | Instructor | InstructorRemoved | Marker removed from all maps |
| OnDisconnectedAsync | Framework | InstructorRemoved | Auto-cleanup on tab close |
| BroadcastInstructorRemoved | Server | InstructorRemoved | Hide booked instructor |

---

## Configuration

### Endpoints
- HTTP: http://localhost:5050
- HTTPS: https://localhost:5051

### Database
```
Server=(localdb)\MSSQLLocalDB;Database=BIDE;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;
```

### Seed Data
- Admin: admin@bide.com / Admin123

---

## Known Limitations

1. Passwords stored in plain text (development only)
2. No rate limiting or account lockout
3. No email verification
4. Local file storage (no CDN)
5. No pagination on large lists
6. Session-only auth (no JWT)
7. No unit or integration tests
8. GPS accuracy depends on device hardware
