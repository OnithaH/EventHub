using Microsoft.EntityFrameworkCore;
using EventHub.Models.Entities;

namespace EventHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSet properties for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<BookingDiscount> BookingDiscounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasConversion<int>();
            });

            // Configure Event entity
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.Organizer)
                      .WithMany(u => u.OrganizedEvents)
                      .HasForeignKey(e => e.OrganizerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Venue)
                      .WithMany(v => v.Events)
                      .HasForeignKey(e => e.VenueId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Customer)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Event)
                      .WithMany(e => e.Bookings)
                      .HasForeignKey(b => b.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(b => b.Status).HasConversion<int>();
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.Status).HasConversion<int>();
            });

            // Configure Ticket entity
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasOne(t => t.Booking)
                      .WithMany(b => b.Tickets)
                      .HasForeignKey(t => t.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Status).HasConversion<int>();
                entity.HasIndex(t => t.TicketNumber).IsUnique();
            });

            // Configure BookingDiscount entity (many-to-many)
            modelBuilder.Entity<BookingDiscount>(entity =>
            {
                entity.HasOne(bd => bd.Booking)
                      .WithMany()
                      .HasForeignKey(bd => bd.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bd => bd.Discount)
                      .WithMany(d => d.BookingDiscounts)
                      .HasForeignKey(bd => bd.DiscountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Discount entity
            modelBuilder.Entity<Discount>(entity =>
            {
                entity.HasIndex(d => d.Code).IsUnique();
            });
        }
    }
}