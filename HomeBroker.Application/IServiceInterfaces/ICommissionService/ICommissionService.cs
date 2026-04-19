namespace HomeBroker.Application.IServiceInterfaces.ICommissionService
{
    /// <summary>
    /// Interface for commission calculation engine.
    /// Handles calculation of broker commissions based on property price and configurable tiers.
    /// </summary>
    public interface ICommissionService
    {
        /// <summary>
        /// Calculates the estimated commission for a given property price.
        /// </summary>
        /// <param name="price">The property price in rupees</param>
        /// <returns>The calculated commission amount</returns>
        Task<decimal> CalculateCommissionAsync(decimal price);

        /// <summary>
        /// Gets all active commission configurations.
        /// </summary>
        /// <returns>List of commission configuration tiers</returns>
        Task<IEnumerable<CommissionConfigurationDto>> GetCommissionConfigurationsAsync();

        /// <summary>
        /// Creates or updates a commission configuration tier.
        /// </summary>
        /// <param name="config">Commission configuration to create or update</param>
        /// <returns>The created/updated configuration</returns>
        Task<CommissionConfigurationDto> CreateOrUpdateConfigurationAsync(CommissionConfigurationDto config);

        /// <summary>
        /// Deletes a commission configuration tier.
        /// </summary>
        /// <param name="configId">The configuration id to delete</param>
        Task DeleteConfigurationAsync(int configId);


        Task<CommissionConfigurationDto> GetByIdAsync(int id);
    }

    /// <summary>
    /// Data Transfer Object for Commission Configuration
    /// </summary>
    public class CommissionConfigurationDto
    {
        public int Id { get; set; }
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal Percentage { get; set; }
        public string Description => GetDescription();

        private string GetDescription()
        {
            if (MaxPrice.HasValue)
            {
                return $"₹{MinPrice:N0} - ₹{MaxPrice:N0}: {Percentage}%";
            }
            return $"₹{MinPrice:N0} and above: {Percentage}%";
        }
    }
}
