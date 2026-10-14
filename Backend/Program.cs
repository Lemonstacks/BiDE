using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using BiDE.Data;
using BiDE.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Register the database context with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".BiDE.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

// Configure session (still used for lightweight UI state)
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

// Cache static assets (css/js/images) so repeat loads are instant
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
    }
});

app.UseRouting();

// Session must be before auth and after Routing
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<InstructorHub>("/instructorHub");

// One-time demo data seeding: give the first approved instructor sample
// offerings so the profile page isn't hollow. Idempotent, safe to re-run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DemoData.SeedOfferings(db);
}

app.Run();

// Demo seeding helper
static class DemoData
{
    public static void SeedOfferings(ApplicationDbContext db)
    {
        try
        {
            var instructor = db.Instructors.FirstOrDefault(i => i.Status == BiDE.Models.InstructorStatus.Approved);
            if (instructor == null) return;

            var hasOfferings = db.LessonOfferings.Any(o => o.InstructorId == instructor.InstructorId);
            if (hasOfferings) return;

            db.LessonOfferings.AddRange(
                new BiDE.Models.LessonOffering
                {
                    InstructorId = instructor.InstructorId,
                    Title = "Beginner Driving Lesson",
                    LessonType = "Manual",
                    Price = 350m,
                    Description = "Perfect for first-timers. Covers controls, moving off and stopping.",
                    CreatedAt = DateTime.UtcNow
                },
                new BiDE.Models.LessonOffering
                {
                    InstructorId = instructor.InstructorId,
                    Title = "Highway & Freeway Confidence",
                    LessonType = "Automatic",
                    Price = 450m,
                    Description = "Build confidence merging, overtaking and driving at speed.",
                    CreatedAt = DateTime.UtcNow
                },
                new BiDE.Models.LessonOffering
                {
                    InstructorId = instructor.InstructorId,
                    Title = "K53 Test Preparation",
                    LessonType = "Manual",
                    Price = 400m,
                    Description = "Mock test run-through covering everything examiners look for.",
                    CreatedAt = DateTime.UtcNow
                });

            db.SaveChanges();
        }
        catch
        {
            // Non-fatal: never let demo seeding crash startup
        }
    }
}
