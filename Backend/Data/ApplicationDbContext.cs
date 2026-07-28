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

        public DbSet<User> Users { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<LessonOffering> LessonOfferings { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - unique email
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // User -> Instructor (one-to-one)
            modelBuilder.Entity<Instructor>(entity =>
            {
                entity.HasOne(i => i.User)
                      .WithOne(u => u.Instructor)
                      .HasForeignKey<Instructor>(i => i.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // User -> Student (one-to-one)
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasOne(s => s.User)
                      .WithOne(u => u.Student)
                      .HasForeignKey<Student>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Instructor -> LessonOfferings (one-to-many)
            modelBuilder.Entity<LessonOffering>(entity =>
            {
                entity.HasOne(lo => lo.Instructor)
                      .WithMany(i => i.LessonOfferings)
                      .HasForeignKey(lo => lo.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Student -> Bookings (one-to-many)
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Student)
                      .WithMany(s => s.Bookings)
                      .HasForeignKey(b => b.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.LessonOffering)
                      .WithMany(lo => lo.Bookings)
                      .HasForeignKey(b => b.LessonOfferingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Booking -> Payment (one-to-one)
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Review relationships
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Booking)
                      .WithOne(b => b.Review)
                      .HasForeignKey<Review>(r => r.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Student)
                      .WithMany(s => s.Reviews)
                      .HasForeignKey(r => r.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Instructor)
                      .WithMany(i => i.Reviews)
                      .HasForeignKey(r => r.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
