using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AISmartRecallAPI.Models
{
    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("profile")]
        public UserProfile Profile { get; set; } = new UserProfile();

        [BsonElement("aiSettings")]
        public AISettings AISettings { get; set; } = new AISettings();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("lastActive")]
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
    }

    public class UserProfile
    {
        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [BsonElement("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [BsonElement("level")]
        public int Level { get; set; } = 1;

        [BsonElement("totalStudyTime")]
        public long TotalStudyTime { get; set; } = 0; // in milliseconds

        [BsonElement("streak")]
        public int Streak { get; set; } = 0;
    }

    public class AISettings
    {
        [BsonElement("apiKeys")]
        public Dictionary<string, string> APIKeys { get; set; } = new Dictionary<string, string>();

        [BsonElement("preferredAI")]
        public string PreferredAI { get; set; } = "chatgpt";

        [BsonElement("defaultLearningMode")]
        public string DefaultLearningMode { get; set; } = "understanding";
    }

}
