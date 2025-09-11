using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecallAPI.Services
{
    /// <summary>
    /// Question service interface for managing learning questions
    /// </summary>
    public interface IQuestionService
    {
        /// <summary>
        /// Create new question manually
        /// </summary>
        /// <param name="userId">User ID creating the question</param>
        /// <param name="request">Question creation request</param>
        /// <returns>Created question information or null if content not found/access denied</returns>
        Task<QuestionDTO?> CreateQuestionAsync(string userId, CreateQuestionRequestDTO request);

        /// <summary>
        /// Get question by ID
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <returns>Question details or null if not found</returns>
        Task<QuestionDTO?> GetQuestionByIdAsync(string questionId);

        /// <summary>
        /// Get questions for specific content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="request">Get questions request with filters</param>
        /// <returns>List of questions</returns>
        Task<GetQuestionsResponseDTO> GetQuestionsByContentAsync(string contentId, GetQuestionsRequestDTO request);

        /// <summary>
        /// Update existing question
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="userId">User ID updating the question</param>
        /// <param name="request">Question update request</param>
        /// <returns>Updated question information or null if not found/access denied</returns>
        Task<QuestionDTO?> UpdateQuestionAsync(string questionId, string userId, UpdateQuestionRequestDTO request);

        /// <summary>
        /// Delete question
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="userId">User ID deleting the question</param>
        /// <returns>True if deleted successfully, false if not found/access denied</returns>
        Task<bool> DeleteQuestionAsync(string questionId, string userId);

        /// <summary>
        /// Generate questions using AI for specific content
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID requesting generation</param>
        /// <param name="request">AI generation request</param>
        /// <returns>Generated questions or null if content not found/access denied</returns>
        Task<GenerateQuestionsResponseDTO?> GenerateQuestionsAsync(string contentId, string userId, GenerateQuestionsRequestDTO request);

        /// <summary>
        /// Validate question answer
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="request">Answer validation request</param>
        /// <returns>Validation result or null if question not found</returns>
        Task<ValidateAnswerResponseDTO?> ValidateAnswerAsync(string questionId, ValidateAnswerRequestDTO request);

        /// <summary>
        /// Get random questions for learning session
        /// </summary>
        /// <param name="userId">User ID requesting questions</param>
        /// <param name="request">Random questions request</param>
        /// <returns>Random questions list</returns>
        Task<GetRandomQuestionsResponseDTO> GetRandomQuestionsAsync(string userId, GetRandomQuestionsRequestDTO request);

        /// <summary>
        /// Bulk create questions from generation
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <param name="userId">User ID creating questions</param>
        /// <param name="questions">List of questions to create</param>
        /// <returns>List of created questions</returns>
        Task<List<QuestionDTO>> BulkCreateQuestionsAsync(string contentId, string userId, List<QuestionDTO> questions);

        /// <summary>
        /// Get questions by multiple IDs
        /// </summary>
        /// <param name="questionIds">List of question IDs</param>
        /// <returns>List of questions</returns>
        Task<List<QuestionDTO>> GetQuestionsByIdsAsync(List<string> questionIds);

        /// <summary>
        /// Check if user has access to question (through content ownership)
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasAccessToQuestionAsync(string questionId, string userId);
    }
}
