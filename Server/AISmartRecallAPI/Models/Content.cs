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

        [BsonElement("type")]
        public ContentType Type { get; set; } = ContentType.Understanding;

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

    public enum ContentType
    {
        [BsonRepresentation(BsonType.String)]
        Memorization,
        
        [BsonRepresentation(BsonType.String)]
        Understanding
    }

    // DTOs for API
    public class CreateContentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string ContentText { get; set; } = string.Empty;
        public ContentType Type { get; set; } = ContentType.Understanding;
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsPublic { get; set; } = false;
    }

    public class UpdateContentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string ContentText { get; set; } = string.Empty;
        public ContentType Type { get; set; } = ContentType.Understanding;
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsPublic { get; set; } = false;
    }

    public class ContentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ContentText { get; set; } = string.Empty;
        public ContentType Type { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsPublic { get; set; }
        public int StudyCount { get; set; }
    }
}
