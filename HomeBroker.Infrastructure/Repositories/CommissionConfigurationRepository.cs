using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.DbContext;
using HouseBroker.Domain.Entities;

namespace HomeBroker.Infrastructure;

/// <summary>
/// Repository for CommissionConfiguration entity.
/// Only depends on DbContext - transaction management is handled by UnitOfWork.
/// </summary>
public class CommissionConfigurationRepository : BaseRepository<CommissionConfiguration>, ICommissionConfigurationRepository
{
    public CommissionConfigurationRepository(HomeBrokerDbContext context) : base(context)
    {
    }
}
