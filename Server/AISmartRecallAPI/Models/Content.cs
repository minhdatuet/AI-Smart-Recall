using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AISmartRecallAPI.Models
{
    public class Content
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("contentText")]
        public string ContentText { get; set; } = string.Empty;

        [BsonElement("learningMode")]
        public string LearningMode { get; set; } = "understanding";

        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isPublic")]
        public bool IsPublic { get; set; } = false;

        [BsonElement("studyCount")]
        public int StudyCount { get; set; } = 0;
    }
}
