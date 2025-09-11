using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace AISmartRecallAPI.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        // Basic CRUD Operations
        Task<T> GetByIdAsync(ObjectId id);
        Task<T> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> FindOneAsync(Expression<Func<T, bool>> predicate);
        Task<T> CreateAsync(T entity);
        Task<IEnumerable<T>> CreateManyAsync(IEnumerable<T> entities);
        Task<T> UpdateAsync(T entity);
        Task<T> UpdateAsync(ObjectId id, UpdateDefinition<T> update);
        Task<bool> DeleteAsync(ObjectId id);
        Task<bool> DeleteAsync(string id);
        Task<long> DeleteManyAsync(Expression<Func<T, bool>> predicate);

        // Advanced Query Operations
        Task<long> CountAsync();
        Task<long> CountAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool ascending = true);

        // MongoDB Specific Operations
        Task<T> FindOneAndUpdateAsync(Expression<Func<T, bool>> predicate, UpdateDefinition<T> update, bool returnUpdated = true);
        Task<T> FindOneAndDeleteAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindWithProjectionAsync<TProjection>(Expression<Func<T, bool>> predicate, Expression<Func<T, TProjection>> projection);
        Task<IEnumerable<T>> AggregateAsync(PipelineDefinition<T, T> pipeline);
    }
}
