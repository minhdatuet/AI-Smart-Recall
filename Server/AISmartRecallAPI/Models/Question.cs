using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AISmartRecallAPI.Models
{
    public class Question
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("contentId")]
        public ObjectId ContentId { get; set; }

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [BsonElement("options")]
        public List<string> Options { get; set; } = new List<string>();

        [BsonElement("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [BsonElement("explanation")]
        public string Explanation { get; set; } = string.Empty;

        [BsonElement("difficulty")]
        public int Difficulty { get; set; } = 1; // 1-5 scale

        [BsonElement("aiProvider")]
        public string AIProvider { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("timesAnswered")]
        public int TimesAnswered { get; set; } = 0;

        [BsonElement("timesCorrect")]
        public int TimesCorrect { get; set; } = 0;
    }
}
