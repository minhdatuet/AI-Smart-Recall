using MemoryPack;

namespace AISmartRecall.SharedModels.DTOs
{
    #region Learning Session DTOs

    [MemoryPackable]
    public partial class StartLearningSessionRequestDTO
    {
        public string ContentId { get; set; } = string.Empty;
        public string? RoomId { get; set; } // null for solo sessions
        public List<string> QuestionIds { get; set; } = new();
        public string SessionType { get; set; } = "solo"; // "solo" | "group"
    }

    [MemoryPackable]
    public partial class LearningSessionDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string? RoomId { get; set; }
        public List<SessionQuestionDTO> Questions { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double Score { get; set; }
        public int TotalTimeSeconds { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "active"; // "active" | "completed" | "abandoned"
    }

    [MemoryPackable]
    public partial class SessionQuestionDTO
    {
        public string QuestionId { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }

    [MemoryPackable]
    public partial class SubmitAnswerRequestDTO
    {
        public string SessionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int TimeSpentSeconds { get; set; }
    }

    [MemoryPackable]
    public partial class SubmitAnswerResponseDTO
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public double CurrentScore { get; set; }
        public int QuestionsRemaining { get; set; }
        public bool SessionCompleted { get; set; }
    }

    [MemoryPackable]
    public partial class CompleteLearningSessionRequestDTO
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalTimeSeconds { get; set; }
    }

    [MemoryPackable]
    public partial class CompleteLearningSessionResponseDTO
    {
        public LearningSessionDTO Session { get; set; } = new();
        public UserProgressDTO UpdatedProgress { get; set; } = new();
        public bool LevelUp { get; set; }
        public int ExperienceGained { get; set; }
    }

    #endregion

    #region Progress & Statistics DTOs

    [MemoryPackable]
    public partial class UserProgressDTO
    {
        public string UserId { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int TotalSessions { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public double OverallAccuracy { get; set; }
        public int StreakDays { get; set; }
        public DateTime LastStudyDate { get; set; }
        public Dictionary<string, int> StudyTimeByContent { get; set; } = new();
        public Dictionary<string, double> AccuracyByTopic { get; set; } = new();
    }

    [MemoryPackable]
    public partial class GetProgressRequestDTO
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ContentId { get; set; }
    }

    [MemoryPackable]
    public partial class GetProgressResponseDTO
    {
        public UserProgressDTO Progress { get; set; } = new();
        public List<LearningSessionDTO> RecentSessions { get; set; } = new();
        public Dictionary<string, object> Statistics { get; set; } = new();
    }

    [MemoryPackable]
    public partial class LearningStatisticsDTO
    {
        public int TotalStudyTimeMinutes { get; set; }
        public double AverageSessionScore { get; set; }
        public double WeeklyProgress { get; set; }
        public Dictionary<string, int> QuestionTypeAccuracy { get; set; } = new();
        public List<string> StrengthAreas { get; set; } = new();
        public List<string> WeakAreas { get; set; } = new();
        public List<DailyProgressDTO> DailyProgress { get; set; } = new();
    }

    [MemoryPackable]
    public partial class DailyProgressDTO
    {
        public DateTime Date { get; set; }
        public int SessionsCompleted { get; set; }
        public int QuestionsAnswered { get; set; }
        public double AverageScore { get; set; }
        public int StudyTimeMinutes { get; set; }
    }

    #endregion

    #region Room Learning DTOs

    [MemoryPackable]
    public partial class CreateRoomRequestDTO
    {
        public string Name { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public int MaxParticipants { get; set; } = 10;
        public int TimeLimitMinutes { get; set; } = 15;
        public int QuestionCount { get; set; } = 10;
        public bool IsPrivate { get; set; } = false;
    }

    [MemoryPackable]
    public partial class LearningRoomDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // 6-digit join code
        public string HostId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public ContentDTO Content { get; set; } = new();
        public List<RoomParticipantDTO> Participants { get; set; } = new();
        public RoomSettingsDTO Settings { get; set; } = new();
        public string Status { get; set; } = "waiting"; // "waiting" | "active" | "completed"
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    [MemoryPackable]
    public partial class RoomSettingsDTO
    {
        public int MaxParticipants { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int QuestionCount { get; set; }
        public bool IsPrivate { get; set; }
        public bool AllowSpectators { get; set; } = false;
    }

    [MemoryPackable]
    public partial class RoomParticipantDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int CurrentScore { get; set; }
        public int QuestionsAnswered { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsHost { get; set; }
        public bool IsReady { get; set; }
        public string Status { get; set; } = "joined"; // "joined" | "ready" | "playing" | "finished"
    }

    [MemoryPackable]
    public partial class JoinRoomRequestDTO
    {
        public string RoomCode { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class JoinRoomResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public LearningRoomDTO? Room { get; set; }
    }

    [MemoryPackable]
    public partial class StartRoomSessionRequestDTO
    {
        public string RoomId { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class RoomLeaderboardDTO
    {
        public List<RoomParticipantDTO> Rankings { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }

    #endregion

    #region Real-time Events (for SignalR)

    [MemoryPackable]
    public partial class RoomEventDTO
    {
        public string EventType { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // JSON serialized data
        public DateTime Timestamp { get; set; }
    }

    [MemoryPackable]
    public partial class ParticipantAnsweredEventDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int NewScore { get; set; }
        public int QuestionNumber { get; set; }
    }

    [MemoryPackable]
    public partial class RoomStatusChangedEventDTO
    {
        public string NewStatus { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Message { get; set; }
    }

    #endregion
}
