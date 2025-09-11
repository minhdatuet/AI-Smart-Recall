using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecallAPI.Services
{
    /// <summary>
    /// Content service interface for managing learning content
    /// </summary>
    public interface IContentService
    {
        /// <summary>
        /// Create new content
        /// </summary>
        /// <param name="userId">User ID creating the content</param>
        /// <param name="request">Content creation request</param>
        /// <returns>Created content information</returns>
        Task<ContentDTO?> CreateContentAsync(string userId, CreateContentRequestDTO request);

        /// <summary>
        /// Get content by ID
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <returns>Content details or null if not found</returns>
        Task<ContentDTO?> GetContentByIdAsync(string contentId);

        /// <summary>
        /// Get user's contents with pagination and filtering
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Get contents request with filters</param>
        /// <returns>Paginated content list</returns>
        Task<GetContentsResponseDTO> GetUserContentsAsync(string userId, GetContentsRequestDTO request);

        /// <summary>
        /// Update existing content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID updating the content</param>
        /// <param name="request">Content update request</param>
        /// <returns>Updated content information or null if not found/access denied</returns>
        Task<ContentDTO?> UpdateContentAsync(string contentId, string userId, UpdateContentRequestDTO request);

        /// <summary>
        /// Delete content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID deleting the content</param>
        /// <returns>True if deleted successfully, false if not found/access denied</returns>
        Task<bool> DeleteContentAsync(string contentId, string userId);

        /// <summary>
        /// Generate questions for content using AI
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID requesting generation</param>
        /// <param name="request">Question generation request</param>
        /// <returns>Generated questions or null if content not found/access denied</returns>
        Task<GenerateQuestionsResponseDTO?> GenerateQuestionsAsync(string contentId, string userId, GenerateQuestionsRequestDTO request);

        /// <summary>
        /// Get public contents with pagination
        /// </summary>
        /// <param name="request">Get public contents request</param>
        /// <returns>Paginated public content list</returns>
        Task<GetPublicContentsResponseDTO> GetPublicContentsAsync(GetPublicContentsRequestDTO request);

        /// <summary>
        /// Check if user has access to content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasAccessToContentAsync(string contentId, string userId);

        /// <summary>
        /// Get content statistics
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID requesting statistics</param>
        /// <returns>Content statistics or null if not found/access denied</returns>
        Task<ContentStatisticsDTO?> GetContentStatisticsAsync(string contentId, string userId);
    }
}
