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

        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<LessonOffering> LessonOfferings { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique email constraints
            modelBuilder.Entity<Instructor>()
                .HasIndex(i => i.Email).IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email).IsUnique();

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email).IsUnique();

            // Instructor -> Admin (approval relationship)
            modelBuilder.Entity<Instructor>(entity =>
            {
                entity.HasOne(i => i.ApprovedByAdmin)
                      .WithMany()
                      .HasForeignKey(i => i.ApprovedByAdminId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Instructor -> Availability (one-to-many)
            modelBuilder.Entity<Availability>(entity =>
            {
                entity.HasOne(a => a.Instructor)
                      .WithMany(i => i.Availabilities)
                      .HasForeignKey(a => a.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Instructor -> LessonOffering (one-to-many)
            modelBuilder.Entity<LessonOffering>(entity =>
            {
                entity.HasOne(lo => lo.Instructor)
                      .WithMany(i => i.LessonOfferings)
                      .HasForeignKey(lo => lo.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Booking relationships
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Instructor)
                      .WithMany(i => i.Bookings)
                      .HasForeignKey(b => b.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Student)
                      .WithMany(s => s.Bookings)
                      .HasForeignKey(b => b.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Schedule)
                      .WithMany(a => a.Bookings)
                      .HasForeignKey(b => b.ScheduleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.LessonOffering)
                      .WithMany(lo => lo.Bookings)
                      .HasForeignKey(b => b.OfferId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Original schedule for rescheduled bookings
                entity.HasOne(b => b.OriginalSchedule)
                      .WithMany()
                      .HasForeignKey(b => b.OriginalScheduleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Booking -> LessonProgress (one-to-many)
            modelBuilder.Entity<LessonProgress>(entity =>
            {
                entity.HasOne(lp => lp.Booking)
                      .WithMany(b => b.LessonProgresses)
                      .HasForeignKey(lp => lp.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking -> Payment (one-to-one)
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Instructor)
                      .WithMany(i => i.Payments)
                      .HasForeignKey(p => p.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Booking -> Review (one-to-one)
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
            });
            // Seed Admin
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminId = 1,
                    FirstName = "System",
                    LastName = "Administrator",
                    Contact = "0712345678",
                    Email = "admin@bide.com",
                    Password = "Admin123"
                });
        }
    }
}
