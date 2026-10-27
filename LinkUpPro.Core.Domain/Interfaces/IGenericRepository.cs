namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task<bool> DeleteAsync(params object[] keyValues);
        Task<List<TEntity>> GetAllListAsync();
        Task<List<TEntity>> GetAllListWithIncludeAsync(List<string> properties);
        IQueryable<TEntity> GetAllQuery();
        IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties);
        Task<TEntity?> GetByIdAsync(params object[] keyValues);
        Task<TEntity?> UpdateAsync(TEntity entity, params object[] keyValues);
    }
}