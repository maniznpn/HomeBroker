using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IUnitOfWork;
using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.Services.CommissionService;
using HouseBroker.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace HomeBroker.Tests.Services
{
    /// <summary>
    /// Unit tests for CommissionService.
    /// Tests commission calculation logic including:
    /// - Percentage-based commission calculation
    /// - Tier-based pricing
    /// - Configuration retrieval and caching
    /// - Edge cases (zero price, very high price)
    /// </summary>
    public class CommissionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICommissionConfigurationRepository> _mockRepository;
        private readonly CommissionService _service;
        private readonly IMemoryCache _memoryCache;



        public CommissionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepository = new Mock<ICommissionConfigurationRepository>();

            _mockUnitOfWork
                .Setup(u => u.CommissionConfigurationRepository)
                .Returns(_mockRepository.Object);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _service = new CommissionService(_mockUnitOfWork.Object,_memoryCache);

        }

        

        [Fact]
        public async Task CalculateCommissionAsync_WithDifferentTiers_ReturnsCorrectCommission()
        {
            // Arrange - Different commission tiers based on price range
            var configs = new List<CommissionConfiguration>
            {
                new CommissionConfiguration
                {
                    Id = 1,
                    MinPrice = 0,
                    MaxPrice = 100000,
                    Percentage = 3.0m
                },
                new CommissionConfiguration
                {
                    Id = 2,
                    MinPrice = 100001,
                    MaxPrice = 500000,
                    Percentage = 2.5m
                },
                new CommissionConfiguration
                {
                    Id = 3,
                    MinPrice = 500001,
                    MaxPrice = 999999999,
                    Percentage = 2.0m
                }
            };

            // Test price in "Premium Commission" tier
            decimal priceInPremiumTier = 250000;
            decimal expectedCommission = 6250; // 2.5% of 250000

            var mockQuery = configs.AsQueryable();

            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act
            var result = await _service.CalculateCommissionAsync(priceInPremiumTier);

            // Assert
            Assert.Equal(expectedCommission, result);
        }

        [Fact]
        public async Task CalculateCommissionAsync_WithZeroPrice_ReturnsZeroCommission()
        {
            // Arrange
            decimal price = 0;
            decimal expectedCommission = 0;

            var config = new CommissionConfiguration
            {
                Id = 1,
                MinPrice = 0,
                MaxPrice = 999999999,
                Percentage = 2.5m
            };

            var mockQuery = new List<CommissionConfiguration> { config }.AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act
            var result = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(expectedCommission, result);
        }

        [Fact]
        public async Task CalculateCommissionAsync_WithVeryHighPrice_CalculatesCorrectly()
        {
            // Arrange
            decimal price = 5000000; // 5 million
            decimal commissionPercentage = 1.5m; // Lower rate for luxury properties
            decimal expectedCommission = 75000; // 1.5% of 5,000,000

            var config = new CommissionConfiguration
            {
                Id = 1,
                MinPrice = 1000000,
                MaxPrice = null, // No upper limit
                Percentage = commissionPercentage
            };

            var mockQuery = new List<CommissionConfiguration> { config }.AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act
            var result = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(expectedCommission, result);
        }

        [Fact]
        public async Task CalculateCommissionAsync_WithNullMaxPrice_TreatsAsUnlimited()
        {
            // Arrange
            decimal price = 10000000; // Very high price

            var config = new CommissionConfiguration
            {
                Id = 1,
                MinPrice = 0,
                MaxPrice = null, // No upper limit
                Percentage = 2.0m
            };

            var mockQuery = new List<CommissionConfiguration> { config }.AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            decimal expectedCommission = 200000; // 2.0% of 10,000,000

            // Act
            var result = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(expectedCommission, result);
        }

        [Fact]
        public async Task CalculateCommissionAsync_WithPriceOutOfRange_ThrowsOrUsesDefaultConfig()
        {
            // Arrange
            decimal price = 1000000;

            // No configuration matches the price range
            var mockQuery = Enumerable.Empty<CommissionConfiguration>().AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act & Assert
            // The service should either throw an exception or use a default/fallback
            // Adjust this based on your actual implementation
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CalculateCommissionAsync(price));
        }

        

        [Fact]
        public async Task CalculateCommissionAsync_MultipleCallsSamePrice_CachesBehaviorCorrectly()
        {
            // Arrange
            decimal price = 150000;
            var config = new CommissionConfiguration
            {
                Id = 1,
                MinPrice = 0,
                MaxPrice = 999999999,
                Percentage = 2.0m
            };

            var mockQuery = new List<CommissionConfiguration> { config }.AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act - Call multiple times with same price
            var result1 = await _service.CalculateCommissionAsync(price);
            var result2 = await _service.CalculateCommissionAsync(price);
            var result3 = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
            Assert.Equal(3000, result1); // 2% of 150000
        }

        [Theory]
        [InlineData(50000, 1500)]      // 3% commission
        [InlineData(150000, 3750)]     // 2.5% commission
        [InlineData(500000, 10000)]    // 2% commission
        [InlineData(1000000, 15000)]   // 1.5% commission
        public async Task CalculateCommissionAsync_TheoryBasedPrices_CalculatesCorrectly(decimal price, decimal expectedCommission)
        {
            // Arrange
            var configs = new List<CommissionConfiguration>
            {
                new CommissionConfiguration
                {
                    Id = 1,
                    MinPrice = 0,
                    MaxPrice = 100000,
                    Percentage = 3.0m
                },
                new CommissionConfiguration
                {
                    Id = 2,
                    MinPrice = 100001,
                    MaxPrice = 300000,
                    Percentage = 2.5m
                },
                new CommissionConfiguration
                {
                    Id = 3,
                    MinPrice = 300001,
                    MaxPrice = 600000,
                    Percentage = 2.0m
                },
                new CommissionConfiguration
                {
                    Id = 4,
                    MinPrice = 600001,
                    MaxPrice = null,
                    Percentage = 1.5m
                }
            };

            var mockQuery = configs.AsQueryable();

            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act
            var result = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(expectedCommission, result);
        }

        [Fact]
        public async Task CalculateCommissionAsync_WithMinPriceOnly_AcceptsAllPricesAboveMin()
        {
            // Arrange - Configuration with only MinPrice set, no MaxPrice limit
            var config = new CommissionConfiguration
            {
                Id = 1,
                MinPrice = 50000,
                MaxPrice = null, // Unlimited upper bound
                Percentage = 2.0m
            };

            decimal price = 5000000; // Far above minimum
            decimal expectedCommission = 100000; // 2% of 5,000,000

            var mockQuery = new List<CommissionConfiguration> { config }.AsQueryable();
            _mockRepository
                .Setup(r => r.GetAllAsync(It.IsAny<bool>()))
                .Returns(mockQuery);
            _mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<CommissionConfiguration, bool>>>(), false))
                .Returns(mockQuery);

            // Act
            var result = await _service.CalculateCommissionAsync(price);

            // Assert
            Assert.Equal(expectedCommission, result);
        }
    }
}
