using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AISmartRecallAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningController : BaseController
    {
        private readonly ILearningService _learningService;
        private readonly ILogger<LearningController> _logger;

        public LearningController(
            ILearningService learningService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            MongoDBContext dbContext,
            ILogger<LearningController> logger
        ) : base(httpContextAccessor, config, dbContext, logger)
        {
            _learningService = learningService;
            _logger = logger;
        }

        /// <summary>
        /// Start new learning session
        /// </summary>
        /// <param name="request">Learning session start request</param>
        /// <returns>Created learning session</returns>
        [HttpPost("sessions/start")]
        [Authorize]
        public async Task<IActionResult> StartLearningSession([FromBody] StartLearningSessionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.ContentId))
                {
                    return BadRequest(new { message = "Content ID is required" });
                }

                var result = await _learningService.StartLearningSessionAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found or no questions available" });
                }

                _logger.LogInformation("Learning session started: {SessionId} for user: {UserId}", result.Id, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting learning session");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Submit answer for current question
        /// </summary>
        /// <param name="request">Answer submission request</param>
        /// <returns>Answer validation result</returns>
        [HttpPost("sessions/answer")]
        [Authorize]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.SessionId) || string.IsNullOrEmpty(request.Answer))
                {
                    return BadRequest(new { message = "Session ID and answer are required" });
                }

                var result = await _learningService.SubmitAnswerAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Session not found or already completed" });
                }

                _logger.LogInformation("Answer submitted for session: {SessionId} by user: {UserId}", request.SessionId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Complete learning session
        /// </summary>
        /// <param name="request">Session completion request</param>
        /// <returns>Session completion result with statistics</returns>
        [HttpPost("sessions/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteLearningSession([FromBody] CompleteLearningSessionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequest(new { message = "Session ID is required" });
                }

                var result = await _learningService.CompleteLearningSessionAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Session not found or already completed" });
                }

                _logger.LogInformation("Learning session completed: {SessionId} for user: {UserId}", request.SessionId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing learning session");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user progress and statistics
        /// </summary>
        /// <param name="request">Progress request with filters</param>
        /// <returns>User progress and learning statistics</returns>
        [HttpPost("progress")]
        [Authorize]
        public async Task<IActionResult> GetProgress([FromBody] GetProgressRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var result = await _learningService.GetProgressAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user progress");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get learning statistics
        /// </summary>
        /// <param name="request">Statistics request with date range</param>
        /// <returns>Detailed learning statistics</returns>
        [HttpPost("statistics")]
        [Authorize]
        public async Task<IActionResult> GetLearningStatistics([FromBody] GetProgressRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var result = await _learningService.GetLearningStatisticsAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning statistics");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create learning room for group sessions
        /// </summary>
        /// <param name="request">Room creation request</param>
        /// <returns>Created room information</returns>
        [HttpPost("rooms/create")]
        [Authorize]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.ContentId))
                {
                    return BadRequest(new { message = "Room name and content ID are required" });
                }

                var result = await _learningService.CreateRoomAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found" });
                }

                _logger.LogInformation("Learning room created: {RoomId} by user: {UserId}", result.Id, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating learning room");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Join learning room
        /// </summary>
        /// <param name="request">Room join request</param>
        /// <returns>Room information with participants</returns>
        [HttpPost("rooms/join")]
        [Authorize]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.RoomCode))
                {
                    return BadRequest(new { message = "Room code is required" });
                }

                var result = await _learningService.JoinRoomAsync(userId, request);

                if (result?.Success != true)
                {
                    return BadRequest(new { message = result?.Message ?? "Failed to join room" });
                }

                _logger.LogInformation("User joined room: {RoomCode} by user: {UserId}", request.RoomCode, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining learning room");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Start room session (host only)
        /// </summary>
        /// <param name="request">Room session start request</param>
        /// <returns>Started room session information</returns>
        [HttpPost("rooms/start-session")]
        [Authorize]
        public async Task<IActionResult> StartRoomSession([FromBody] StartRoomSessionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (request == null || string.IsNullOrEmpty(request.RoomId))
                {
                    return BadRequest(new { message = "Room ID is required" });
                }

                var result = await _learningService.StartRoomSessionAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Room not found or access denied" });
                }

                _logger.LogInformation("Room session started: {RoomId} by user: {UserId}", request.RoomId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting room session");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get room leaderboard
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <returns>Room leaderboard with participant rankings</returns>
        [HttpGet("rooms/{roomId}/leaderboard")]
        [Authorize]
        public async Task<IActionResult> GetRoomLeaderboard(string roomId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (string.IsNullOrEmpty(roomId))
                {
                    return BadRequest(new { message = "Room ID is required" });
                }

                var result = await _learningService.GetRoomLeaderboardAsync(roomId, userId);

                if (result == null)
                {
                    return NotFound(new { message = "Room not found or access denied" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room leaderboard: {RoomId}", roomId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
