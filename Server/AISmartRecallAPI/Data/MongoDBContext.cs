using MongoDB.Driver;
using AISmartRecallAPI.Models;

namespace AISmartRecallAPI.Data
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database;
        
        public MongoDBContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
            var databaseName = configuration.GetValue<string>("Database:Name") ?? "AISmartRecallDB";
            
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Collections
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<Content> Contents => _database.GetCollection<Content>("contents");
        public IMongoCollection<Question> Questions => _database.GetCollection<Question>("questions");
        public IMongoCollection<LearningRoom> LearningRooms => _database.GetCollection<LearningRoom>("learningRooms");
        public IMongoCollection<LearningSession> LearningSessions => _database.GetCollection<LearningSession>("learningSessions");
        public IMongoCollection<UserProgress> UserProgress => _database.GetCollection<UserProgress>("userProgress");

        // Indexes setup method
        public async Task CreateIndexesAsync()
        {
            // User indexes
            var userIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var userIndexOptions = new CreateIndexOptions { Unique = true };
            await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(userIndexKeys, userIndexOptions));

            // Content indexes
            var contentUserIdIndex = Builders<Content>.IndexKeys.Ascending(c => c.UserId);
            await Contents.Indexes.CreateOneAsync(new CreateIndexModel<Content>(contentUserIdIndex));

            // Question indexes
            var questionContentIdIndex = Builders<Question>.IndexKeys.Ascending(q => q.ContentId);
            await Questions.Indexes.CreateOneAsync(new CreateIndexModel<Question>(questionContentIdIndex));

            // Room indexes
            var roomCodeIndex = Builders<LearningRoom>.IndexKeys.Ascending(r => r.Code);
            var roomCodeOptions = new CreateIndexOptions { Unique = true };
            await LearningRooms.Indexes.CreateOneAsync(new CreateIndexModel<LearningRoom>(roomCodeIndex, roomCodeOptions));

            // Session indexes
            var sessionUserIdIndex = Builders<LearningSession>.IndexKeys.Ascending(s => s.UserId);
            await LearningSessions.Indexes.CreateOneAsync(new CreateIndexModel<LearningSession>(sessionUserIdIndex));
            
            // Progress indexes
            var progressUserIdIndex = Builders<UserProgress>.IndexKeys.Ascending(p => p.UserId);
            var progressUserIdOptions = new CreateIndexOptions { Unique = true };
            await UserProgress.Indexes.CreateOneAsync(new CreateIndexModel<UserProgress>(progressUserIdIndex, progressUserIdOptions));
        }
    }
}
