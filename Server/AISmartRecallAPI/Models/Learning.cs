using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AISmartRecallAPI.Models
{
    #region Learning Session Models

    public class LearningSession
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("contentId")]
        public string ContentId { get; set; } = string.Empty;

        [BsonElement("roomId")]
        public string? RoomId { get; set; }

        [BsonElement("sessionType")]
        public string SessionType { get; set; } = "solo"; // "solo" | "group"

        [BsonElement("questions")]
        public List<SessionQuestion> Questions { get; set; } = new();

        [BsonElement("totalQuestions")]
        public int TotalQuestions { get; set; }

        [BsonElement("correctAnswers")]
        public int CorrectAnswers { get; set; }

        [BsonElement("score")]
        public double Score { get; set; }

        [BsonElement("totalTimeSeconds")]
        public int TotalTimeSeconds { get; set; }

        [BsonElement("startedAt")]
        public DateTime StartedAt { get; set; }

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "active"; // "active" | "completed" | "abandoned"
    }

    public class SessionQuestion
    {
        [BsonElement("questionId")]
        public string QuestionId { get; set; } = string.Empty;

        [BsonElement("userAnswer")]
        public string UserAnswer { get; set; } = string.Empty;

        [BsonElement("isCorrect")]
        public bool IsCorrect { get; set; }

        [BsonElement("timeSpentSeconds")]
        public int TimeSpentSeconds { get; set; }

        [BsonElement("attemptCount")]
        public int AttemptCount { get; set; }

        [BsonElement("answeredAt")]
        public DateTime? AnsweredAt { get; set; }
    }

    #endregion

    #region Learning Room Models

    public class LearningRoom
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("code")]
        public string Code { get; set; } = string.Empty; // 6-digit join code

        [BsonElement("hostId")]
        public ObjectId HostId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("contentId")]
        public string ContentId { get; set; } = string.Empty;

        [BsonElement("participants")]
        public List<RoomParticipant> Participants { get; set; } = new();

        [BsonElement("maxParticipants")]
        public int MaxParticipants { get; set; } = 10;

        [BsonElement("timeLimitMinutes")]
        public int TimeLimitMinutes { get; set; } = 15;

        [BsonElement("questionCount")]
        public int QuestionCount { get; set; } = 10;

        [BsonElement("isPrivate")]
        public bool IsPrivate { get; set; } = false;

        [BsonElement("status")]
        public string Status { get; set; } = "waiting"; // "waiting" | "active" | "completed"

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }
    }

    public class RoomParticipant
    {
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [BsonElement("currentScore")]
        public int CurrentScore { get; set; }

        [BsonElement("questionsAnswered")]
        public int QuestionsAnswered { get; set; }

        [BsonElement("joinedAt")]
        public DateTime JoinedAt { get; set; }

        [BsonElement("isHost")]
        public bool IsHost { get; set; }

        [BsonElement("isReady")]
        public bool IsReady { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "joined"; // "joined" | "ready" | "playing" | "finished"
    }

    #endregion

    #region Progress Models

    public class UserProgress
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("level")]
        public int Level { get; set; } = 1;

        [BsonElement("experience")]
        public int Experience { get; set; } = 0;

        [BsonElement("totalSessions")]
        public int TotalSessions { get; set; } = 0;

        [BsonElement("totalQuestionsAnswered")]
        public int TotalQuestionsAnswered { get; set; } = 0;

        [BsonElement("totalCorrectAnswers")]
        public int TotalCorrectAnswers { get; set; } = 0;

        [BsonElement("streakDays")]
        public int StreakDays { get; set; } = 0;

        [BsonElement("lastStudyDate")]
        public DateTime LastStudyDate { get; set; }

        [BsonElement("studyTimeByContent")]
        public Dictionary<string, int> StudyTimeByContent { get; set; } = new();

        [BsonElement("accuracyByTopic")]
        public Dictionary<string, double> AccuracyByTopic { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    #endregion
}
