using HomeBroker.Domain.IRepositories;

namespace HomeBroker.Application.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ICommissionConfigurationRepository CommissionConfigurationRepository { get; }
        IPropertyListingRepository PropertyListingRepository { get; }
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveChangesAsync();
    }
}
