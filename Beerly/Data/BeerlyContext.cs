using Microsoft.EntityFrameworkCore;
using Beerly.Models;

namespace Beerly.Data
{
    public class BeerlyContext : DbContext
    {
        public BeerlyContext(DbContextOptions<BeerlyContext> options) : base(options) { }

        public DbSet<Beer> Beers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Beer)
                .WithMany()
                .HasForeignKey(r => r.BeerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}