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
    public class QuestionController : BaseController
    {
        private readonly IQuestionService _questionService;
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(
            IQuestionService questionService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            MongoDBContext dbContext,
            ILogger<QuestionController> logger
        ) : base(httpContextAccessor, config, dbContext, logger)
        {
            _questionService = questionService;
            _logger = logger;
        }

        /// <summary>
        /// Create new question manually
        /// </summary>
        /// <param name="request">Question creation request</param>
        /// <returns>Created question information</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Validate request
                if (request == null || string.IsNullOrEmpty(request.Question) || string.IsNullOrEmpty(request.ContentId))
                {
                    return BadRequest(new { message = "Question text and content ID are required" });
                }

                var result = await _questionService.CreateQuestionAsync(userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found or access denied" });
                }

                _logger.LogInformation("Question created by user: {UserId}, QuestionId: {QuestionId}", userId, result.Id);

                return CreatedAtAction(nameof(GetQuestion), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get question by ID
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <returns>Question details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestion(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Question ID is required" });
                }

                var question = await _questionService.GetQuestionByIdAsync(id);

                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                return Ok(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question: {QuestionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get questions for specific content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="request">Get questions request with filters</param>
        /// <returns>List of questions</returns>
        [HttpPost("by-content/{contentId}")]
        public async Task<IActionResult> GetQuestionsByContent(string contentId, [FromBody] GetQuestionsRequestDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(contentId))
                {
                    return BadRequest(new { message = "Content ID is required" });
                }

                var result = await _questionService.GetQuestionsByContentAsync(contentId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for content: {ContentId}", contentId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update existing question
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <param name="request">Question update request</param>
        /// <returns>Updated question information</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateQuestion(string id, [FromBody] UpdateQuestionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Question ID is required" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "Update request is required" });
                }

                var result = await _questionService.UpdateQuestionAsync(id, userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Question not found or access denied" });
                }

                _logger.LogInformation("Question updated: {QuestionId} by user: {UserId}", id, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question: {QuestionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete question
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteQuestion(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Question ID is required" });
                }

                var success = await _questionService.DeleteQuestionAsync(id, userId);

                if (!success)
                {
                    return NotFound(new { message = "Question not found or access denied" });
                }

                _logger.LogInformation("Question deleted: {QuestionId} by user: {UserId}", id, userId);

                return Ok(new { message = "Question deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question: {QuestionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate questions using AI for specific content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="request">AI generation request</param>
        /// <returns>Generated questions</returns>
        [HttpPost("generate/{contentId}")]
        [Authorize]
        public async Task<IActionResult> GenerateQuestionsForContent(string contentId, [FromBody] GenerateQuestionsRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (string.IsNullOrEmpty(contentId))
                {
                    return BadRequest(new { message = "Content ID is required" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "Generation request is required" });
                }

                var result = await _questionService.GenerateQuestionsAsync(contentId, userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found or access denied" });
                }

                _logger.LogInformation("AI questions generated for content: {ContentId} by user: {UserId}", contentId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions for content: {ContentId}", contentId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate question answer
        /// </summary>
        /// <param name="id">Question ID</param>
        /// <param name="request">Answer validation request</param>
        /// <returns>Validation result</returns>
        [HttpPost("{id}/validate")]
        public async Task<IActionResult> ValidateAnswer(string id, [FromBody] ValidateAnswerRequestDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Question ID is required" });
                }

                if (request == null || string.IsNullOrEmpty(request.UserAnswer))
                {
                    return BadRequest(new { message = "User answer is required" });
                }

                var result = await _questionService.ValidateAnswerAsync(id, request);

                if (result == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating answer for question: {QuestionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get random questions for learning session
        /// </summary>
        /// <param name="request">Random questions request</param>
        /// <returns>Random questions list</returns>
        [HttpPost("random")]
        [Authorize]
        public async Task<IActionResult> GetRandomQuestions([FromBody] GetRandomQuestionsRequestDTO request)
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
                    return BadRequest(new { message = "Request is required" });
                }

                var result = await _questionService.GetRandomQuestionsAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random questions");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
