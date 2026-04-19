using HomeBroker.Infrastructure.Identity;
using HouseBroker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeBroker.Infrastructure
{
    public static class SeedData
    {
        public static void SeedUsers(this ModelBuilder builder)
        {
            //seed broker
            var broker = new ApplicationUser
            {
                Id = 1,
                FullName = "Manish Neupane",
                UserName = "manish@broker.com",
                NormalizedUserName = "MANISH@BROKER.COM",
                Email = "manish@broker.com",
                EmailConfirmed = true,
                NormalizedEmail = "MANISH@BROKER.COM",
                SecurityStamp = "d290f1ee-6c54-4b01-90e6-d701748f0851",
                ConcurrencyStamp = "12345678-1234-1234-1234-123456789012",
                PasswordHash = "AQAAAAIAAYagAAAAEBXKGYLCdKl8K1VvoL8Hd9eCYpUnOX6nKDlXt2d8J0j9HqHZdTmYVFGEq1B/Aq7LMw==" // Admin#123@,
            };

            //Seed houseSeeker User
            var houseSeeker = new ApplicationUser
            {
                Id = 2,
                FullName = "rohan@houseseeker.com",
                UserName = "rohan@houseseeker.com",
                NormalizedUserName = "ROHAN@HOUSESEEKER.COM",
                Email = "rohan@houseseeker.com",
                EmailConfirmed = true,
                NormalizedEmail = "ROHAN@HOUSESEEKER.COM",
                SecurityStamp = "d290f1ee-6c54-4b01-90e6-d701748f0851",
                ConcurrencyStamp = "12345678-1234-1234-1234-123456789012",
                PasswordHash = "AQAAAAIAAYagAAAAEBXKGYLCdKl8K1VvoL8Hd9eCYpUnOX6nKDlXt2d8J0j9HqHZdTmYVFGEq1B/Aq7LMw==" // Admin#123@,
            };

            var admin = new ApplicationUser
            {
                Id = 3,
                FullName = "Admin User",
                UserName = "user@admin.com",
                NormalizedUserName = "USER@ADMIN.COM",
                Email = "user@admin.com",
                EmailConfirmed = true,
                NormalizedEmail = "USER@ADMIN.COM",
                SecurityStamp = "d390f1ee-6c54-4b01-90e6-d701748f0851",
                ConcurrencyStamp = "12345668-1234-1234-1234-123456789012",
                PasswordHash = "AQAAAAIAAYagAAAAEBXKGYLCdKl8K1VvoL8Hd9eCYpUnOX6nKDlXt2d8J0j9HqHZdTmYVFGEq1B/Aq7LMw==" // Admin#123@,
            };

            builder.Entity<ApplicationUser>().HasData(broker, houseSeeker, admin);

            builder.Entity<IdentityRole<long>>().HasData(
                   new IdentityRole<long>
                   {
                       Id = 1,
                       Name = UserRole.Broker.ToString(),
                       NormalizedName = UserRole.Broker.ToString().ToUpper(),
                       ConcurrencyStamp = "ffdb8934-39d5-4893-b016-b1b3008fa8c8"
                   },
                   new IdentityRole<long>
                   {
                       Id = 2,
                       Name = UserRole.HouseSeeker.ToString(),
                       NormalizedName = UserRole.HouseSeeker.ToString().ToUpper(),
                       ConcurrencyStamp = "c368aed9-3dc1-4dec-b186-4087936a128b"
                   },
                   new IdentityRole<long>
                   {
                       Id = 3,
                       Name = UserRole.Admin.ToString(),
                       NormalizedName = UserRole.Admin.ToString().ToUpper(),
                       ConcurrencyStamp = "c368aed9-3dc1-4dec-b186-4087936a128C"
                   }
               );

            builder.Entity<IdentityUserRole<long>>().HasData(
                new IdentityUserRole<long>
                {
                    RoleId = 1,
                    UserId = 1
                },
                new IdentityUserRole<long>
                {
                    RoleId = 2,
                    UserId = 2
                },
                new IdentityUserRole<long>
                {
                    RoleId = 3,
                    UserId = 3
                }
            );

        }

        public static void SeedCommissionConfigurations(this ModelBuilder builder)
        {
            // Default tiers matching assignment spec:
            //   price < 50,00,000          → 2%
            //   50,00,000 to 1 crore       → 1.75%
            //   > 1 crore                  → 1.5%
            //
            // These are the STARTING values. Brokers/admins can change them via
            // inserting new rows in the CommissionConfiguration table, and the system will use the latest configuration.
            // The CommissionService reads from DB (with caching), so changes take effect
            // within the cache TTL (10 minutes) without redeployment.

            builder.Entity<CommissionConfiguration>().HasData(
                new CommissionConfiguration
                {
                    Id = 1,
                    MinPrice = 0m,
                    MaxPrice = 4_999_999.99m,   // Up to ₹49,99,999 → 2%
                    Percentage = 2m
                },
                new CommissionConfiguration
                {
                    Id = 2,
                    MinPrice = 5_000_000m,       // ₹50,00,000
                    MaxPrice = 10_000_000m,      // ₹1,00,00,000 (1 crore)
                    Percentage = 1.75m
                },
                new CommissionConfiguration
                {
                    Id = 3,
                    MinPrice = 10_000_000.01m,  // Above 1 crore
                    MaxPrice = null,             // No upper bound
                    Percentage = 1.5m
                }
            );
        }
    }
}