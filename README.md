# BiDE - Booking & Instructor Dispatch Engine

**BiDE** is a web-based driving lesson marketplace designed to connect learner drivers with verified driving instructors. The platform allows students to find instructors, request bookings, upload proof of payment, track lesson progress, and leave reviews. Instructors can manage lesson offerings, availability, booking requests, and payment verification, while administrators oversee users, instructor applications, bookings, payments, and reports.

> **Project Status:** In development.  
> Backend structure and database schema are complete. Frontend integration pending.

---

## Project Overview

Many learner drivers struggle to find reliable, affordable, and accessible driving instructors. Most learners depend on informal methods such as word-of-mouth, social media, or messaging platforms, which can be inefficient, unclear, and unreliable.

At the same time, experienced driving instructors often lack a structured digital platform where they can list their services, manage bookings, track student progress, and communicate their availability clearly.

BiDE solves this by acting as a two-sided platform between:

- **Students** who want to book driving lessons.
- **Instructors** who want to offer driving lessons.
- **Administrators** who manage and monitor the platform.

The system is designed to support instructor discovery, flexible booking, payment-proof handling, progress tracking, reviews, and administrative control.

---

## Problem Statement

Learner drivers need a reliable way to find and book driving instructors based on factors such as location, price, availability, and experience. Instructors also need a better way to advertise their services, manage bookings, and track student progress.

BiDE aims to centralise this process by providing a structured system for booking and managing driving lessons.

---

## Project Objectives

The main objectives of BiDE are to:

- Provide a central platform for finding driving instructors.
- Allow students to request single or multi-session driving lessons.
- Allow instructors to manage their availability and lesson offerings.
- Support payment proof uploads for externally handled payments.
- Allow instructors to verify or reject payment proof.
- Track lesson progress and completed lessons.
- Allow students to review instructors after completed lessons.
- Provide administrators with tools to manage users, bookings, payments, and instructor applications.

---

## User Roles

### Student

Students use the system to search for instructors, view instructor profiles, request bookings, upload proof of payment, view booking statuses, track lesson progress, view completed lessons, and leave reviews.

### Instructor

Instructors use the system to manage their profile, availability, lesson offerings, booking requests, lesson feedback, completed lessons, and payment verification.

### Administrator

Administrators manage the overall system by reviewing instructor applications, managing users, monitoring bookings, viewing payments, and generating reports.

---

## Core Features

### Student Features

- Register and manage account
- Search for instructors
- View instructor profiles
- Make booking requests
- Upload proof of payment
- View booking status
- View lesson progress
- View completed lessons
- Leave instructor reviews

### Instructor Features

- Register and manage profile
- Manage availability
- Manage lesson offerings
- View booking requests
- Respond to booking requests
- Verify payment proof
- Provide lesson feedback
- View completed lessons

### Admin Features

- Review instructor applications
- Manage users
- Monitor bookings
- View payments
- Generate reports

---

## Business Rules and Assumptions

- A student must register and have a verified account before making a booking request.
- An instructor must apply and be approved by an administrator before appearing in search results.
- A booking is confirmed only after proof of payment has been uploaded and verified.
- Students may only leave reviews for completed lessons.
- Bookings may be cancelled or rescheduled subject to system validation.
- Instructors manage their own availability.
- Payments are handled externally through EFT or cash.
- The system only stores and verifies proof of payment.
- Each booking is linked to one lesson offering.
- Administrators have access to user, booking, payment, and reporting functions.

---

## System Constraints

- The system is planned as a web-based application only.
- No mobile or desktop version is planned at this stage.
- Real payment gateway integration is not included.
- Payment functionality will be simulated through proof-of-payment uploads.
- The system will use technologies required by the academic module.
- The project must be completed within the academic project timeframe.
- The system will use sample or test data during development.
- Real-world instructor certification validation is not included.
- Advanced features such as real-time notifications or integrated messaging are not part of the current scope.

---

## Technology Stack

- **Backend:** ASP.NET MVC (.NET 8)
- **ORM:** Entity Framework Core 8.0
- **Database:** SQL Server (MSSQLSERVER01 instance)
- **Frontend:** React (via Base44 — integration pending)
- **IDE:** Visual Studio / Kiro
- **Version Control:** Git and GitHub

---

## Database Schema

The database consists of 9 tables with the following relationships:

| Table | Description |
|-------|-------------|
| **Instructors** | Driving instructors with profile, credentials, and verification status |
| **Students** | Learner drivers with profile information |
| **Admins** | System administrators |
| **Availabilities** | Instructor time slots (date, start/end time, status) |
| **LessonOfferings** | Lesson packages instructors offer (type, title, price) |
| **Bookings** | Links student, instructor, availability slot, and lesson offering |
| **LessonProgresses** | Tracks individual lesson sessions within a booking |
| **Payments** | Proof-of-payment records with verification workflow |
| **Reviews** | Student ratings and comments after completed lessons |

### Key Relationships

```
Instructor ──1:many──> Availabilities
Instructor ──1:many──> LessonOfferings
Instructor ──1:many──> Bookings
Student ──1:many──> Bookings
Booking ──many:1──> Availability (schedule slot)
Booking ──many:1──> LessonOffering
Booking ──1:many──> LessonProgresses
Booking ──1:1──> Payment
Booking ──1:1──> Review
Payment ──many:1──> Instructor (verifier)
```

---

## Repository Structure

```text
BiDE/
├── README.md
├── Backend/
│   ├── BiDE.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   ├── Models/
│   │   ├── Admin.cs
│   │   ├── Availability.cs
│   │   ├── Booking.cs
│   │   ├── Instructor.cs
│   │   ├── LessonOffering.cs
│   │   ├── LessonProgress.cs
│   │   ├── Payment.cs
│   │   ├── Review.cs
│   │   └── Student.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Migrations/
│   ├── Services/
│   ├── Views/
│   └── wwwroot/
├── Frontend/
│   └── (React app — pending integration)
└── Documentation/
    ├── Database Design Diagrams/
    ├── FSSB and Schedule/
    ├── UI design items/
    └── usecases inline/
```

---

## Documentation

Project documentation is stored in the `Documentation/` folder.

Current documentation includes:

- Functional Specification and Solution Blueprint
- Use Case documentation
- System Flow documentation
- CRUD Matrix
- Database and system design diagrams (Visio)
- UI design preview (Base44)
- Project feedback documents

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local instance)
- Entity Framework Core CLI (`dotnet tool install --global dotnet-ef`)

### Setup

```bash
cd Backend
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

### Connection String

The application connects to SQL Server using Windows Authentication. Update `appsettings.json` if your server instance differs:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER\\INSTANCE;Database=BIDE;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
}
```
