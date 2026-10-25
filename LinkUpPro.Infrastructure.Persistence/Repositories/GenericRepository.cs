using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly LinkUpProAppContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(LinkUpProAppContext context)
        {
            _dbContext = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();
        }

        public virtual async Task<TEntity?> UpdateAsync(TEntity entity, params object[] keyValues)
        {
            var entry = await _dbSet.FindAsync(keyValues);

            if (entry != null)
            {
                _dbContext.Entry(entry).CurrentValues.SetValues(entity);
                await _dbContext.SaveChangesAsync();

                return entry;
            }

            return null;
        }

        public virtual async Task<bool> DeleteAsync(params object[] keyValues)
        {
            var entity = await _dbSet.FindAsync(keyValues);

            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public virtual async Task<TEntity?> GetByIdAsync(params object[] keyValues)
        {
            return await _dbSet.FindAsync(keyValues);
        }

        public virtual async Task<List<TEntity>> GetAllListAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>Executes a filtered query in the database instead of loading the entire table.</summary>
        public virtual Task<List<TEntity>> GetListAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate)
            => _dbSet.Where(predicate).ToListAsync();

        public virtual async Task<List<TEntity>> GetAllListWithIncludeAsync(List<string> properties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var property in properties)
            {
                query = query.Include(property);
            }

            return await query.ToListAsync();
        }

        public virtual IQueryable<TEntity> GetAllQuery()
        {
            return _dbSet.AsQueryable();
        }

        public virtual IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var property in properties)
            {
                query = query.Include(property);
            }

            return query;
        }
    }
}
