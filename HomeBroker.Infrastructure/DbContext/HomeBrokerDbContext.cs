using HomeBroker.Infrastructure.Identity;
using HouseBroker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeBroker.Infrastructure.DbContext
{
    public class HomeBrokerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>
    {
        public HomeBrokerDbContext(DbContextOptions<HomeBrokerDbContext> options) : base(options)
        {
        }

        public virtual DbSet<PropertyListing> PropertyListings { get; set; }
        public virtual DbSet<CommissionConfiguration> CommissionConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PropertyListing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EstimatedCommission).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<CommissionConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MinPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)");
            });

            modelBuilder.SeedUsers();
            modelBuilder.SeedCommissionConfigurations();

        }
    }
}
