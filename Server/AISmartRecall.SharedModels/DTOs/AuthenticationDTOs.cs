using MemoryPack;

namespace AISmartRecall.SharedModels.DTOs
{
    #region Authentication DTOs

    [MemoryPackable]
    public partial class LoginRequestDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserProfileDTO User { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class RegisterRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class RegisterResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserProfileDTO? User { get; set; }
    }

    [MemoryPackable]
    public partial class UserProfileDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        
        // Learning Statistics
        public int TotalContentsCreated { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public int StreakDays { get; set; }
    }

    [MemoryPackable]
    public partial class UpdateProfileRequestDTO
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? PreferredAIProvider { get; set; }
        public string? DefaultLearningMode { get; set; }
    }

    [MemoryPackable]
    public partial class UpdateAPIKeysRequestDTO
    {
        public string? OpenRouterKey { get; set; }
    }

    [MemoryPackable]
    public partial class RefreshTokenRequestDTO
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    [MemoryPackable]
    public partial class RefreshTokenResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion

    #region AI Provider DTOs

    [MemoryPackable]
    public partial class AIProviderDTO
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<string> SupportedLanguages { get; set; } = new();
    }

    [MemoryPackable]
    public partial class GetAIProvidersResponseDTO
    {
        public List<AIProviderDTO> Providers { get; set; } = new();
        public string UserPreferredProvider { get; set; } = string.Empty;
    }

    #endregion
}
