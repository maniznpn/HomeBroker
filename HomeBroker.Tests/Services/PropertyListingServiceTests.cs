using HomeBroker.Application.Exceptions;
using HomeBroker.Application.IImageService;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IServiceInterfaces.IPropertyListingService;
using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using HomeBroker.Application.IUnitOfWork;
using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.Identity;
using HomeBroker.Infrastructure.Services.PropertyListingService;
using HouseBroker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace HomeBroker.Tests.Services
{
    /// <summary>
    /// Unit tests for PropertyListingService.
    /// Tests CRUD operations, caching, and commission integration.
    /// </summary>
    public class PropertyListingServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICommissionService> _mockCommissionService;
        private readonly Mock<IImageService> _mockImageService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IUserMeta> _mockUserMeta;
        private readonly IMemoryCache _memoryCache;
        private readonly PropertyListingService _service;

        public PropertyListingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCommissionService = new Mock<ICommissionService>();
            _mockImageService = new Mock<IImageService>();

            // Mock UserManager with null Store (for testing purposes)
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _mockUserMeta = new Mock<IUserMeta>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _service = new PropertyListingService(
                _mockCommissionService.Object,
                _mockImageService.Object,
                _memoryCache,
                _mockUserManager.Object,
                _mockUserMeta.Object,
                _mockUnitOfWork.Object);
        }


        [Fact]
        public async Task GetListingByIdAsync_WithInvalidId_ThrowsException()
        {
            // Arrange
            long invalidId = 99999;
            long brokerId = 1;
            var mockRepository = new Mock<IPropertyListingRepository>();
            mockRepository
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<PropertyListing, bool>>>(), false))
                .Returns(Enumerable.Empty<PropertyListing>().AsQueryable());

            _mockUnitOfWork.Setup(u => u.PropertyListingRepository).Returns(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _service.GetListingByIdAsync(invalidId, brokerId));
        }



        [Fact]
        public async Task UpdateListingAsync_BrokerMismatch_ThrowsUnauthorizedException()
        {
            // Arrange
            long listingId = 11;
            long ownerId = 1;
            long hackerId = 2;
            var listing = new PropertyListing { Id = listingId, BrokerId = ownerId };

            var mockRepo = new Mock<IPropertyListingRepository>();
            mockRepo.Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PropertyListing, bool>>>(), false))
                    .Returns(new List<PropertyListing> { listing }.AsQueryable());
            _mockUnitOfWork.Setup(u => u.PropertyListingRepository).Returns(mockRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateListingAsync(listingId, new UpdatePropertyListingRequest(), hackerId));
        }

        [Fact]
        public async Task GetListingByIdAsync_ReturnsCachedItem_OnSecondCall()
        {
            // Arrange
            long id = 50;
            var listing = new PropertyListing { Id = id, BrokerId = 1 };
            var broker = new ApplicationUser { Id = 1 };

            _mockUnitOfWork.Setup(u => u.PropertyListingRepository.GetByIdAsync(id)).ReturnsAsync(listing);
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(broker);

            // Act
            await _service.GetListingByIdAsync(id); // First call - hits DB
            await _service.GetListingByIdAsync(id); // Second call - should hit Cache

            // Assert
            _mockUnitOfWork.Verify(u => u.PropertyListingRepository.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task SearchListingsAsync_FiltersCorrectly_AndHydratesBrokerData()
        {
            // Arrange
            var filters = new PropertyListingSearchFilters
            {
                PageNumber = 1,
                PageSize = 10,
                Location = "London"
            };

            var listings = new List<PropertyListing>
    {
        new PropertyListing { Id = 1, BrokerId = 100, Location = "London", IsDeleted = false, CreatedOn = DateTime.UtcNow }
    }.AsQueryable();

            var broker = new ApplicationUser { Id = 100, FullName = "Agent Smith", Email = "smith@matrix.com" };

            var mockRepo = new Mock<IPropertyListingRepository>();

            // Pattern match: Setup FindByCondition to return our queryable list
            mockRepo.Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PropertyListing, bool>>>(), It.IsAny<bool>()))
                    .Returns(listings);

            _mockUnitOfWork.Setup(u => u.PropertyListingRepository).Returns(mockRepo.Object);

            // Identity UserManager uses strings for IDs in the FindByIdAsync method
            _mockUserManager.Setup(u => u.FindByIdAsync("100")).ReturnsAsync(broker);
            _mockUserMeta.Setup(m => m.GetUserId()).Returns(100);

            // Act
            var result = await _service.SearchListingsAsync(filters);

            // Assert
            Assert.NotNull(result);
            var item = result.First();
            Assert.Equal("Agent Smith", item.BrokerName);
            Assert.Equal("London", item.Location);
            _mockUserManager.Verify(u => u.FindByIdAsync("100"), Times.Once);
        }
    }
}
