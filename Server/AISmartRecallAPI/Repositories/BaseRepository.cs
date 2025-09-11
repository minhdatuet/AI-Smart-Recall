using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using AISmartRecallAPI.Data;

namespace AISmartRecallAPI.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;
        protected readonly MongoDBContext _context;

        public BaseRepository(MongoDBContext context, string collectionName)
        {
            _context = context;
            _collection = _context.GetType().GetProperty(GetCollectionPropertyName(collectionName))?
                .GetValue(_context) as IMongoCollection<T> ?? 
                throw new InvalidOperationException($"Collection {collectionName} not found in context");
        }

        private static string GetCollectionPropertyName(string collectionName)
        {
            return collectionName.ToLower() switch
            {
                "users" => nameof(MongoDBContext.Users),
                "contents" => nameof(MongoDBContext.Contents), 
                "questions" => nameof(MongoDBContext.Questions),
                "learningSessions" => nameof(MongoDBContext.LearningSessions),
                _ => throw new ArgumentException($"Unknown collection: {collectionName}")
            };
        }

        public virtual async Task<T> GetByIdAsync(ObjectId id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return null;

            return await GetByIdAsync(objectId);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).ToListAsync();
        }

        public virtual async Task<T> FindOneAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).FirstOrDefaultAsync();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task<IEnumerable<T>> CreateManyAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            await _collection.InsertManyAsync(entityList);
            return entityList;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException("Entity must have an Id property");

            var id = (ObjectId)idProperty.GetValue(entity);
            var filter = Builders<T>.Filter.Eq("_id", id);
            
            await _collection.ReplaceOneAsync(filter, entity);
            return entity;
        }

        public virtual async Task<T> UpdateAsync(ObjectId id, UpdateDefinition<T> update)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.FindOneAndUpdateAsync(
                filter, 
                update,
                new FindOneAndUpdateOptions<T> { ReturnDocument = ReturnDocument.After }
            );
        }

        public virtual async Task<bool> DeleteAsync(ObjectId id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return false;

            return await DeleteAsync(objectId);
        }

        public virtual async Task<long> DeleteManyAsync(Expression<Func<T, bool>> predicate)
        {
            var result = await _collection.DeleteManyAsync(predicate);
            return result.DeletedCount;
        }

        public virtual async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }

        public virtual async Task<long> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.CountDocumentsAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.CountDocumentsAsync(predicate) > 0;
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _collection
                .Find(_ => true)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate)
        {
            return await _collection
                .Find(predicate)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool ascending = true)
        {
            var query = _collection.Find(predicate);
            
            if (ascending)
                query = query.SortBy(orderBy);
            else
                query = query.SortByDescending(orderBy);

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public virtual async Task<T> FindOneAndUpdateAsync(Expression<Func<T, bool>> predicate, UpdateDefinition<T> update, bool returnUpdated = true)
        {
            var options = new FindOneAndUpdateOptions<T>
            {
                ReturnDocument = returnUpdated ? ReturnDocument.After : ReturnDocument.Before
            };

            return await _collection.FindOneAndUpdateAsync(predicate, update, options);
        }

        public virtual async Task<T> FindOneAndDeleteAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.FindOneAndDeleteAsync(predicate);
        }

        public virtual async Task<IEnumerable<T>> FindWithProjectionAsync<TProjection>(Expression<Func<T, bool>> predicate, Expression<Func<T, TProjection>> projection)
        {
            // For now, just return without projection to avoid type issues
            return await _collection
                .Find(predicate)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> AggregateAsync(PipelineDefinition<T, T> pipeline)
        {
            return await _collection.Aggregate(pipeline).ToListAsync();
        }
    }
}
