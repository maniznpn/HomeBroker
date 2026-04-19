using HomeBroker.Application.Exceptions;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using HomeBroker.Application.IUnitOfWork;
using HouseBroker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeBroker.Infrastructure.Services.CommissionService
{
    public class CommissionService : ICommissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private const string ConfigsCacheKey = "CommissionConfigurations";

        public CommissionService(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Calculates commission by reading tiers from DB (cached 10 min).
        /// </summary>
        public async Task<decimal> CalculateCommissionAsync(decimal price)
        {
            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            if (price == 0)
                return 0;

            var configs = await GetCachedConfigurationsAsync();

            if (!configs.Any())
                throw new InvalidOperationException("No commission configurations found in the database.");

            var matchingTier = configs
                .OrderBy(c => c.MinPrice)
                .FirstOrDefault(c =>
                    price >= c.MinPrice &&
                    (!c.MaxPrice.HasValue || price <= c.MaxPrice.Value));

            // Fallback to highest tier (catches prices above all defined ranges)
            matchingTier ??= configs.OrderByDescending(c => c.MinPrice).First();

            return Math.Round((price * matchingTier.Percentage) / 100, 2);
        }

        /// <summary>
        /// Gets all commission tiers ordered by MinPrice.
        /// </summary>
        public async Task<IEnumerable<CommissionConfigurationDto>> GetCommissionConfigurationsAsync()
        {
            var configs = await _unitOfWork.CommissionConfigurationRepository
                .GetAllAsync()
                .OrderBy(x => x.MinPrice)
                .ToListAsync();

            if (!configs.Any()) throw new BadRequestException("Commission information is empty. Please add commisions first!");

            return configs.Select(MapToDto);
        }

        /// <summary>
        /// Creates or updates a commission tier. Invalidates cache on success.
        /// </summary>
        public async Task<CommissionConfigurationDto> CreateOrUpdateConfigurationAsync(CommissionConfigurationDto configDto)
        {
            ValidateCommissionConfiguration(configDto);

            CommissionConfiguration config;

            if (configDto.Id > 0)
            {
                config = await _unitOfWork.CommissionConfigurationRepository.GetByIdAsync(configDto.Id)
                    ?? throw new BadRequestException($"Commission configuration with ID {configDto.Id} not found.");

                config.MinPrice = configDto.MinPrice;
                config.MaxPrice = configDto.MaxPrice;
                config.Percentage = configDto.Percentage;

                _unitOfWork.CommissionConfigurationRepository.Update(config);
            }
            else
            {
                config = new CommissionConfiguration
                {
                    MinPrice = configDto.MinPrice,
                    MaxPrice = configDto.MaxPrice,
                    Percentage = configDto.Percentage
                };

                await _unitOfWork.CommissionConfigurationRepository.AddAsync(config);
            }

            await _unitOfWork.SaveChangesAsync();

            _cache.Remove(ConfigsCacheKey); // new tier takes effect on next calculation

            return MapToDto(config);
        }

        /// <summary>
        /// Deletes a commission tier. Invalidates cache on success.
        /// </summary>
        public async Task DeleteConfigurationAsync(int configId)
        {
            await _unitOfWork.CommissionConfigurationRepository.DeleteAsync(configId);
            await _unitOfWork.SaveChangesAsync();

            _cache.Remove(ConfigsCacheKey);
        }

        // ── Private helpers ────────────────────────────────────────────

        private async Task<List<CommissionConfiguration>> GetCachedConfigurationsAsync()
        {
            if (_cache.TryGetValue(ConfigsCacheKey, out List<CommissionConfiguration>? cached))
                return cached!;

            var configs = _unitOfWork.CommissionConfigurationRepository
                .GetAllAsync()
                .OrderBy(c => c.MinPrice)
                .ToList();

            _cache.Set(ConfigsCacheKey, configs, TimeSpan.FromMinutes(10));
            return await Task.FromResult(configs);
        }

        private void ValidateCommissionConfiguration(CommissionConfigurationDto config)
        {
            if (config.MinPrice < 0)
                throw new ArgumentException("Minimum price cannot be negative.", nameof(config.MinPrice));

            if (config.MaxPrice.HasValue && config.MaxPrice < config.MinPrice)
                throw new ArgumentException("Maximum price cannot be less than minimum price.", nameof(config.MaxPrice));

            if (config.Percentage < 0 || config.Percentage > 100)
                throw new ArgumentException("Percentage must be between 0 and 100.", nameof(config.Percentage));

            var overlapping = _unitOfWork.CommissionConfigurationRepository
                .FindByCondition(c =>
                    c.Id != config.Id &&
                    c.MinPrice <= (config.MaxPrice ?? decimal.MaxValue) &&
                    (!c.MaxPrice.HasValue || c.MaxPrice >= config.MinPrice))
                .Any();

            if (overlapping)
                throw new ArgumentException("This commission tier overlaps with an existing configuration.", nameof(config));
        }

        public async Task<CommissionConfigurationDto> GetByIdAsync(int id)
        {
            var commission = await _unitOfWork.CommissionConfigurationRepository.GetByIdAsync(id);
            if (commission == null) throw new BadRequestException($"No commission found for the provided ID {id}");
            return new CommissionConfigurationDto
            {
                Id = commission.Id,
                MaxPrice = commission.MaxPrice,
                MinPrice = commission.MinPrice,
                Percentage = commission.Percentage
            };
        }

        private static CommissionConfigurationDto MapToDto(CommissionConfiguration config) =>
            new CommissionConfigurationDto
            {
                Id = config.Id,
                MinPrice = config.MinPrice,
                MaxPrice = config.MaxPrice,
                Percentage = config.Percentage
            };


    }
}