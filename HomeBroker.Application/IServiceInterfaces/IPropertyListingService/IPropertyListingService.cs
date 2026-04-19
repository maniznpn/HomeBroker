using HomeBroker.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace HomeBroker.Application.IServiceInterfaces.IPropertyListingService
{
    /// <summary>
    /// Interface for property listing management service.
    /// Handles CRUD operations and search for property listings.
    /// </summary>
    public interface IPropertyListingService
    {
        /// <summary>
        /// Creates a new property listing with automatic commission calculation.
        /// </summary>
        /// <param name="request">Property listing creation request</param>
        /// <param name="brokerId">The ID of the broker creating the listing</param>
        /// <returns>The created property listing</returns>
        Task<PropertyListingDto> CreateListingAsync(CreatePropertyListingRequest request, long brokerId);

        /// <summary>
        /// Updates an existing property listing.
        /// </summary>
        /// <param name="id">The property listing ID</param>
        /// <param name="request">Property listing update request</param>
        /// <param name="brokerId">The ID of the broker (for authorization)</param>
        /// <returns>The updated property listing</returns>
        Task<PropertyListingDto> UpdateListingAsync(long id, UpdatePropertyListingRequest request, long brokerId);

        /// <summary>
        /// Gets a property listing by ID.
        /// Commission is only visible to the listing's broker.
        /// </summary>
        /// <param name="id">The property listing ID</param>
        /// <param name="currentBrokerId">The ID of the current broker (for authorization)</param>
        /// <returns>The property listing</returns>
        Task<PropertyListingDto> GetListingByIdAsync(long id, long? currentBrokerId = null);

        /// <summary>
        /// Gets all property listings with optional filters.
        /// </summary>
        /// <param name="filters">Search filters</param>
        /// <returns>List of property listings</returns>
        Task<IEnumerable<PropertyListingDto>> SearchListingsAsync(PropertyListingSearchFilters filters);

        /// <summary>
        /// Deletes a property listing (soft delete).
        /// </summary>
        /// <param name="id">The property listing ID</param>
        /// <param name="brokerId">The ID of the broker (for authorization)</param>
        Task DeleteListingAsync(long id, long brokerId);
    }

    /// <summary>
    /// Request DTO for creating a property listing
    /// </summary>
    public class CreatePropertyListingRequest
    {
        public PropertyType PropertyType { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }
        public IFormFile? PropertyImage { get; set; }
    }

    /// <summary>
    /// Request DTO for updating a property listing
    /// </summary>
    public class UpdatePropertyListingRequest
    {
        public PropertyType PropertyType { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }
        public string ImageUrl { get; set; }
    }

    /// <summary>
    /// Response DTO for property listings
    /// </summary>
    public class PropertyListingDto
    {
        public long Id { get; set; }
        public PropertyType PropertyType { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }
        public string ImageUrl { get; set; }
        public decimal EstimatedCommission { get; set; }
        public long BrokerId { get; set; }
        public bool IsOwnedByCurrentBroker { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string BrokerName { get; set; }
        public string BrokerEmail { get; set; }
        public string BrokerPhone { get; set; }
    }

    /// <summary>
    /// Search filters for property listings
    /// </summary>
    public class PropertyListingSearchFilters
    {
        public string Location { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public PropertyType? PropertyType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
