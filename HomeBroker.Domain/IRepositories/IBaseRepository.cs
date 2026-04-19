using System.Linq.Expressions;

namespace HomeBroker.Domain.IRepositories
{
    public interface IBaseRepository<T> where T : class
    {
        IQueryable<T> GetAllAsync(bool trackChanges = false);
        Task<T?> GetByIdAsync(long id);
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        void Update(T entity);
        Task DeleteAsync(long id);
        Task DeleteAsync(int id);
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    }
}
