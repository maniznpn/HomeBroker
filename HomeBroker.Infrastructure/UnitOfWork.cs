using HomeBroker.Application.IUnitOfWork;
using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Storage;

namespace HomeBroker.Infrastructure;

/// <summary>
/// Unit of Work pattern implementation for managing repository lifecycles and transactions.
/// Repositories are now injected via constructor to prevent circular dependencies.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly HomeBrokerDbContext _dbContext;
    private readonly IPropertyListingRepository _propertyListingRepository;
    private readonly ICommissionConfigurationRepository _commissionConfigurationRepository;
    private IDbContextTransaction _transaction;

    public UnitOfWork(
        HomeBrokerDbContext dbContext,
        IPropertyListingRepository propertyListingRepository,
        ICommissionConfigurationRepository commissionConfigurationRepository)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _propertyListingRepository = propertyListingRepository ?? throw new ArgumentNullException(nameof(propertyListingRepository));
        _commissionConfigurationRepository = commissionConfigurationRepository ?? throw new ArgumentNullException(nameof(commissionConfigurationRepository));
    }

    public ICommissionConfigurationRepository CommissionConfigurationRepository =>
        _commissionConfigurationRepository;

    public IPropertyListingRepository PropertyListingRepository =>
        _propertyListingRepository;

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress to commit.");
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await DisposeTransactionAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}