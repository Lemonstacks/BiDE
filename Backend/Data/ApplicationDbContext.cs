using Microsoft.EntityFrameworkCore;
using BiDE.Models;

namespace BiDE.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for each entity will be added here as models are created
        // public DbSet<User> Users { get; set; }
        // public DbSet<Instructor> Instructors { get; set; }
        // public DbSet<Student> Students { get; set; }
        // public DbSet<Booking> Bookings { get; set; }
        // public DbSet<LessonOffering> LessonOfferings { get; set; }
        // public DbSet<Payment> Payments { get; set; }
        // public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Entity configurations and relationships will be defined here
        }
    }
}
