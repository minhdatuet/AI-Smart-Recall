using MemoryPack;

namespace AISmartRecall.SharedModels.DTOs
{
    #region Content DTOs

    [MemoryPackable]
    public partial class CreateContentRequestDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string LearningMode { get; set; } = string.Empty; // "memorization" | "understanding"
        public List<string> Tags { get; set; } = new();
        public bool IsPublic { get; set; } = false;
    }

    [MemoryPackable]
    public partial class ContentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string LearningMode { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Statistics
        public int TotalQuestions { get; set; }
        public int TimesStudied { get; set; }
        public double AverageScore { get; set; }
    }

    [MemoryPackable]
    public partial class GetContentsResponseDTO
    {
        public List<ContentDTO> Contents { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    [MemoryPackable]
    public partial class UpdateContentRequestDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsPublic { get; set; }
    }

    [MemoryPackable]
    public partial class GetContentsRequestDTO
    {
        public string? SearchTerm { get; set; }
        public List<string>? Tags { get; set; }
        public string? LearningMode { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "createdAt"; // "createdAt", "title", "timesStudied"
        public string SortOrder { get; set; } = "desc"; // "asc", "desc"
    }

    [MemoryPackable]
    public partial class GetPublicContentsRequestDTO
    {
        public string? SearchTerm { get; set; }
        public List<string>? Tags { get; set; }
        public string? LearningMode { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "timesStudied"; // "timesStudied", "averageScore", "createdAt"
        public string SortOrder { get; set; } = "desc";
    }

    [MemoryPackable]
    public partial class GetPublicContentsResponseDTO
    {
        public List<ContentDTO> Contents { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    [MemoryPackable]
    public partial class ContentStatisticsDTO
    {
        public string ContentId { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TimesStudied { get; set; }
        public double AverageScore { get; set; }
        public int TotalStudyTime { get; set; } // in seconds
        public DateTime LastStudied { get; set; }
        public Dictionary<string, int> QuestionTypeBreakdown { get; set; } = new();
    }

    #endregion

    #region Question DTOs

    [MemoryPackable]
    public partial class GenerateQuestionsRequestDTO
    {
        public string ContentId { get; set; } = string.Empty;
        public string AIProvider { get; set; } = string.Empty; // "openai" | "gemini" | "qwen"
        public int QuestionCount { get; set; } = 10;
        public List<string> QuestionTypes { get; set; } = new();
    }

    [MemoryPackable]
    public partial class QuestionDTO
    {
        public string Id { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "fill_blank", "multiple_choice", etc.
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new(); // For multiple choice
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 1; // 1-5
        public string AIProvider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    [MemoryPackable]
    public partial class GenerateQuestionsResponseDTO
    {
        public List<QuestionDTO> Questions { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AIProvider { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class GetQuestionsRequestDTO
    {
        public string ContentId { get; set; } = string.Empty;
        public List<string>? QuestionTypes { get; set; }
        public int? Limit { get; set; }
    }

    [MemoryPackable]
    public partial class GetQuestionsResponseDTO
    {
        public List<QuestionDTO> Questions { get; set; } = new();
        public int TotalCount { get; set; }
    }

    [MemoryPackable]
    public partial class CreateQuestionRequestDTO
    {
        public string ContentId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 1;
    }

    [MemoryPackable]
    public partial class UpdateQuestionRequestDTO
    {
        public string Type { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 1;
    }

    [MemoryPackable]
    public partial class ValidateAnswerRequestDTO
    {
        public string UserAnswer { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; } = false;
        public bool TrimWhitespace { get; set; } = true;
    }

    [MemoryPackable]
    public partial class ValidateAnswerResponseDTO
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public double Similarity { get; set; } // For partial credit
    }

    [MemoryPackable]
    public partial class GetRandomQuestionsRequestDTO
    {
        public List<string>? ContentIds { get; set; }
        public List<string>? QuestionTypes { get; set; }
        public int? MinDifficulty { get; set; }
        public int? MaxDifficulty { get; set; }
        public int Count { get; set; } = 10;
        public List<string>? ExcludeQuestionIds { get; set; } // For avoiding repeats
    }

    [MemoryPackable]
    public partial class GetRandomQuestionsResponseDTO
    {
        public List<QuestionDTO> Questions { get; set; } = new();
        public int TotalAvailable { get; set; }
    }

    #endregion

    #region Question Types Enum (as constants)

    public static class QuestionTypes
    {
        public const string FillBlank = "fill_blank";
        public const string MultipleChoice = "multiple_choice";
        public const string TrueFalse = "true_false";
        public const string Flashcard = "flashcard";
        public const string ExactTyping = "exact_typing";
        public const string ShortAnswer = "short_answer";
        public const string MatchConcepts = "match_concepts";
    }

    public static class LearningModes
    {
        public const string Memorization = "memorization";
        public const string Understanding = "understanding";
    }

    public static class AIProviders
    {
        public const string OpenRouter = "openrouter";
        // Legacy providers (kept for backward compatibility)
        public const string OpenAI = "openai";
        public const string Gemini = "gemini";
        public const string Qwen = "qwen";
    }

    #endregion
}
