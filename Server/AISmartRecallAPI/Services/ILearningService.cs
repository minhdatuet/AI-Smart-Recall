using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecallAPI.Services
{
    /// <summary>
    /// Learning service interface for managing learning sessions and progress
    /// </summary>
    public interface ILearningService
    {
        #region Learning Sessions

        /// <summary>
        /// Start new learning session
        /// </summary>
        /// <param name="userId">User ID starting the session</param>
        /// <param name="request">Learning session start request</param>
        /// <returns>Created learning session or null if content not found/no questions</returns>
        Task<LearningSessionDTO?> StartLearningSessionAsync(string userId, StartLearningSessionRequestDTO request);

        /// <summary>
        /// Submit answer for current question
        /// </summary>
        /// <param name="userId">User ID submitting answer</param>
        /// <param name="request">Answer submission request</param>
        /// <returns>Answer validation result or null if session not found</returns>
        Task<SubmitAnswerResponseDTO?> SubmitAnswerAsync(string userId, SubmitAnswerRequestDTO request);

        /// <summary>
        /// Complete learning session
        /// </summary>
        /// <param name="userId">User ID completing the session</param>
        /// <param name="request">Session completion request</param>
        /// <returns>Session completion result or null if session not found</returns>
        Task<CompleteLearningSessionResponseDTO?> CompleteLearningSessionAsync(string userId, CompleteLearningSessionRequestDTO request);

        /// <summary>
        /// Get active learning session for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Active session or null if none</returns>
        Task<LearningSessionDTO?> GetActiveLearningSessionAsync(string userId);

        #endregion

        #region Progress & Statistics

        /// <summary>
        /// Get user progress and statistics
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Progress request with filters</param>
        /// <returns>User progress and learning statistics</returns>
        Task<GetProgressResponseDTO> GetProgressAsync(string userId, GetProgressRequestDTO? request);

        /// <summary>
        /// Get learning statistics
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Statistics request with date range</param>
        /// <returns>Detailed learning statistics</returns>
        Task<LearningStatisticsDTO> GetLearningStatisticsAsync(string userId, GetProgressRequestDTO? request);

        /// <summary>
        /// Update user progress after session completion
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="session">Completed learning session</param>
        /// <returns>Updated user progress</returns>
        Task<UserProgressDTO> UpdateUserProgressAsync(string userId, LearningSessionDTO session);

        #endregion

        #region Room Learning

        /// <summary>
        /// Create learning room for group sessions
        /// </summary>
        /// <param name="userId">User ID creating the room (host)</param>
        /// <param name="request">Room creation request</param>
        /// <returns>Created room information or null if content not found</returns>
        Task<LearningRoomDTO?> CreateRoomAsync(string userId, CreateRoomRequestDTO request);

        /// <summary>
        /// Join learning room
        /// </summary>
        /// <param name="userId">User ID joining the room</param>
        /// <param name="request">Room join request</param>
        /// <returns>Join result with room information</returns>
        Task<JoinRoomResponseDTO> JoinRoomAsync(string userId, JoinRoomRequestDTO request);

        /// <summary>
        /// Start room session (host only)
        /// </summary>
        /// <param name="userId">User ID starting the session (must be host)</param>
        /// <param name="request">Room session start request</param>
        /// <returns>Started room session or null if room not found/access denied</returns>
        Task<LearningRoomDTO?> StartRoomSessionAsync(string userId, StartRoomSessionRequestDTO request);

        /// <summary>
        /// Get room leaderboard
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="userId">User ID requesting leaderboard</param>
        /// <returns>Room leaderboard or null if room not found/access denied</returns>
        Task<RoomLeaderboardDTO?> GetRoomLeaderboardAsync(string roomId, string userId);

        /// <summary>
        /// Leave learning room
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="userId">User ID leaving the room</param>
        /// <returns>True if left successfully, false if not in room</returns>
        Task<bool> LeaveRoomAsync(string roomId, string userId);

        /// <summary>
        /// Get room by ID
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="userId">User ID requesting room info</param>
        /// <returns>Room information or null if not found/access denied</returns>
        Task<LearningRoomDTO?> GetRoomAsync(string roomId, string userId);

        #endregion

        #region Utilities

        /// <summary>
        /// Generate room code for new rooms
        /// </summary>
        /// <returns>Unique 6-digit room code</returns>
        string GenerateRoomCode();

        /// <summary>
        /// Check if user is in room
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user is in room, false otherwise</returns>
        Task<bool> IsUserInRoomAsync(string roomId, string userId);

        /// <summary>
        /// Check if user is room host
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user is host, false otherwise</returns>
        Task<bool> IsUserRoomHostAsync(string roomId, string userId);

        #endregion
    }
}
