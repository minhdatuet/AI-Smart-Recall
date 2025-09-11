using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Repositories
{
    public class ContentRepository : BaseRepository<Content>, IContentRepository
    {
        public ContentRepository(MongoDBContext context) : base(context, "contents")
        {
        }

        public async Task<IEnumerable<Content>> GetByUserIdAsync(ObjectId userId)
        {
            return await FindAsync(c => c.UserId == userId);
        }

        public async Task<IEnumerable<Content>> GetPublicContentsAsync()
        {
            return await FindAsync(c => c.IsPublic);
        }

        public async Task<IEnumerable<Content>> SearchContentsAsync(string searchTerm, ObjectId? userId = null, bool publicOnly = false)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>();

            // Base visibility filter
            if (publicOnly)
            {
                filters.Add(builder.Eq(c => c.IsPublic, true));
            }
            else if (userId.HasValue)
            {
                filters.Add(builder.Or(
                    builder.Eq(c => c.UserId, userId.Value),
                    builder.Eq(c => c.IsPublic, true)
                ));
            }

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filters.Add(builder.Or(
                    builder.Regex("title", new BsonRegularExpression(searchTerm, "i")),
                    builder.Regex("contentText", new BsonRegularExpression(searchTerm, "i"))
                ));
            }

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<Content>> GetContentsByTagsAsync(List<string> tags, ObjectId? userId = null, bool publicOnly = false)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>();

            // Base visibility filter
            if (publicOnly)
            {
                filters.Add(builder.Eq(c => c.IsPublic, true));
            }
            else if (userId.HasValue)
            {
                filters.Add(builder.Or(
                    builder.Eq(c => c.UserId, userId.Value),
                    builder.Eq(c => c.IsPublic, true)
                ));
            }

            // Tags filter
            if (tags?.Any() == true)
            {
                filters.Add(builder.AnyIn("tags", tags));
            }

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<Content>> GetContentsByLearningModeAsync(string learningMode, ObjectId? userId = null, bool publicOnly = false)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>();

            // Base visibility filter
            if (publicOnly)
            {
                filters.Add(builder.Eq(c => c.IsPublic, true));
            }
            else if (userId.HasValue)
            {
                filters.Add(builder.Or(
                    builder.Eq(c => c.UserId, userId.Value),
                    builder.Eq(c => c.IsPublic, true)
                ));
            }

            // Learning mode filter
            if (!string.IsNullOrEmpty(learningMode))
            {
                filters.Add(builder.Eq(c => c.LearningMode, learningMode));
            }

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<Content>> GetUserContentsPaginatedAsync(ObjectId userId, int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? learningMode = null, string? sortBy = null, string? sortOrder = null)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>
            {
                builder.Eq(c => c.UserId, userId)
            };

            // Apply additional filters
            AddSearchFilters(builder, filters, searchTerm, tags, learningMode);

            var finalFilter = builder.And(filters);
            var query = _collection.Find(finalFilter);

            // Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            return await query
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Content>> GetPublicContentsPaginatedAsync(int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? learningMode = null, string? sortBy = null, string? sortOrder = null)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>
            {
                builder.Eq(c => c.IsPublic, true)
            };

            // Apply additional filters
            AddSearchFilters(builder, filters, searchTerm, tags, learningMode);

            var finalFilter = builder.And(filters);
            var query = _collection.Find(finalFilter);

            // Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            return await query
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<long> GetTotalCountAsync(ObjectId? userId = null, bool publicOnly = false, string? searchTerm = null, List<string>? tags = null, string? learningMode = null)
        {
            var builder = Builders<Content>.Filter;
            var filters = new List<FilterDefinition<Content>>();

            // Base visibility filter
            if (publicOnly)
            {
                filters.Add(builder.Eq(c => c.IsPublic, true));
            }
            else if (userId.HasValue)
            {
                filters.Add(builder.Eq(c => c.UserId, userId.Value));
            }

            // Apply additional filters
            AddSearchFilters(builder, filters, searchTerm, tags, learningMode);

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.CountDocumentsAsync(finalFilter);
        }

        public async Task<IEnumerable<string>> GetAllTagsAsync(ObjectId? userId = null)
        {
            var builder = Builders<Content>.Filter;
            var filter = userId.HasValue ? 
                builder.Eq(c => c.UserId, userId.Value) : 
                builder.Empty;

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.ToBsonDocument()),
                new BsonDocument("$unwind", "$tags"),
                new BsonDocument("$group", new BsonDocument("_id", "$tags")),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result.Select(doc => doc["_id"].AsString).Where(tag => !string.IsNullOrEmpty(tag));
        }

        public async Task<IEnumerable<string>> GetAllLearningModesAsync(ObjectId? userId = null)
        {
            var builder = Builders<Content>.Filter;
            var filter = userId.HasValue ? 
                builder.Eq(c => c.UserId, userId.Value) : 
                builder.Empty;

            var result = await _collection.Distinct<string>("learningMode", filter).ToListAsync();
            return result.Where(mode => !string.IsNullOrEmpty(mode));
        }

        public async Task<bool> IsContentOwnedByUserAsync(ObjectId contentId, ObjectId userId)
        {
            return await ExistsAsync(c => c.Id == contentId && c.UserId == userId);
        }

        public async Task<IEnumerable<Content>> GetRecentContentsAsync(ObjectId userId, int count = 10)
        {
            return await _collection
                .Find(c => c.UserId == userId)
                .SortByDescending(c => c.UpdatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Content>> GetPopularPublicContentsAsync(int count = 10)
        {
            // For now, sort by creation date. In future, you might want to sort by view count or other popularity metrics
            return await _collection
                .Find(c => c.IsPublic)
                .SortByDescending(c => c.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        private void AddSearchFilters(FilterDefinitionBuilder<Content> builder, List<FilterDefinition<Content>> filters, string? searchTerm, List<string>? tags, string? learningMode)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filters.Add(builder.Or(
                    builder.Regex("title", new BsonRegularExpression(searchTerm, "i")),
                    builder.Regex("contentText", new BsonRegularExpression(searchTerm, "i"))
                ));
            }

            if (tags?.Any() == true)
            {
                filters.Add(builder.AnyIn("tags", tags));
            }

            if (!string.IsNullOrEmpty(learningMode))
            {
                filters.Add(builder.Eq(c => c.LearningMode, learningMode));
            }
        }

        private IFindFluent<Content, Content> ApplySorting(IFindFluent<Content, Content> query, string? sortBy, string? sortOrder)
        {
            var ascending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";

            return sortBy?.ToLower() switch
            {
                "title" => ascending ? query.SortBy(c => c.Title) : query.SortByDescending(c => c.Title),
                "updatedat" => ascending ? query.SortBy(c => c.UpdatedAt) : query.SortByDescending(c => c.UpdatedAt),
                "createdat" => ascending ? query.SortBy(c => c.CreatedAt) : query.SortByDescending(c => c.CreatedAt),
                _ => query.SortByDescending(c => c.CreatedAt) // Default sort
            };
        }
    }
}
