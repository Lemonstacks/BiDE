using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

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

// Cache static assets (css/js/images) so repeat loads are instant
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
    }
});

app.UseRouting();

// Session must be before Authorization and after Routing
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<InstructorHub>("/instructorHub");

app.Run();
