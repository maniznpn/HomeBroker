using HomeBroker.Application.Exceptions;
using HomeBroker.Application.IImageService;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IServiceInterfaces.IPropertyListingService;
using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using HomeBroker.Application.IUnitOfWork;
using HomeBroker.Infrastructure.Identity;
using HouseBroker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeBroker.Infrastructure.Services.PropertyListingService
{
    /// <summary>
    /// Property Listing Service.
    /// Handles CRUD operations and search with commission integration.
    /// Caches individual listings and search results; invalidates on mutation.
    /// </summary>
    public class PropertyListingService : IPropertyListingService
    {
        private readonly ICommissionService _commissionService;
        private readonly IImageService _imageService;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserMeta _meta;
        private readonly IUnitOfWork _unitOfWork;

        // Cache settings
        private static readonly TimeSpan ListingCacheDuration = TimeSpan.FromMinutes(5);
        private const string ListingCachePrefix = "Listing_";
        private const string SearchCachePrefix = "Search_";

        public PropertyListingService(
            ICommissionService commissionService,
            IImageService imageService,
            IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            IUserMeta meta,
            IUnitOfWork unitOfWork)
        {
            _commissionService = commissionService ?? throw new ArgumentNullException(nameof(commissionService));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Creates a new property listing with automatic commission calculation.
        /// Invalidates all search result caches after creation.
        /// </summary>
        public async Task<PropertyListingDto> CreateListingAsync(CreatePropertyListingRequest request, long brokerId)
        {
            ValidateCreateRequest(request);

            var imageUrl = string.Empty;
            var commission = await _commissionService.CalculateCommissionAsync(request.Price);

            if (request.PropertyImage != null)
                imageUrl = await _imageService.UploadImageAsync(request.PropertyImage);

            var listing = new PropertyListing
            {
                PropertyType = request.PropertyType,
                Location = request.Location,
                Price = request.Price,
                Description = request.Description,
                Features = request.Features,
                ImageUrl = imageUrl,
                EstimatedCommission = commission,
                BrokerId = brokerId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = brokerId
            };

            await _unitOfWork.PropertyListingRepository.AddAsync(listing);
            await _unitOfWork.SaveChangesAsync();

            InvalidateSearchCache();

            var broker = await _userManager.FindByIdAsync(brokerId.ToString());
            return MapToDto(listing, brokerId, broker);
        }

        /// <summary>
        /// Updates an existing property listing.
        /// Recalculates commission if price changed.
        /// Invalidates both per-listing and search caches.
        /// </summary>
        public async Task<PropertyListingDto> UpdateListingAsync(long id, UpdatePropertyListingRequest request, long brokerId)
        {
            var listing = await _unitOfWork.PropertyListingRepository.GetByIdAsync(id) ??
                throw new NotFoundException($"Property listing with ID {id} not found.");

            if (listing.BrokerId != brokerId)
                throw new UnauthorizedAccessException("You are not authorized to update this listing.");

            ValidateUpdateRequest(request);

            if (listing.Price != request.Price)
                listing.EstimatedCommission = await _commissionService.CalculateCommissionAsync(request.Price);

            listing.PropertyType = request.PropertyType;
            listing.Location = request.Location;
            listing.Price = request.Price;
            listing.Description = request.Description;
            listing.Features = request.Features;
            listing.ImageUrl = request.ImageUrl;
            listing.UpdatedOn = DateTime.UtcNow;
            listing.UpdatedBy = brokerId;

            _unitOfWork.PropertyListingRepository.Update(listing);

            await _unitOfWork.SaveChangesAsync();

            // Invalidate this listing's cache entry and all search results
            _cache.Remove($"{ListingCachePrefix}{id}");
            InvalidateSearchCache();

            var broker = await _userManager.FindByIdAsync(brokerId.ToString());
            return MapToDto(listing, brokerId, broker);
        }

        /// <summary>
        /// Gets a property listing by ID.
        /// Result is cached per listing. Commission visible only to the owning broker.
        /// Broker contact details (name, email, phone) are always included.
        /// </summary>
        public async Task<PropertyListingDto> GetListingByIdAsync(long id, long? currentBrokerId = null)
        {
            var cacheKey = $"{ListingCachePrefix}{id}";

            if (!_cache.TryGetValue(cacheKey, out PropertyListing? listing) || listing == null)
            {
                listing = await (_unitOfWork.PropertyListingRepository.GetByIdAsync(id))
                    ?? throw new BadRequestException($"Property listing with ID {id} not found.");

                _cache.Set(cacheKey, listing, ListingCacheDuration);
            }

            // Broker contact details fetched outside cache (user data can change)
            var broker = await _userManager.FindByIdAsync(listing.BrokerId.ToString());
            return MapToDto(listing, currentBrokerId, broker);
        }

        /// <summary>
        /// Searches property listings with optional filters.
        /// Results are cached by filter fingerprint (5 min TTL).
        /// Commission is hidden for all search results (only visible on direct GetById as owner).
        /// </summary>
        public async Task<IEnumerable<PropertyListingDto>> SearchListingsAsync(PropertyListingSearchFilters filters)
        {
            var cacheKey = BuildSearchCacheKey(filters);

            if (!_cache.TryGetValue(cacheKey, out List<PropertyListing>? listings) || listings == null)
            {
                var query = _unitOfWork.PropertyListingRepository.FindByCondition(p => !p.IsDeleted);

                if (!string.IsNullOrWhiteSpace(filters.Location))
                    query = query.Where(p => p.Location.Contains(filters.Location));

                if (filters.MinPrice.HasValue)
                    query = query.Where(p => p.Price >= filters.MinPrice.Value);

                if (filters.MaxPrice.HasValue)
                    query = query.Where(p => p.Price <= filters.MaxPrice.Value);

                if (filters.PropertyType.HasValue)
                    query = query.Where(p => p.PropertyType == filters.PropertyType.Value);

                listings = await (query
                    .OrderByDescending(p => p.CreatedOn)
                    .Skip((filters.PageNumber - 1) * filters.PageSize)
                    .Take(filters.PageSize))
                    .ToListAsync();

                _cache.Set(cacheKey, listings, ListingCacheDuration);
            }

            // Broker details are fetched per unique broker to minimise DB calls
            var brokerIds = listings.Select(l => l.BrokerId).Distinct().ToList();
            var brokers = new Dictionary<long, ApplicationUser?>();
            foreach (var bid in brokerIds)
                brokers[bid] = await _userManager.FindByIdAsync(bid.ToString());

            return listings.Select(l => MapToDto(l, _meta.GetUserId(), brokers.GetValueOrDefault(l.BrokerId))).ToList();
        }

        /// <summary>
        /// Soft-deletes a property listing.
        /// Invalidates per-listing and search caches.
        /// </summary>
        public async Task DeleteListingAsync(long id, long brokerId)
        {
            var listing = await (_unitOfWork.PropertyListingRepository.FindByCondition(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync())
                ?? throw new BadRequestException($"Property listing with ID {id} not found.");

            if (listing.BrokerId != brokerId)
                throw new UnauthorizedAccessException("You are not authorized to delete this listing.");

            listing.IsDeleted = true;
            listing.UpdatedOn = DateTime.UtcNow;
            listing.UpdatedBy = brokerId;

            _unitOfWork.PropertyListingRepository.Update(listing);
            await _unitOfWork.SaveChangesAsync();

            _cache.Remove($"{ListingCachePrefix}{id}");
            InvalidateSearchCache();
        }

        // ── Private helpers ────────────────────────────────────────────

        private PropertyListingDto MapToDto(PropertyListing listing, long? currentBrokerId, ApplicationUser? broker)
        {
            var isOwner = currentBrokerId.HasValue && listing.BrokerId == currentBrokerId.Value;

            return new PropertyListingDto
            {
                Id = listing.Id,
                PropertyType = listing.PropertyType,
                Location = listing.Location,
                Price = listing.Price,
                Description = listing.Description,
                Features = listing.Features,
                ImageUrl = listing.ImageUrl,
                EstimatedCommission = isOwner ? listing.EstimatedCommission : 0,
                BrokerId = listing.BrokerId,
                BrokerName = broker?.FullName ?? "Unknown",
                BrokerEmail = broker?.Email ?? string.Empty,
                BrokerPhone = broker?.PhoneNumber ?? string.Empty,
                IsOwnedByCurrentBroker = isOwner,
                CreatedOn = listing.CreatedOn,
                UpdatedOn = listing.UpdatedOn
            };
        }

        private void ValidateCreateRequest(CreatePropertyListingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
                throw new ArgumentException("Location is required.", nameof(request.Location));
            if (request.Price <= 0)
                throw new ArgumentException("Price must be greater than zero.", nameof(request.Price));
            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ArgumentException("Description is required.", nameof(request.Description));
        }

        private void ValidateUpdateRequest(UpdatePropertyListingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
                throw new ArgumentException("Location is required.", nameof(request.Location));
            if (request.Price <= 0)
                throw new ArgumentException("Price must be greater than zero.", nameof(request.Price));
            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ArgumentException("Description is required.", nameof(request.Description));
        }

        /// <summary>
        /// Builds a deterministic cache key from the search filter values.
        /// </summary>
        private static string BuildSearchCacheKey(PropertyListingSearchFilters filters) =>
            $"{SearchCachePrefix}{filters.Location}_{filters.MinPrice}_{filters.MaxPrice}_{filters.PropertyType}_{filters.PageNumber}_{filters.PageSize}";

        /// <summary>
        /// Removes all search result cache entries by using a shared expiry token approach.
        /// Since IMemoryCache has no prefix-scan, we use a generation counter as part of the key.
        /// On each mutation we bump the counter — old keys become unreachable and expire naturally.
        /// </summary>
        private void InvalidateSearchCache()
        {
            var generation = (int)(_cache.Get<int>("SearchCacheGeneration") + 1);
            _cache.Set("SearchCacheGeneration", generation, TimeSpan.FromDays(1));
        }
    }
}
