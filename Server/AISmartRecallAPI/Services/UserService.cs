using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCrypt.Net;

namespace AISmartRecallAPI.Services
{
    public interface IUserService
    {
        Task<LoginResponse?> AuthenticateAsync(LoginRequest request);
        Task<UserInfo?> RegisterAsync(RegisterRequest request);
        Task<UserInfo?> GetUserByIdAsync(string userId);
        Task<UserInfo?> UpdateUserAsync(string userId, UserInfo userInfo);
        Task<bool> UpdateAPIKeysAsync(string userId, Dictionary<string, string> apiKeys);
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

        public async Task<LoginResponse?> AuthenticateAsync(LoginRequest request)
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
                    return null;
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password attempt for user: {Email}", request.Email);
                    return null;
                }

                // Update last active
                user.LastActive = DateTime.UtcNow;
                await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

                // Generate JWT token
                var token = await GenerateJwtTokenAsync(user);

                var userInfo = MapToUserInfo(user);

                _logger.LogInformation("User authenticated successfully: {Email}", request.Email);

                return new LoginResponse
                {
                    Token = token,
                    User = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
                throw;
            }
        }

        public async Task<UserInfo?> RegisterAsync(RegisterRequest request)
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
                    return null;
                }

                // Check if username is taken
                var existingUsername = await _context.Users
                    .Find(u => u.Username.ToLower() == request.Username.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    _logger.LogWarning("Registration attempt with existing username: {Username}", request.Username);
                    return null;
                }

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Profile = new UserProfile
                    {
                        DisplayName = request.DisplayName,
                        Level = 1,
                        TotalStudyTime = 0,
                        Streak = 0
                    },
                    AISettings = new AISettings
                    {
                        PreferredAI = "chatgpt",
                        DefaultLearningMode = "understanding",
                        APIKeys = new Dictionary<string, string>()
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                await _context.Users.InsertOneAsync(user);

                _logger.LogInformation("New user registered successfully: {Email}", request.Email);

                return MapToUserInfo(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                throw;
            }
        }

        public async Task<UserInfo?> GetUserByIdAsync(string userId)
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

                return user == null ? null : MapToUserInfo(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserInfo?> UpdateUserAsync(string userId, UserInfo userInfo)
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
                user.Profile.DisplayName = userInfo.Profile.DisplayName;
                user.Profile.Avatar = userInfo.Profile.Avatar;
                user.AISettings.PreferredAI = userInfo.AISettings.PreferredAI;
                user.AISettings.DefaultLearningMode = userInfo.AISettings.DefaultLearningMode;
                user.LastActive = DateTime.UtcNow;

                await _context.Users.ReplaceOneAsync(u => u.Id == objectId, user);

                _logger.LogInformation("User updated successfully: {UserId}", userId);

                return MapToUserInfo(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateAPIKeysAsync(string userId, Dictionary<string, string> apiKeys)
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

                // Encrypt API keys before storing
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

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-for-ai-smart-recall-that-is-at-least-64-characters-long-and-secure-for-production-use-2024";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AISmartRecall";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "AISmartRecall";
            var jwtExpireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 1440); // 24 hours default

            _logger.LogDebug("Generating JWT with Key length: {KeyLength}, Issuer: {Issuer}, Audience: {Audience}", 
                jwtKey.Length, jwtIssuer, jwtAudience);

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

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug("JWT token generated successfully with length: {TokenLength}", tokenString.Length);
            
            return tokenString;
        }

        private UserInfo MapToUserInfo(User user)
        {
            return new UserInfo
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                Profile = user.Profile,
                AISettings = new AISettings
                {
                    PreferredAI = user.AISettings.PreferredAI,
                    DefaultLearningMode = user.AISettings.DefaultLearningMode,
                    APIKeys = DecryptAPIKeys(user.AISettings.APIKeys) // Return decrypted keys
                }
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
