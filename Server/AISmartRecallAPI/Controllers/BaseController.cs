using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace AISmartRecallAPI.Controllers
{
    /// <summary>
    /// Base controller providing common functionality for all API controllers
    /// </summary>
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly IConfiguration Config;
        protected readonly IHttpContextAccessor HttpContextAccessor;
        protected readonly MongoDBContext DbContext;
        protected readonly ILogger Logger;

        private string? _userId;

        /// <summary>
        /// Gets the current authenticated user ID from JWT token
        /// </summary>
        protected string? UserId
        {
            get
            {
                if (string.IsNullOrEmpty(_userId))
                {
                    _userId = HttpContextAccessor.HttpContext?.User
                        .FindFirstValue(ClaimTypes.NameIdentifier);
                }
                return _userId;
            }
        }

        /// <summary>
        /// Gets the current user's email from JWT token
        /// </summary>
        protected string? UserEmail =>
            HttpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        /// <summary>
        /// Gets the current user's username from JWT token
        /// </summary>
        protected string? Username =>
            HttpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

        /// <summary>
        /// Gets the client's IP address
        /// </summary>
        protected string? ClientIP =>
            HttpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public BaseController(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            MongoDBContext dbContext,
            ILogger logger)
        {
            HttpContextAccessor = httpContextAccessor;
            Config = config;
            DbContext = dbContext;
            Logger = logger;
        }

        /// <summary>
        /// Gets the current authenticated user from database
        /// </summary>
        /// <returns>Current user or null if not found</returns>
        protected async Task<User?> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(UserId) || 
                !MongoDB.Bson.ObjectId.TryParse(UserId, out var objectId))
            {
                return null;
            }

            return await DbContext.Users
                .Find(u => u.Id == objectId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Maps MongoDB User model to SharedModels UserProfileDTO
        /// </summary>
        /// <param name="user">MongoDB User model</param>
        /// <returns>UserProfileDTO for API responses</returns>
        protected UserProfileDTO MapToUserProfileDTO(User user)
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

        /// <summary>
        /// Creates standardized API response for successful operations
        /// </summary>
        /// <typeparam name="T">Response data type</typeparam>
        /// <param name="data">Response data</param>
        /// <param name="message">Success message</param>
        /// <returns>Standardized API response</returns>
        protected IActionResult CreateSuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                success = true,
                message = message,
                data = data
            });
        }

        /// <summary>
        /// Creates standardized API response for errors
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Standardized error response</returns>
        protected IActionResult CreateErrorResponse(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                success = false,
                message = message,
                data = (object?)null
            });
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if email is valid</returns>
        protected bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Logs user action for auditing purposes
        /// </summary>
        /// <param name="action">Action performed</param>
        /// <param name="details">Additional details</param>
        protected void LogUserAction(string action, object? details = null)
        {
            Logger.LogInformation(
                "User Action - UserId: {UserId}, Email: {Email}, Action: {Action}, IP: {IP}, Details: {@Details}",
                UserId, UserEmail, action, ClientIP, details);
        }

        /// <summary>
        /// Gets available AI providers configuration
        /// </summary>
        /// <returns>List of available AI providers</returns>
        protected List<AIProviderDTO> GetAvailableAIProviders()
        {
            return new List<AIProviderDTO>
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
        }
    }
}
