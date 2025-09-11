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
    public class ContentController : BaseController
    {
        private readonly IContentService _contentService;
        private readonly ILogger<ContentController> _logger;

        public ContentController(
            IContentService contentService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration config,
            MongoDBContext dbContext,
            ILogger<ContentController> logger
        ) : base(httpContextAccessor, config, dbContext, logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        /// <summary>
        /// Create new content
        /// </summary>
        /// <param name="request">Content creation request</param>
        /// <returns>Created content information</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateContent([FromBody] CreateContentRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Validate request
                if (request == null || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Content))
                {
                    return BadRequest(new { message = "Title and content are required" });
                }

                var result = await _contentService.CreateContentAsync(userId, request);
                
                _logger.LogInformation("Content created by user: {UserId}, ContentId: {ContentId}", userId, result?.Id);
                
                return CreatedAtAction(nameof(GetContent), new { id = result?.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating content");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get content by ID
        /// </summary>
        /// <param name="id">Content ID</param>
        /// <returns>Content details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContent(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Content ID is required" });
                }

                var content = await _contentService.GetContentByIdAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Content not found" });
                }

                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content: {ContentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user's contents with pagination and filtering
        /// </summary>
        /// <param name="request">Get contents request with filters</param>
        /// <returns>Paginated content list</returns>
        [HttpPost("search")]
        [Authorize]
        public async Task<IActionResult> GetUserContents([FromBody] GetContentsRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var result = await _contentService.GetUserContentsAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contents");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update existing content
        /// </summary>
        /// <param name="id">Content ID</param>
        /// <param name="request">Content update request</param>
        /// <returns>Updated content information</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateContent(string id, [FromBody] UpdateContentRequestDTO request)
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
                    return BadRequest(new { message = "Content ID is required" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "Update request is required" });
                }

                var result = await _contentService.UpdateContentAsync(id, userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found or access denied" });
                }

                _logger.LogInformation("Content updated: {ContentId} by user: {UserId}", id, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content: {ContentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete content
        /// </summary>
        /// <param name="id">Content ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteContent(string id)
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
                    return BadRequest(new { message = "Content ID is required" });
                }

                var success = await _contentService.DeleteContentAsync(id, userId);

                if (!success)
                {
                    return NotFound(new { message = "Content not found or access denied" });
                }

                _logger.LogInformation("Content deleted: {ContentId} by user: {UserId}", id, userId);

                return Ok(new { message = "Content deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting content: {ContentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate questions for content using AI
        /// </summary>
        /// <param name="id">Content ID</param>
        /// <param name="request">Question generation request</param>
        /// <returns>Generated questions</returns>
        [HttpPost("{id}/generate-questions")]
        [Authorize]
        public async Task<IActionResult> GenerateQuestions(string id, [FromBody] GenerateQuestionsRequestDTO request)
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
                    return BadRequest(new { message = "Content ID is required" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "Generation request is required" });
                }

                var result = await _contentService.GenerateQuestionsAsync(id, userId, request);

                if (result == null)
                {
                    return NotFound(new { message = "Content not found or access denied" });
                }

                _logger.LogInformation("Questions generated for content: {ContentId} by user: {UserId}", id, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions for content: {ContentId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get public contents with pagination
        /// </summary>
        /// <param name="request">Get public contents request</param>
        /// <returns>Paginated public content list</returns>
        [HttpPost("public")]
        public async Task<IActionResult> GetPublicContents([FromBody] GetPublicContentsRequestDTO request)
        {
            try
            {
                var result = await _contentService.GetPublicContentsAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public contents");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
