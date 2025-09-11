using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Repositories
{
    public class LearningSessionRepository : BaseRepository<LearningSession>, ILearningSessionRepository
    {
        public LearningSessionRepository(MongoDBContext context) : base(context, "learningSessions")
        {
        }

        public async Task<IEnumerable<LearningSession>> GetByUserIdAsync(ObjectId userId)
        {
            return await FindAsync(s => s.UserId == userId);
        }

        public async Task<IEnumerable<LearningSession>> GetByContentIdAsync(ObjectId contentId)
        {
            return await FindAsync(s => s.ContentId == contentId.ToString());
        }

        public async Task<IEnumerable<LearningSession>> GetByContentIdsAsync(List<ObjectId> contentIds)
        {
            var strIds = contentIds.Select(id => id.ToString()).ToList();
            var filter = Builders<LearningSession>.Filter.In(s => s.ContentId, strIds);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<IEnumerable<LearningSession>> GetByStatusAsync(string status, ObjectId? userId = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.Status, status)
            };

            if (userId.HasValue)
            {
                filters.Add(builder.Eq(s => s.UserId, userId.Value));
            }

            var finalFilter = builder.And(filters);
            return await _collection.Find(finalFilter).ToListAsync();
        }

        public async Task<IEnumerable<LearningSession>> GetActiveLearningSessionsAsync(ObjectId userId)
        {
            var builder = Builders<LearningSession>.Filter;
            var filter = builder.And(
                builder.Eq(s => s.UserId, userId),
                builder.Eq(s => s.Status, "active")
            );

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<LearningSession?> GetActiveSessionByRoomIdAsync(string roomId)
        {
            return await FindOneAsync(s => s.RoomId == roomId && s.Status == "active");
        }

        public async Task<IEnumerable<LearningSession>> GetSessionsByRoomIdAsync(string roomId)
        {
            return await FindAsync(s => s.RoomId == roomId);
        }

        public async Task<IEnumerable<LearningSession>> GetCompletedSessionsAsync(ObjectId userId, int? limit = null)
        {
            var baseQuery = _collection
                .Find(s => s.UserId == userId && s.Status == "completed");
                
            var sortedQuery = baseQuery.SortByDescending(s => s.CompletedAt);

            if (limit.HasValue)
            {
                return await sortedQuery.Limit(limit.Value).ToListAsync();
            }

            return await sortedQuery.ToListAsync();
        }

        public async Task<IEnumerable<LearningSession>> GetSessionsPaginatedAsync(ObjectId userId, int page, int pageSize, string? status = null, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.UserId, userId)
            };

            if (!string.IsNullOrEmpty(status))
            {
                filters.Add(builder.Eq(s => s.Status, status));
            }

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(s => s.ContentId, contentId.Value.ToString()));
            }

            if (fromDate.HasValue)
            {
                filters.Add(builder.Gte(s => s.StartedAt, fromDate.Value));
            }

            if (toDate.HasValue)
            {
                filters.Add(builder.Lte(s => s.StartedAt, toDate.Value));
            }

            var finalFilter = builder.And(filters);

            return await _collection
                .Find(finalFilter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .SortByDescending(s => s.StartedAt)
                .ToListAsync();
        }

        public async Task<long> GetTotalSessionCountAsync(ObjectId userId, string? status = null, ObjectId? contentId = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.UserId, userId)
            };

            if (!string.IsNullOrEmpty(status))
            {
                filters.Add(builder.Eq(s => s.Status, status));
            }

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(s => s.ContentId, contentId.Value.ToString()));
            }

            var finalFilter = builder.And(filters);
            return await _collection.CountDocumentsAsync(finalFilter);
        }

        public async Task<double> GetAverageScoreAsync(ObjectId userId, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.UserId, userId),
                builder.Eq(s => s.Status, "completed")
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(s => s.ContentId, contentId.Value.ToString()));
            }

            if (fromDate.HasValue)
            {
                filters.Add(builder.Gte(s => s.CompletedAt, fromDate.Value));
            }

            if (toDate.HasValue)
            {
                filters.Add(builder.Lte(s => s.CompletedAt, toDate.Value));
            }

            var finalFilter = builder.And(filters);

            var pipeline = new[]
            {
                new BsonDocument("$match", finalFilter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = BsonNull.Value,
                    ["avgScore"] = new BsonDocument("$avg", "$score")
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return result != null && result.Contains("avgScore") ? result["avgScore"].ToDouble() : 0.0;
        }

        public async Task<long> GetTotalStudyTimeAsync(ObjectId userId, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.UserId, userId),
                builder.Eq(s => s.Status, "completed")
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(s => s.ContentId, contentId.Value.ToString()));
            }

            if (fromDate.HasValue)
            {
                filters.Add(builder.Gte(s => s.CompletedAt, fromDate.Value));
            }

            if (toDate.HasValue)
            {
                filters.Add(builder.Lte(s => s.CompletedAt, toDate.Value));
            }

            var finalFilter = builder.And(filters);

            var pipeline = new[]
            {
                new BsonDocument("$match", finalFilter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = BsonNull.Value,
                    ["totalTime"] = new BsonDocument("$sum", "$totalTimeSeconds")
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return result != null && result.Contains("totalTime") ? result["totalTime"].ToInt64() : 0L;
        }

        public async Task<Dictionary<string, long>> GetSessionStatusDistributionAsync(ObjectId userId)
        {
            var filter = Builders<LearningSession>.Filter.Eq(s => s.UserId, userId);
            var pipeline = new[]
            {
                new BsonDocument("$match", filter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$status",
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

        public async Task<Dictionary<DateTime, long>> GetStudyActivityAsync(ObjectId userId, int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var builder = Builders<LearningSession>.Filter;
            var filter = builder.And(
                builder.Eq(s => s.UserId, userId),
                builder.Gte(s => s.StartedAt, startDate)
            );

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument {
                        { "year", new BsonDocument("$year", "$startedAt") },
                        { "month", new BsonDocument("$month", "$startedAt") },
                        { "day", new BsonDocument("$dayOfMonth", "$startedAt") }
                    },
                    ["count"] = new BsonDocument("$sum", 1)
                }),
                new BsonDocument("$sort", new BsonDocument
                {
                    { "_id.year", 1 },
                    { "_id.month", 1 },
                    { "_id.day", 1 }
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var activity = new Dictionary<DateTime, long>();
            
            foreach (var doc in result)
            {
                var id = doc["_id"].AsBsonDocument;
                var date = new DateTime(id["year"].AsInt32, id["month"].AsInt32, id["day"].AsInt32);
                activity[date] = doc["count"].AsInt64;
            }

            return activity;
        }

        public async Task<IEnumerable<LearningSession>> GetTopScoringSessionsAsync(ObjectId userId, int count = 10, ObjectId? contentId = null)
        {
            var builder = Builders<LearningSession>.Filter;
            var filters = new List<FilterDefinition<LearningSession>>
            {
                builder.Eq(s => s.UserId, userId),
                builder.Eq(s => s.Status, "completed")
            };

            if (contentId.HasValue)
            {
                filters.Add(builder.Eq(s => s.ContentId, contentId.Value.ToString()));
            }

            var finalFilter = builder.And(filters);

            return await _collection
                .Find(finalFilter)
                .SortByDescending(s => s.Score)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<LearningSession>> GetRecentSessionsAsync(ObjectId userId, int count = 10)
        {
            return await _collection
                .Find(s => s.UserId == userId)
                .SortByDescending(s => s.StartedAt)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<bool> HasActiveSessionAsync(ObjectId userId, ObjectId contentId)
        {
            return await ExistsAsync(s => s.UserId == userId && s.ContentId == contentId.ToString() && s.Status == "active");
        }

        public async Task<LearningSession?> GetLastSessionAsync(ObjectId userId, ObjectId contentId)
        {
            return await _collection
                .Find(s => s.UserId == userId && s.ContentId == contentId.ToString())
                .SortByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<ObjectId, SessionStats>> GetContentSessionStatsAsync(ObjectId userId, List<ObjectId> contentIds)
        {
            var strIds = contentIds.Select(id => id.ToString()).ToList();
            var builder = Builders<LearningSession>.Filter;
            var filter = builder.And(
                builder.Eq(s => s.UserId, userId),
                builder.In(s => s.ContentId, strIds)
            );

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.ToBsonDocument()),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$contentId",
                    ["totalSessions"] = new BsonDocument("$sum", 1),
                    ["completedSessions"] = new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { new BsonDocument("$eq", new BsonArray { "$status", "completed" }), 1, 0 })),
                    ["averageScore"] = new BsonDocument("$avg", "$score"),
                    ["totalStudyTime"] = new BsonDocument("$sum", "$totalTimeSeconds"),
                    ["lastStudied"] = new BsonDocument("$max", "$completedAt")
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var stats = new Dictionary<ObjectId, SessionStats>();

            foreach (var doc in result)
            {
                var contentIdStr = doc["_id"].AsString;
                if (ObjectId.TryParse(contentIdStr, out var contentId))
                {
                    stats[contentId] = new SessionStats
                    {
                        TotalSessions = doc.GetValue("totalSessions", 0).ToInt64(),
                        CompletedSessions = doc.GetValue("completedSessions", 0).ToInt64(),
                        AverageScore = doc.GetValue("averageScore", 0.0).ToDouble(),
                        TotalStudyTime = doc.GetValue("totalStudyTime", 0).ToInt64(),
                        LastStudied = doc.Contains("lastStudied") && !doc["lastStudied"].IsBsonNull ? doc["lastStudied"].ToUniversalTime() : null
                    };
                }
            }

            return stats;
        }

        public async Task<bool> DeleteSessionsByContentIdAsync(ObjectId contentId)
        {
            var result = await DeleteManyAsync(s => s.ContentId == contentId.ToString());
            return result > 0;
        }
    }
}
