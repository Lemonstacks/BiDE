using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register the database context with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure session for authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".BiDE.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Skip HTTPS redirect in development so LAN devices stay on fast plain HTTP
// (the dev HTTPS cert is untrusted on other devices and slows the handshake)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Cache static assets (css/js/images) so repeat loads are instant IN PRODUCTION.
// In Development we skip the long cache so edits show up on a normal refresh
// (no hard-refresh needed).
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}
else
{
    app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
        }
    });
}

app.UseRouting();

// Session must be before Authorization and after Routing
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// On startup, create/update the database from the EF Core migrations so the app is
// runnable with no manual "dotnet ef database update" step, then seed demo accounts
// into an empty database. This makes the published executable turnkey: launch it and
// the database sets itself up. (SQL Server LocalDB must be installed on the machine.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    DemoData.Seed(db);
}

app.Run();

// Turnkey demo seeding. Populates an EMPTY database with base accounts so the app is
// usable on first run. Idempotent: does nothing if any instructor already exists, so
// it never duplicates or touches real data. Passwords are plain text, matching the
// project's existing accepted auth decision.
static class DemoData
{
    public static void Seed(ApplicationDbContext db)
    {
        try
        {
            if (db.Instructors.Any()) return; // already populated

            if (!db.Admins.Any())
            {
                db.Admins.Add(new Admin
                {
                    FirstName = "Site", LastName = "Admin", Contact = "0110000000",
                    Email = "admin@bide.test", Password = "Admin123"
                });
            }

            if (!db.Students.Any())
            {
                db.Students.Add(new Student
                {
                    FirstName = "Sam", LastName = "Student", Contact = "0120000000",
                    Email = "student@bide.test", Suburb = "Johannesburg", Password = "Student123",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var now = DateTime.UtcNow;
            var thandi = new Instructor
            {
                FirstName = "Thandi", LastName = "Nkosi", Contact = "0130000001",
                Email = "thandi@bide.test", Password = "Teach123", Experience = 6,
                IsVerified = true, Certification = "K53 Certified Instructor", Suburb = "Sandton",
                Status = InstructorStatus.Approved, ApplicationDate = now, ApprovalDate = now,
                CreatedAt = now
            };
            var sipho = new Instructor
            {
                FirstName = "Sipho", LastName = "Dlamini", Contact = "0130000002",
                Email = "sipho@bide.test", Password = "Teach123", Experience = 10,
                IsVerified = true, Certification = "Advanced Driving Instructor", Suburb = "Rosebank",
                Status = InstructorStatus.Approved, ApplicationDate = now, ApprovalDate = now,
                CreatedAt = now
            };
            var lerato = new Instructor
            {
                FirstName = "Lerato", LastName = "Mokoena", Contact = "0130000003",
                Email = "lerato@bide.test", Password = "Teach123", Experience = 2,
                IsVerified = false, Certification = "Learner Instructor", Suburb = "Soweto",
                Status = InstructorStatus.Pending, ApplicationDate = now,
                CreatedAt = now
            };
            db.Instructors.AddRange(thandi, sipho, lerato);
            db.SaveChanges();

            foreach (var inst in new[] { thandi, sipho })
            {
                db.LessonOfferings.AddRange(
                    new LessonOffering { InstructorId = inst.InstructorId, Title = "Beginner Driving Lesson", LessonType = "Manual", Price = 350m, Description = "Perfect for first-timers.", CreatedAt = now },
                    new LessonOffering { InstructorId = inst.InstructorId, Title = "Automatic Essentials", LessonType = "Automatic", Price = 400m, Description = "Smooth, confident automatic driving.", CreatedAt = now });

                for (int d = 0; d < 5; d++)
                {
                    var date = DateTime.Today.AddDays(d);
                    db.Availabilities.Add(new Availability { InstructorId = inst.InstructorId, Date = date, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), AvailabilityStatus = AvailabilityStatus.Available, CreatedAt = now });
                    db.Availabilities.Add(new Availability { InstructorId = inst.InstructorId, Date = date, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 0, 0), AvailabilityStatus = AvailabilityStatus.Available, CreatedAt = now });
                }
            }
            db.SaveChanges();
        }
        catch
        {
            // Never let demo seeding crash startup.
        }
    }
}
