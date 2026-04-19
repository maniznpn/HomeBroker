using HomeBroker.Application;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IServiceInterfaces.IPropertyListingService;
using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using HomeBroker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBroker.WebApi.Controllers
{
    /// <summary>
    /// API controller for managing property listings.
    /// Provides endpoints for creating, reading, updating, and searching property listings.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyListingsController : ControllerBase
    {
        private readonly IPropertyListingService _propertyListingService;
        private readonly ICommissionService _commissionService;
        private readonly ILogger<PropertyListingsController> _logger;
        private readonly IUserMeta _meta;

        public PropertyListingsController(
            IPropertyListingService propertyListingService,
            ICommissionService commissionService,
            ILogger<PropertyListingsController> logger,
            IUserMeta meta)
        {
            _propertyListingService = propertyListingService ?? throw new ArgumentNullException(nameof(propertyListingService));
            _commissionService = commissionService ?? throw new ArgumentNullException(nameof(commissionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meta = meta;
        }

        /// <summary>
        /// Creates a new property listing with automatic commission calculation.
        /// </summary>
        /// <param name="request">Property listing creation request</param>
        /// <returns>Created property listing with calculated commission</returns>
        [HttpPost]
        [Authorize(Policy = "BrokerPolicy")]
        public async Task<ActionResult<PropertyListingDto>> CreateListing(CreatePropertyListingRequest request)
        {

            var brokerId = _meta.GetUserId();
            var result = await _propertyListingService.CreateListingAsync(request, brokerId);
            _logger.LogInformation($"Property listing created with ID {result.Id} by broker {brokerId}");
            return Ok(new APIResponse(result));

        }

        /// <summary>
        /// Retrieves a property listing by ID.
        /// Commission is visible only to the broker who owns the listing.
        /// </summary>
        /// <param name="id">Property listing ID</param>
        /// <returns>Property listing details</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PropertyListingDto>> GetListing(long id)
        {
            var brokerId = _meta.GetUserId();
            var result = await _propertyListingService.GetListingByIdAsync(id, brokerId);
            return Ok(new APIResponse(result));

        }

        /// <summary>
        /// Searches property listings with optional filters.
        /// Supports filtering by location, price range, and property type.
        /// Results are paginated.
        /// </summary>
        /// <param name="location">Filter by location (partial match)</param>
        /// <param name="minPrice">Minimum price filter</param>
        /// <param name="maxPrice">Maximum price filter</param>
        /// <param name="propertyType">Filter by property type</param>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Page size for pagination (default: 10, max: 100)</param>
        /// <returns>Paginated list of property listings</returns>
        [HttpGet("search")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PropertyListingDto>>> SearchListings(
            [FromQuery] string location = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? propertyType = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var filters = new PropertyListingSearchFilters
            {
                Location = location,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PropertyType = propertyType.HasValue ? (PropertyType)propertyType.Value : null,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var results = await _propertyListingService.SearchListingsAsync(filters);
            return Ok(new APIResponse(results));
        }

        /// <summary>
        /// Updates a property listing.
        /// Commission is recalculated if the price is changed.
        /// Only the broker who owns the listing can update it.
        /// </summary>
        /// <param name="id">Property listing ID</param>
        /// <param name="request">Property listing update request</param>
        /// <returns>Updated property listing</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "BrokerPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PropertyListingDto>> UpdateListing(long id, [FromBody] UpdatePropertyListingRequest request)
        {
            var brokerId = _meta.GetUserId();
            var result = await _propertyListingService.UpdateListingAsync(id, request, brokerId);
            _logger.LogInformation($"Property listing {id} updated by broker {brokerId}");
            return Ok(new APIResponse(result));

        }

        /// <summary>
        /// Deletes a property listing (soft delete).
        /// Only the broker who owns the listing can delete it.
        /// </summary>
        /// <param name="id">Property listing ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "BrokerPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteListing(long id)
        {
            var brokerId = _meta.GetUserId();
            await _propertyListingService.DeleteListingAsync(id, brokerId);
            _logger.LogInformation($"Property listing {id} deleted by broker {brokerId}");
            return Ok(new APIResponse("Successfully deleted!"));

        }

        /// <summary>
        /// Gets all active commission configurations.
        /// </summary>
        /// <returns>List of commission tiers</returns>
        [HttpGet("commissions/configurations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = "BrokerPolicy")]
        public async Task<ActionResult<IEnumerable<CommissionConfigurationDto>>> GetCommissionConfigurations()
        {
            var result = await _commissionService.GetCommissionConfigurationsAsync();
            return Ok(new APIResponse(result));

        }

        /// <summary>
        /// Calculates the estimated commission for a given price.
        /// This can be used to preview commission before creating a listing.
        /// </summary>
        /// <param name="price">Property price in rupees</param>
        /// <returns>Calculated commission amount</returns>
        [HttpGet("commissions/calculate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Policy = "BrokerPolicy")]
        public async Task<ActionResult<decimal>> CalculateCommission([FromQuery] decimal price)
        {
            if (price < 0)
            {
                return BadRequest(new { error = "Price cannot be negative" });
            }

            var commission = await _commissionService.CalculateCommissionAsync(price);
            return Ok(new { price, commission });

        }
    }
}
