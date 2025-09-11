using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Repositories
{
    public class QuestionRepository : BaseRepository<Question>, IQuestionRepository
    {
        private readonly IContentRepository _contentRepository;

        public QuestionRepository(MongoDBContext context, IContentRepository contentRepository) : base(context, "questions")
        {
            _contentRepository = contentRepository;
        }

        public async Task<IEnumerable<Question>> GetByContentIdAsync(ObjectId contentId)
        {
            return await FindAsync(q => q.ContentId == contentId);
        }

        public async Task<IEnumerable<Question>> GetByContentIdsAsync(List<ObjectId> contentIds)
        {
            var filter = Builders<Question>.Filter.In(q => q.ContentId, contentIds);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetByTypeAsync(string questionType, ObjectId? contentId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>
            {
                builder.Eq(q => q.Type, questionType)
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }

            var finalFilter = builder.And(filters);
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetByDifficultyRangeAsync(int minDifficulty, int maxDifficulty, ObjectId? contentId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>
            {
                builder.Gte(q => q.Difficulty, minDifficulty),
                builder.Lte(q => q.Difficulty, maxDifficulty)
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }

            var finalFilter = builder.And(filters);
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetRandomQuestionsAsync(int count, List<ObjectId>? contentIds = null, List<string>? questionTypes = null, int? minDifficulty = null, int? maxDifficulty = null, List<ObjectId>? excludeIds = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>();

            if (contentIds?.Any() == true)
            {
                filters.Add(builder.In(q => q.ContentId, contentIds));
            }

            if (questionTypes?.Any() == true)
            {
                filters.Add(builder.In(q => q.Type, questionTypes));
            }

            if (minDifficulty.HasValue)
            {
                filters.Add(builder.Gte(q => q.Difficulty, minDifficulty.Value));
            }

            if (maxDifficulty.HasValue)
            {
                filters.Add(builder.Lte(q => q.Difficulty, maxDifficulty.Value));
            }

            if (excludeIds?.Any() == true)
            {
                filters.Add(builder.Nin(q => q.Id, excludeIds));
            }

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;

            // Get more questions than needed for better randomization
            var questions = await _collection
                .Find(finalFilter)
                .Limit(count * 2)
                .ToListAsync();

            // Randomize and take the requested count
            var random = new Random();
            return questions
                .OrderBy(q => random.Next())
                .Take(count)
                .ToList();
        }

        public async Task<long> GetCountByContentIdAsync(ObjectId contentId)
        {
            return await CountAsync(q => q.ContentId == contentId);
        }

        public async Task<long> GetTotalCountByUserAsync(ObjectId userId)
        {
            // Get all content IDs for the user first
            var userContents = await _contentRepository.GetByUserIdAsync(userId);
            var contentIds = userContents.Select(c => c.Id).ToList();

            if (!contentIds.Any())
                return 0;

            var filter = Builders<Question>.Filter.In(q => q.ContentId, contentIds);
            return await _collection.CountDocumentsAsync(filter);
        }

        public async Task<IEnumerable<Question>> GetQuestionsPaginatedAsync(ObjectId contentId, int page, int pageSize, List<string>? questionTypes = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>
            {
                builder.Eq(q => q.ContentId, contentId)
            };

            if (questionTypes?.Any() == true)
            {
                filters.Add(builder.In(q => q.Type, questionTypes));
            }

            var finalFilter = builder.And(filters);

            return await _collection
                .Find(finalFilter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsQuestionOwnedByUserAsync(ObjectId questionId, ObjectId userId)
        {
            // First get the question to find its content ID
            var question = await GetByIdAsync(questionId);
            if (question == null)
                return false;

            // Then check if the content is owned by the user
            return await _contentRepository.IsContentOwnedByUserAsync(question.ContentId, userId);
        }

        public async Task<Dictionary<string, long>> GetQuestionTypeDistributionAsync(ObjectId? contentId = null, ObjectId? userId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>();

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }
            else if (userId.HasValue)
            {
                // Get all content IDs for the user
                var userContents = await _contentRepository.GetByUserIdAsync(userId.Value);
                var contentIds = userContents.Select(c => c.Id).ToList();
                
                if (contentIds.Any())
                {
                    filters.Add(builder.In(q => q.ContentId, contentIds));
                }
                else
                {
                    return new Dictionary<string, long>(); // No content, no questions
                }
            }

            var matchFilter = filters.Any() ? builder.And(filters) : builder.Empty;

            var pipeline = new[]
            {
                new BsonDocument("$match", matchFilter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$type",
                    ["count"] = new BsonDocument("$sum", 1)
                }),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result.ToDictionary(
                doc => doc["_id"].AsString,
                doc => doc["count"].AsInt64
            );
        }

        public async Task<Dictionary<int, long>> GetDifficultyDistributionAsync(ObjectId? contentId = null, ObjectId? userId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>();

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }
            else if (userId.HasValue)
            {
                // Get all content IDs for the user
                var userContents = await _contentRepository.GetByUserIdAsync(userId.Value);
                var contentIds = userContents.Select(c => c.Id).ToList();
                
                if (contentIds.Any())
                {
                    filters.Add(builder.In(q => q.ContentId, contentIds));
                }
                else
                {
                    return new Dictionary<int, long>(); // No content, no questions
                }
            }

            var matchFilter = filters.Any() ? builder.And(filters) : builder.Empty;

            var pipeline = new[]
            {
                new BsonDocument("$match", matchFilter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$difficulty",
                    ["count"] = new BsonDocument("$sum", 1)
                }),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return result.ToDictionary(
                doc => doc["_id"].AsInt32,
                doc => doc["count"].AsInt64
            );
        }

        public async Task<IEnumerable<Question>> GetQuestionsByAIProviderAsync(string aiProvider, ObjectId? contentId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>
            {
                builder.Eq(q => q.AIProvider, aiProvider)
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }

            var finalFilter = builder.And(filters);
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<bool> DeleteByContentIdAsync(ObjectId contentId)
        {
            var result = await DeleteManyAsync(q => q.ContentId == contentId);
            return result > 0;
        }

        public async Task<long> GetCountByAIProviderAsync(string aiProvider, ObjectId? contentId = null)
        {
            var builder = Builders<Question>.Filter;
            var filters = new List<FilterDefinition<Question>>
            {
                builder.Eq(q => q.AIProvider, aiProvider)
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(q => q.ContentId, contentId.Value));
            }

            var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.CountDocumentsAsync(finalFilter);
        }
    }
}
