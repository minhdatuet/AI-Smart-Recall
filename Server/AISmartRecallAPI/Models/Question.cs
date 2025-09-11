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
        public QuestionType Type { get; set; }

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

    public enum QuestionType
    {
        [BsonRepresentation(BsonType.String)]
        FillInBlank,        // Điền vào chỗ trống

        [BsonRepresentation(BsonType.String)]
        MultipleChoice,     // Trắc nghiệm

        [BsonRepresentation(BsonType.String)]
        TrueFalse,         // Đúng/Sai

        [BsonRepresentation(BsonType.String)]
        Flashcard,         // Thẻ ghi nhớ

        [BsonRepresentation(BsonType.String)]
        ExactTyping,       // Gõ lại chính xác

        [BsonRepresentation(BsonType.String)]
        MatchConcepts,     // Ghép ý tương ứng

        [BsonRepresentation(BsonType.String)]
        ShortAnswer        // Tự luận ngắn
    }

    public class LearningRoom
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("code")]
        public string Code { get; set; } = string.Empty;

        [BsonElement("hostId")]
        public ObjectId HostId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("contentId")]
        public ObjectId ContentId { get; set; }

        [BsonElement("participants")]
        public List<ObjectId> Participants { get; set; } = new List<ObjectId>();

        [BsonElement("settings")]
        public RoomSettings Settings { get; set; } = new RoomSettings();

        [BsonElement("status")]
        public RoomStatus Status { get; set; } = RoomStatus.Waiting;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }
    }

    public class RoomSettings
    {
        [BsonElement("maxParticipants")]
        public int MaxParticipants { get; set; } = 10;

        [BsonElement("timeLimit")]
        public int TimeLimit { get; set; } = 300; // seconds

        [BsonElement("questionCount")]
        public int QuestionCount { get; set; } = 10;

        [BsonElement("isPrivate")]
        public bool IsPrivate { get; set; } = false;
    }

    public enum RoomStatus
    {
        [BsonRepresentation(BsonType.String)]
        Waiting,

        [BsonRepresentation(BsonType.String)]
        Active,

        [BsonRepresentation(BsonType.String)]
        Completed,

        [BsonRepresentation(BsonType.String)]
        Cancelled
    }

    public class LearningSession
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("contentId")]
        public ObjectId ContentId { get; set; }

        [BsonElement("roomId")]
        public ObjectId? RoomId { get; set; } // null for solo sessions

        [BsonElement("questions")]
        public List<QuestionAnswer> Questions { get; set; } = new List<QuestionAnswer>();

        [BsonElement("totalScore")]
        public int TotalScore { get; set; } = 0;

        [BsonElement("totalTime")]
        public long TotalTime { get; set; } = 0; // milliseconds

        [BsonElement("startedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("isCompleted")]
        public bool IsCompleted { get; set; } = false;
    }

    public class QuestionAnswer
    {
        [BsonElement("questionId")]
        public ObjectId QuestionId { get; set; }

        [BsonElement("userAnswer")]
        public string UserAnswer { get; set; } = string.Empty;

        [BsonElement("isCorrect")]
        public bool IsCorrect { get; set; } = false;

        [BsonElement("timeSpent")]
        public long TimeSpent { get; set; } = 0; // milliseconds

        [BsonElement("attempts")]
        public int Attempts { get; set; } = 1;

        [BsonElement("answeredAt")]
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }

    // DTOs for API
    public class GenerateQuestionsRequest
    {
        public string ContentId { get; set; } = string.Empty;
        public List<QuestionType> QuestionTypes { get; set; } = new List<QuestionType>();
        public int Count { get; set; } = 10;
        public string AIProvider { get; set; } = "chatgpt";
    }

    public class QuestionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public string AIProvider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class StartSessionRequest
    {
        public string ContentId { get; set; } = string.Empty;
        public List<QuestionType> QuestionTypes { get; set; } = new List<QuestionType>();
        public int QuestionCount { get; set; } = 10;
        public string? RoomCode { get; set; }
    }

    public class AnswerQuestionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public long TimeSpent { get; set; } = 0;
    }
}
