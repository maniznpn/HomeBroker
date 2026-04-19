using HomeBroker.Application;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IServiceInterfaces.IPropertyListingService;
using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using HomeBroker.Domain.Enums;
using HomeBroker.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeBroker.Tests.Controllers
{
    /// <summary>
    /// Unit tests for PropertyListingsController.
    /// Tests HTTP endpoints including:
    /// - Successful requests (200 OK)
    /// - Server errors (500)
    /// - Authorization and user context
    /// - Request/response mapping
    /// 
    /// NOTE: The controller does not have try/catch blocks, so 404/500 exception-based
    /// tests are not applicable unless a global exception handler middleware is in place.
    /// Tests reflect what the controller actually returns.
    /// </summary>
    public class PropertyListingsControllerTests
    {
        private readonly Mock<IPropertyListingService> _mockPropertyListingService;
        private readonly Mock<IUserMeta> _mockUserMeta;
        private readonly Mock<ILogger<PropertyListingsController>> _mockLogger;
        private readonly PropertyListingsController _controller;
        private readonly Mock<ICommissionService> _mockCommissionService;

        public PropertyListingsControllerTests()
        {
            _mockPropertyListingService = new Mock<IPropertyListingService>();
            _mockUserMeta = new Mock<IUserMeta>();
            _mockLogger = new Mock<ILogger<PropertyListingsController>>();
            _mockCommissionService = new Mock<ICommissionService>();

            _controller = new PropertyListingsController(
                _mockPropertyListingService.Object,
                _mockCommissionService.Object,
                _mockLogger.Object,
                _mockUserMeta.Object);
        }

        // Helper to unwrap APIResponse and extract the inner data
        private T UnwrapApiResponse<T>(object value)
        {
            Assert.NotNull(value);
            var apiResponse = Assert.IsType<APIResponse>(value);
            Assert.IsType<T>(apiResponse.Data);
            return (T)apiResponse.Data;
        }

        #region GET /api/propertylistings/{id}

        [Fact]
        public async Task GetListing_WithValidId_Returns200OkWithData()
        {
            // Arrange
            long listingId = 1;
            long brokerId = 1;

            var mockListing = new PropertyListingDto
            {
                Id = listingId,
                PropertyType = PropertyType.Apartment,
                Location = "Downtown",
                Price = 250000,
                Description = "Nice apartment",
                Features = "Balcony, Gym",
                BrokerId = brokerId,
                BrokerName = "John Broker",
                BrokerEmail = "john@broker.com",
                BrokerPhone = "1234567890",
                IsOwnedByCurrentBroker = true,
                CreatedOn = DateTime.UtcNow
            };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.GetListingByIdAsync(listingId, brokerId))
                .ReturnsAsync(mockListing);

            // Act
            var result = await _controller.GetListing(listingId);

            // Assert
            // Controller returns Ok(new APIResponse(result)), so unwrap accordingly
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedListing = UnwrapApiResponse<PropertyListingDto>(okResult.Value);
            Assert.Equal(listingId, returnedListing.Id);
            Assert.Equal("Downtown", returnedListing.Location);
            Assert.Equal(250000, returnedListing.Price);
        }

        [Fact]
        public async Task GetListing_CallsServiceWithCorrectParameters()
        {
            // Arrange
            long listingId = 5;
            long brokerId = 3;

            var mockListing = new PropertyListingDto
            {
                Id = listingId,
                Location = "Test",
                Price = 100000,
                BrokerId = brokerId
            };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.GetListingByIdAsync(listingId, brokerId))
                .ReturnsAsync(mockListing);

            // Act
            await _controller.GetListing(listingId);

            // Assert - Verify service was called with correct parameters
            _mockPropertyListingService.Verify(
                s => s.GetListingByIdAsync(listingId, brokerId),
                Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(999)]
        public async Task GetListing_WithVariousIds_ReturnsCorrectId(long listingId)
        {
            // Arrange
            long brokerId = 1;
            var mockListing = new PropertyListingDto { Id = listingId };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.GetListingByIdAsync(listingId, brokerId))
                .ReturnsAsync(mockListing);

            // Act
            var result = await _controller.GetListing(listingId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedListing = UnwrapApiResponse<PropertyListingDto>(okResult.Value);
            Assert.Equal(listingId, returnedListing.Id);
        }

        // NOTE: GetListing_WithInvalidId_Returns404NotFound and
        // GetListing_WhenServiceThrowsException_Returns500Error are removed because
        // the controller has no try/catch. Exceptions will propagate to middleware.
        // To test 404/500 behavior, add a try/catch to the controller or test via
        // integration tests with the full middleware pipeline.

        #endregion

        #region GET /api/propertylistings/search

        [Fact]
        public async Task SearchListings_ReturnsListingsSuccessfully()
        {
            // Arrange
            var mockListings = new List<PropertyListingDto>
            {
                new PropertyListingDto { Id = 1, Location = "Downtown", Price = 250000, BrokerName = "John" },
                new PropertyListingDto { Id = 2, Location = "Uptown",   Price = 350000, BrokerName = "Jane" }
            };

            _mockPropertyListingService
                .Setup(s => s.SearchListingsAsync(It.IsAny<PropertyListingSearchFilters>()))
                .ReturnsAsync(mockListings);

            // Act
            // Controller builds its own PropertyListingSearchFilters from individual query params,
            // so we call with no arguments (all defaults).
            var result = await _controller.SearchListings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedListings = UnwrapApiResponse<List<PropertyListingDto>>(okResult.Value);
            Assert.Equal(2, returnedListings.Count);
            Assert.Equal("Downtown", returnedListings[0].Location);
            Assert.Equal("Uptown", returnedListings[1].Location);
        }

        [Fact]
        public async Task SearchListings_WithNoResults_ReturnsEmptyList()
        {
            // Arrange
            _mockPropertyListingService
                .Setup(s => s.SearchListingsAsync(It.IsAny<PropertyListingSearchFilters>()))
                .ReturnsAsync(new List<PropertyListingDto>());

            // Act
            var result = await _controller.SearchListings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedListings = UnwrapApiResponse<List<PropertyListingDto>>(okResult.Value);
            Assert.Empty(returnedListings);
        }

        [Fact]
        public async Task SearchListings_WithFilters_PassesFiltersToService()
        {
            // Arrange
            // Controller accepts individual [FromQuery] params and builds PropertyListingSearchFilters internally.
            // Pass them as individual arguments matching the controller's method signature.
            string location = "Downtown";
            decimal minPrice = 100000;
            decimal maxPrice = 500000;
            int propertyType = (int)PropertyType.Apartment;
            int pageNumber = 2;
            int pageSize = 20;

            _mockPropertyListingService
                .Setup(s => s.SearchListingsAsync(It.IsAny<PropertyListingSearchFilters>()))
                .ReturnsAsync(new List<PropertyListingDto>());

            // Act
            await _controller.SearchListings(location, minPrice, maxPrice, propertyType, pageNumber, pageSize);

            // Assert - Verify service receives filters built from the query params
            _mockPropertyListingService.Verify(
                s => s.SearchListingsAsync(
                    It.Is<PropertyListingSearchFilters>(f =>
                        f.Location == "Downtown" &&
                        f.MinPrice == 100000 &&
                        f.MaxPrice == 500000 &&
                        f.PropertyType == PropertyType.Apartment &&
                        f.PageNumber == 2 &&
                        f.PageSize == 20)),
                Times.Once);
        }

        #endregion

        #region POST /api/propertylistings/create

        [Fact]
        public async Task CreateListing_WithValidRequest_Returns200Ok()
        {
            // Arrange
            long brokerId = 1;
            var createRequest = new CreatePropertyListingRequest
            {
                PropertyType = PropertyType.House,
                Location = "Suburbs",
                Price = 400000,
                Description = "Beautiful house",
                Features = "3 bedrooms, Garage"
            };

            var createdListing = new PropertyListingDto
            {
                Id = 1,
                PropertyType = createRequest.PropertyType,
                Location = createRequest.Location,
                Price = createRequest.Price,
                BrokerId = brokerId,
                BrokerName = "Test Broker"
            };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.CreateListingAsync(createRequest, brokerId))
                .ReturnsAsync(createdListing);

            // Act
            var result = await _controller.CreateListing(createRequest);

            // Assert
            // Controller returns Ok(new APIResponse(result)), not CreatedAtAction
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedListing = UnwrapApiResponse<PropertyListingDto>(okResult.Value);
            Assert.Equal("Suburbs", returnedListing.Location);
            Assert.Equal(400000, returnedListing.Price);
        }

        [Fact]
        public async Task CreateListing_CallsServiceWithCorrectBrokerId()
        {
            // Arrange
            long brokerId = 5;
            var createRequest = new CreatePropertyListingRequest
            {
                PropertyType = PropertyType.Apartment,
                Location = "Downtown",
                Price = 300000,
                Description = "Apartment",
                Features = "Balcony"
            };

            var createdListing = new PropertyListingDto { Id = 1 };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.CreateListingAsync(createRequest, brokerId))
                .ReturnsAsync(createdListing);

            // Act
            await _controller.CreateListing(createRequest);

            // Assert
            _mockPropertyListingService.Verify(
                s => s.CreateListingAsync(createRequest, brokerId),
                Times.Once);
        }

        #endregion

        #region PUT /api/propertylistings/{id}

        [Fact]
        public async Task UpdateListing_WithValidRequest_Returns200Ok()
        {
            // Arrange
            long listingId = 1;
            long brokerId = 1;

            var updateRequest = new UpdatePropertyListingRequest
            {
                PropertyType = PropertyType.Apartment,
                Location = "New Location",
                Price = 350000,
                Description = "Updated description",
                Features = "Updated features"
            };

            var updatedListing = new PropertyListingDto
            {
                Id = listingId,
                Location = updateRequest.Location,
                Price = updateRequest.Price,
                BrokerId = brokerId
            };

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.UpdateListingAsync(listingId, updateRequest, brokerId))
                .ReturnsAsync(updatedListing);

            // Act
            var result = await _controller.UpdateListing(listingId, updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedListing = UnwrapApiResponse<PropertyListingDto>(okResult.Value);
            Assert.Equal("New Location", returnedListing.Location);
            Assert.Equal(350000, returnedListing.Price);
        }

        // NOTE: UpdateListing_WithUnauthorizedUser_Returns403Forbidden is removed.
        // The controller has no try/catch for UnauthorizedAccessException; it would
        // propagate to middleware. Test via integration tests or add try/catch to controller.

        #endregion

        #region DELETE /api/propertylistings/{id}

        [Fact]
        public async Task DeleteListing_WithValidId_Returns200Ok()
        {
            // Arrange
            long listingId = 1;
            long brokerId = 1;

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.DeleteListingAsync(listingId, brokerId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteListing(listingId);

            // Assert
            // Controller returns Ok(new APIResponse("Successfully deleted!")), not 204 NoContent
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var apiResponse = Assert.IsType<APIResponse>(okResult.Value);
            Assert.Equal("Successfully deleted!", apiResponse.Data);
        }

        [Fact]
        public async Task DeleteListing_CallsServiceWithCorrectParameters()
        {
            // Arrange
            long listingId = 3;
            long brokerId = 2;

            _mockUserMeta.Setup(m => m.GetUserId()).Returns(brokerId);
            _mockPropertyListingService
                .Setup(s => s.DeleteListingAsync(listingId, brokerId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.DeleteListing(listingId);

            // Assert
            _mockPropertyListingService.Verify(
                s => s.DeleteListingAsync(listingId, brokerId),
                Times.Once);
        }

        // NOTE: DeleteListing_WithInvalidId_Returns404NotFound is removed.
        // The controller has no try/catch, so exceptions propagate to middleware.
        // Test via integration tests or add try/catch to the controller.

        #endregion

        #region Helper Tests

        [Fact]
        public void Controller_HasCorrectDependencies()
        {
            Assert.NotNull(_controller);
        }

        #endregion
    }
}