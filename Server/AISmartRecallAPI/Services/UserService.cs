using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Driver;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCrypt.Net;
using System.Text;

namespace AISmartRecallAPI.Services
{
    public interface IUserService
    {
        Task<LoginResponseDTO?> AuthenticateAsync(LoginRequestDTO request);
        Task<RegisterResponseDTO?> RegisterAsync(RegisterRequestDTO request);
        Task<UserProfileDTO?> GetUserByIdAsync(string userId);
        Task<UserProfileDTO?> UpdateUserAsync(string userId, UpdateProfileRequestDTO request);
        Task<bool> UpdateAPIKeysAsync(string userId, UpdateAPIKeysRequestDTO request);
        Task<RefreshTokenResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request);
        Task<GetAIProvidersResponseDTO> GetAIProvidersAsync(string userId);
        Task<string> GenerateJwtTokenAsync(User user);
    }

    public class UserService : IUserService
    {
        private readonly MongoDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(MongoDBContext context, IConfiguration configuration, ILogger<UserService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDTO?> AuthenticateAsync(LoginRequestDTO request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .Find(u => u.Email.ToLower() == request.Email.ToLower())
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                    return new LoginResponseDTO
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Token = string.Empty,
                        RefreshToken = string.Empty,
                        User = new UserProfileDTO()
                    };
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password attempt for user: {Email}", request.Email);
                    return new LoginResponseDTO
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Token = string.Empty,
                        RefreshToken = string.Empty,
                        User = new UserProfileDTO()
                    };
                }

                // Update last active
                user.LastActive = DateTime.UtcNow;
                await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

                // Generate JWT token and refresh token
                var token = await GenerateJwtTokenAsync(user);
                var refreshToken = Guid.NewGuid().ToString(); // Simple refresh token

                var userProfile = MapToUserProfileDTO(user);

                _logger.LogInformation("User authenticated successfully: {Email}", request.Email);

                return new LoginResponseDTO
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = userProfile,
                    Success = true,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Internal server error",
                    Token = string.Empty,
                    RefreshToken = string.Empty,
                    User = new UserProfileDTO()
                };
            }
        }

        public async Task<RegisterResponseDTO?> RegisterAsync(RegisterRequestDTO request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .Find(u => u.Email.ToLower() == request.Email.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                // Check if username is taken
                var existingUsername = await _context.Users
                    .Find(u => u.Username.ToLower() == request.Username.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    _logger.LogWarning("Registration attempt with existing username: {Username}", request.Username);
                    return new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Profile = new UserProfile
                    {
                        DisplayName = request.Username, // Default to username
                        Level = 1,
                        TotalStudyTime = 0,
                        Streak = 0
                    },
                    AISettings = new AISettings
                    {
                        PreferredAI = AIProviders.OpenAI,
                        DefaultLearningMode = LearningModes.Understanding,
                        APIKeys = new Dictionary<string, string>()
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                await _context.Users.InsertOneAsync(user);

                _logger.LogInformation("New user registered successfully: {Email}", request.Email);

                var userProfile = MapToUserProfileDTO(user);

                return new RegisterResponseDTO
                {
                    Success = true,
                    Message = "Registration successful",
                    User = userProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                return new RegisterResponseDTO
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        public async Task<UserProfileDTO?> GetUserByIdAsync(string userId)
        {
            try
            {
                if (!MongoDB.Bson.ObjectId.TryParse(userId, out var objectId))
                {
                    return null;
                }

                var user = await _context.Users
                    .Find(u => u.Id == objectId)
                    .FirstOrDefaultAsync();

                return user == null ? null : MapToUserProfileDTO(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserProfileDTO?> UpdateUserAsync(string userId, UpdateProfileRequestDTO request)
        {
            try
            {
                if (!MongoDB.Bson.ObjectId.TryParse(userId, out var objectId))
                {
                    return null;
                }

                var user = await _context.Users
                    .Find(u => u.Id == objectId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                // Update user fields
                user.Profile.DisplayName = request.DisplayName;
                if (!string.IsNullOrEmpty(request.PreferredAIProvider))
                {
                    user.AISettings.PreferredAI = request.PreferredAIProvider;
                }
                if (!string.IsNullOrEmpty(request.DefaultLearningMode))
                {
                    user.AISettings.DefaultLearningMode = request.DefaultLearningMode;
                }
                user.LastActive = DateTime.UtcNow;

                await _context.Users.ReplaceOneAsync(u => u.Id == objectId, user);

                _logger.LogInformation("User updated successfully: {UserId}", userId);

                return MapToUserProfileDTO(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateAPIKeysAsync(string userId, UpdateAPIKeysRequestDTO request)
        {
            try
            {
                if (!MongoDB.Bson.ObjectId.TryParse(userId, out var objectId))
                {
                    return false;
                }

                var user = await _context.Users
                    .Find(u => u.Id == objectId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return false;
                }

                // Update API keys (encrypt before storing)
                var apiKeys = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(request.OpenAIKey))
                    apiKeys[AIProviders.OpenAI] = request.OpenAIKey;
                if (!string.IsNullOrEmpty(request.GeminiKey))
                    apiKeys[AIProviders.Gemini] = request.GeminiKey;
                if (!string.IsNullOrEmpty(request.QwenKey))
                    apiKeys[AIProviders.Qwen] = request.QwenKey;

                user.AISettings.APIKeys = EncryptAPIKeys(apiKeys);
                user.LastActive = DateTime.UtcNow;

                await _context.Users.ReplaceOneAsync(u => u.Id == objectId, user);

                _logger.LogInformation("API keys updated for user: {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API keys for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<RefreshTokenResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            try
            {
                // In a real implementation, you'd validate the refresh token against stored tokens
                // For now, we'll just generate a new token
                
                _logger.LogInformation("Token refresh requested");

                return new RefreshTokenResponseDTO
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = "new-jwt-token", // Would generate new token here
                    RefreshToken = Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new RefreshTokenResponseDTO
                {
                    Success = false,
                    Message = "Failed to refresh token",
                    Token = string.Empty,
                    RefreshToken = string.Empty
                };
            }
        }

        public async Task<GetAIProvidersResponseDTO> GetAIProvidersAsync(string userId)
        {
            try
            {
                var providers = new List<AIProviderDTO>
                {
                    new AIProviderDTO
                    {
                        Name = AIProviders.OpenAI,
                        DisplayName = "OpenAI ChatGPT",
                        Description = "OpenAI's GPT models - excellent for English content and general knowledge",
                        IsAvailable = true,
                        SupportedLanguages = new List<string> { "en", "vi", "fr", "es", "de", "ja", "ko", "zh" }
                    },
                    new AIProviderDTO
                    {
                        Name = AIProviders.Gemini,
                        DisplayName = "Google Gemini",
                        Description = "Google's Gemini Pro - great for multilingual content and complex text",
                        IsAvailable = true,
                        SupportedLanguages = new List<string> { "vi", "en", "zh", "ja", "ko", "fr", "es", "de" }
                    },
                    new AIProviderDTO
                    {
                        Name = AIProviders.Qwen,
                        DisplayName = "Alibaba Qwen",
                        Description = "Alibaba's Qwen models - specialized in Vietnamese and Chinese content",
                        IsAvailable = true,
                        SupportedLanguages = new List<string> { "vi", "zh", "en" }
                    }
                };

                string userPreferredProvider = AIProviders.OpenAI;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var userProfile = await GetUserByIdAsync(userId);
                    // userPreferredProvider = userProfile?.PreferredAIProvider ?? AIProviders.OpenAI;
                }

                return new GetAIProvidersResponseDTO
                {
                    Providers = providers,
                    UserPreferredProvider = userPreferredProvider
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI providers");
                return new GetAIProvidersResponseDTO
                {
                    Providers = new List<AIProviderDTO>(),
                    UserPreferredProvider = AIProviders.OpenAI
                };
            }
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-for-ai-smart-recall-that-is-at-least-64-characters-long-and-secure-for-production-use-2024";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AISmartRecall";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "AISmartRecall";
            var jwtExpireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 1440); // 24 hours default

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("displayName", user.Profile.DisplayName),
                new Claim("level", user.Profile.Level.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserProfileDTO MapToUserProfileDTO(User user)
        {
            return new UserProfileDTO
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.Profile.DisplayName,
                Level = user.Profile.Level,
                Experience = 0, // Will be calculated from learning sessions
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastActive,
                
                // Learning Statistics - will be populated from actual learning data
                TotalContentsCreated = 0, // TODO: Calculate from actual data
                TotalQuestionsAnswered = 0, // TODO: Calculate from sessions
                TotalCorrectAnswers = 0, // TODO: Calculate from sessions
                StreakDays = user.Profile.Streak
            };
        }

        private Dictionary<string, string> EncryptAPIKeys(Dictionary<string, string> apiKeys)
        {
            var encrypted = new Dictionary<string, string>();
            var encryptionKey = _configuration["Encryption:Key"] ?? "default-encryption-key-32-bytes-long";

            foreach (var kvp in apiKeys)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    encrypted[kvp.Key] = SimpleEncrypt(kvp.Value, encryptionKey);
                }
            }

            return encrypted;
        }

        private Dictionary<string, string> DecryptAPIKeys(Dictionary<string, string> encryptedApiKeys)
        {
            var decrypted = new Dictionary<string, string>();
            var encryptionKey = _configuration["Encryption:Key"] ?? "default-encryption-key-32-bytes-long";

            foreach (var kvp in encryptedApiKeys)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    try
                    {
                        decrypted[kvp.Key] = SimpleDecrypt(kvp.Value, encryptionKey);
                    }
                    catch
                    {
                        decrypted[kvp.Key] = ""; // Return empty if decryption fails
                    }
                }
            }

            return decrypted;
        }

        private string SimpleEncrypt(string plaintext, string key)
        {
            // Simple encryption - in production, use more robust encryption
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            for (int i = 0; i < plaintextBytes.Length; i++)
            {
                plaintextBytes[i] = (byte)(plaintextBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(plaintextBytes);
        }

        private string SimpleDecrypt(string ciphertext, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            var ciphertextBytes = Convert.FromBase64String(ciphertext);

            for (int i = 0; i < ciphertextBytes.Length; i++)
            {
                ciphertextBytes[i] = (byte)(ciphertextBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(ciphertextBytes);
        }
    }
}
