using HomeBroker.Application.Exceptions;
using HomeBroker.Domain.IRepositories;
using HomeBroker.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HomeBroker.Infrastructure;

/// <summary>
/// Base repository class providing common CRUD operations.
/// Removed IUnitOfWork dependency to prevent circular dependency issues.
/// Transaction management is handled at the UnitOfWork level.
/// </summary>
public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    private readonly HomeBrokerDbContext _context;
    private readonly DbSet<T> _dbSet;

    public BaseRepository(HomeBrokerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) throw new BadRequestException("Entity not found");
        _dbSet.Remove(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) throw new BadRequestException("Entity not found");
        _dbSet.Remove(entity);
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
        => trackChanges ? _dbSet.Where(expression) : _dbSet.Where(expression).AsNoTracking();

    public IQueryable<T> GetAllAsync(bool trackChanges = false)
        => trackChanges ? _dbSet : _dbSet.AsNoTracking();

    public async Task<T?> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }
}
