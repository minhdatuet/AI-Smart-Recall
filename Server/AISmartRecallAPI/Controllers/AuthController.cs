using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using AISmartRecallAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AISmartRecallAPI.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            MongoDBContext dbContext,
            ILogger<AuthController> logger
        ) : base(httpContextAccessor, config, dbContext, logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        /// <param name="request">Login request with email and password</param>
        /// <returns>Login response with JWT token and user info</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Attempt authentication
                var result = await _userService.AuthenticateAsync(request);

                if (result == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// User registration endpoint
        /// </summary>
        /// <param name="request">Registration request with user details</param>
        /// <returns>User info if registration successful</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.Email) || 
                    string.IsNullOrEmpty(request.Password) ||
                    string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new { message = "Email, password, and username are required" });
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new { message = "Invalid email format" });
                }

                // Validate password strength
                if (request.Password.Length < 6)
                {
                    return BadRequest(new { message = "Password must be at least 6 characters long" });
                }

                // Attempt registration
                var result = await _userService.RegisterAsync(request);

                if (result == null)
                {
                    return Conflict(new { message = "Email or username already exists" });
                }

                _logger.LogInformation("New user registered: {Email}", request.Email);

                return CreatedAtAction(nameof(GetProfile), new { }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user profile (requires authentication)
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userInfo = await _userService.GetUserByIdAsync(userId);

                if (userInfo == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user profile (requires authentication)
        /// </summary>
        /// <param name="userInfo">Updated user information</param>
        /// <returns>Updated user information</returns>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Validate request
                if (request == null)
                {
                    return BadRequest(new { message = "Request is required" });
                }

                var result = await _userService.UpdateUserAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("User profile updated: {UserId}", userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user API keys (requires authentication)
        /// </summary>
        /// <param name="apiKeys">Dictionary of AI provider API keys</param>
        /// <returns>Success message</returns>
        [HttpPut("api-keys")]
        [Authorize]
        public async Task<IActionResult> UpdateAPIKeys([FromBody] UpdateAPIKeysRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "API keys request is required" });
                }

                var result = await _userService.UpdateAPIKeysAsync(userId, request);

                if (!result)
                {
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("API keys updated for user: {UserId}", userId);

                return Ok(new { message = "API keys updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API keys");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Refresh JWT token (requires authentication)
        /// </summary>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userInfo = await _userService.GetUserByIdAsync(userId);

                if (userInfo == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Create User object for token generation
                var user = new User
                {
                    Id = MongoDB.Bson.ObjectId.Parse(userInfo.Id),
                    Username = userInfo.Username,
                    Email = userInfo.Email,
                    Profile = new UserProfile { DisplayName = userInfo.DisplayName }
                };

                var newToken = await _userService.GenerateJwtTokenAsync(user);

                _logger.LogInformation("Token refreshed for user: {UserId}", userId);

                return Ok(new { token = newToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Check if token is valid (requires authentication)
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                return Ok(new 
                { 
                    valid = true,
                    userId,
                    username,
                    email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Logout endpoint (client-side token removal)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User logged out: {UserId}", userId);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get available AI providers
        /// </summary>
        /// <returns>List of supported AI providers</returns>
        [HttpGet("ai-providers")]
        public IActionResult GetAIProviders()
        {
            var providers = new[]
            {
                new { name = "chatgpt", displayName = "ChatGPT (OpenAI)", description = "Best for general content and English text" },
                new { name = "gemini", displayName = "Gemini (Google)", description = "Excellent for multilingual content and long texts" },
                new { name = "qwen", displayName = "Qwen (Alibaba)", description = "Strong in Vietnamese and Chinese languages" }
            };

            return Ok(providers);
        }

        private new bool IsValidEmail(string email)
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
    }
}
