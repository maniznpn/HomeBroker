using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.DbContext;
using HouseBroker.Domain.Entities;

namespace HomeBroker.Infrastructure;

/// <summary>
/// Repository for PropertyListing entity.
/// Only depends on DbContext - transaction management is handled by UnitOfWork.
/// </summary>
public class PropertyListingRepository : BaseRepository<PropertyListing>, IPropertyListingRepository
{
    public PropertyListingRepository(HomeBrokerDbContext context) : base(context)
    {
    }
}
