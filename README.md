# BiDE — Driving Lesson Marketplace

BiDE is an ASP.NET Core 8 MVC web application that connects learner drivers with
verified driving instructors. Students can search instructors, book lessons, pay
(with proof of payment), track lesson progress and leave reviews. Instructors manage
their availability, lesson offerings and booking requests. Admins approve instructor
applications, manage users and monitor bookings and payments.

---

## 1. Technology Stack

- **.NET 8** (ASP.NET Core MVC)
- **Entity Framework Core 8** with **SQL Server LocalDB**
- **Razor views** with plain CSS (design tokens, no build pipeline)
- Cookie-based authentication + session

## 2. Prerequisites

Install these before running the project:

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
   (verify with `dotnet --version`, should report 8.x)
2. **SQL Server LocalDB** — comes with Visual Studio, or install the
   "SQL Server Express LocalDB" feature from the SQL Server Express installer.
   (verify with `sqllocaldb info`)
3. **EF Core CLI tools** (for creating the database) — install once, globally:
   ```
   dotnet tool install --global dotnet-ef --version 8.*
   ```
4. (Optional) **Visual Studio 2022** or **VS Code** to open the solution.

## 3. Project Structure

```
BiDE/
├─ BIDE.sln                 Solution file
├─ Backend/                 The web application (ASP.NET Core MVC)
│  ├─ Controllers/          Account, Instructors, InstructorDashboard, Student, Admin, Profile, Payment, Home
│  ├─ Models/               Entities (Student, Instructor, Admin, Booking, Payment, ...) + StatusConstants
│  ├─ Data/                 ApplicationDbContext (EF Core)
│  ├─ Migrations/           EF Core migrations
│  ├─ Views/                Razor views
│  ├─ wwwroot/              CSS, JS, images, PWA assets
│  ├─ appsettings.json      Connection string + config
│  └─ Program.cs            App startup / middleware
├─ Backend.Tests/           xUnit test project (47 tests)
├─ Documentation/           Architecture / use-case documentation
└─ seed-demo.sql            Optional demo-data script (see step 6)
```

## 4. Database Configuration

The connection string is in `Backend/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BIDE;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
}
```

This points at a LocalDB database named **BIDE**. You do not normally need to change
this. If you use a different SQL Server instance, update the `Server=` value here.

## 5. Setup & Run

From a terminal in the project root (the folder containing `BIDE.sln`):

```powershell
# 1. Restore NuGet packages
dotnet restore

# 2. Create / update the database from the EF Core migrations
dotnet ef database update --project Backend/BiDE.csproj

# 3. Build
dotnet build

# 4. Run the web app
dotnet run --project Backend/BiDE.csproj
```

The app starts and prints the URLs it is listening on, typically:

- http://localhost:5050
- https://localhost:5051

Open either URL in a browser. (Use `https://localhost:5051` for the most complete
experience; accept the local development certificate warning if prompted.)

> If `dotnet ef` is not found, install it (see Prerequisites step 3) and re-run step 2.

## 6. Demo Data (optional but recommended)

A fresh database starts **empty**. You have two options to get usable accounts:

**Option A — Register through the app.** Use the Sign Up page to create a student
and/or instructor account. New instructors start as *Pending*; sign in as an admin to
approve them (you will first need an admin account — use Option B to seed one).

**Option B — Load the included demo data.** After step 5.2 (database created), run the
bundled script against the BIDE database:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d BIDE -i seed-demo.sql
```

This adds the following demo logins (passwords are plain text — see Notes):

| Role                 | Email                 | Password     |
|----------------------|-----------------------|--------------|
| Admin                | admin@bide.test       | Admin123     |
| Student              | student@bide.test     | Student123   |
| Instructor (approved)| thandi@bide.test      | Teach123     |
| Instructor (approved)| sipho@bide.test       | Teach123     |
| Instructor (pending) | lerato@bide.test      | Teach123     |

The two approved instructors come with sample lesson offerings and availability slots,
so you can book a lesson immediately as the student.

## 7. Running the Tests

```powershell
dotnet test
```

All 47 tests should pass. They cover account registration/login, booking, payment
(including proof-upload validation), reviews, instructor offerings/availability,
booking responses, and admin user/payment management.

## 8. Key Features

- **Students:** search instructors, view profiles, book lessons, upload proof of
  payment (EFT or Cash), view booking status, track lesson progress, leave reviews.
- **Instructors:** manage availability and lesson offerings, view and respond to
  booking requests (accept/reject), verify or reject payments, record lesson progress.
- **Admins:** review and approve/reject instructor applications, manage users
  (suspend/reinstate instructors, deactivate students), monitor bookings, view and
  export payments (CSV).

## 9. Notes / Known Limitations

- **Passwords are stored as plain text.** This is an accepted scope decision for this
  academic project; it is not production-safe.
- **Proof-of-payment uploads** are validated by both file extension and the file's
  actual content signature (magic number), so a disallowed file renamed to .pdf/.png
  is rejected. Allowed types: JPG, PNG, PDF (max 5 MB).
- **Card payments are not supported** — only EFT (with proof) and Cash. There is no
  payment gateway integration.
- The database provider is SQL Server LocalDB; no external database server is required.

## 10. Troubleshooting

- **"Cannot open database / login failed"** — ensure LocalDB is installed and running
  (`sqllocaldb start MSSQLLocalDB`), then re-run `dotnet ef database update`.
- **Build fails with a file-lock error on BiDE.exe** — a previous instance of the app
  is still running; stop it, then rebuild.
- **Port already in use** — stop any other instance using ports 5050/5051, or change
  the ports in `Backend/Properties/launchSettings.json`.
