using AISmartRecallAPI.Models;
using MongoDB.Bson;

namespace AISmartRecallAPI.Repositories
{
    public interface ILearningSessionRepository : IBaseRepository<LearningSession>
    {
        Task<IEnumerable<LearningSession>> GetByUserIdAsync(ObjectId userId);
        Task<IEnumerable<LearningSession>> GetByContentIdAsync(ObjectId contentId);
        Task<IEnumerable<LearningSession>> GetByContentIdsAsync(List<ObjectId> contentIds);
        Task<IEnumerable<LearningSession>> GetByStatusAsync(string status, ObjectId? userId = null);
        Task<IEnumerable<LearningSession>> GetActiveLearningSessionsAsync(ObjectId userId);
        Task<LearningSession?> GetActiveSessionByRoomIdAsync(string roomId);
        Task<IEnumerable<LearningSession>> GetSessionsByRoomIdAsync(string roomId);
        Task<IEnumerable<LearningSession>> GetCompletedSessionsAsync(ObjectId userId, int? limit = null);
        Task<IEnumerable<LearningSession>> GetSessionsPaginatedAsync(ObjectId userId, int page, int pageSize, string? status = null, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<long> GetTotalSessionCountAsync(ObjectId userId, string? status = null, ObjectId? contentId = null);
        Task<double> GetAverageScoreAsync(ObjectId userId, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<long> GetTotalStudyTimeAsync(ObjectId userId, ObjectId? contentId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, long>> GetSessionStatusDistributionAsync(ObjectId userId);
        Task<Dictionary<DateTime, long>> GetStudyActivityAsync(ObjectId userId, int days = 30);
        Task<IEnumerable<LearningSession>> GetTopScoringSessionsAsync(ObjectId userId, int count = 10, ObjectId? contentId = null);
        Task<IEnumerable<LearningSession>> GetRecentSessionsAsync(ObjectId userId, int count = 10);
        Task<bool> HasActiveSessionAsync(ObjectId userId, ObjectId contentId);
        Task<LearningSession?> GetLastSessionAsync(ObjectId userId, ObjectId contentId);
        Task<Dictionary<ObjectId, SessionStats>> GetContentSessionStatsAsync(ObjectId userId, List<ObjectId> contentIds);
        Task<bool> DeleteSessionsByContentIdAsync(ObjectId contentId);
    }

    public class SessionStats
    {
        public long TotalSessions { get; set; }
        public long CompletedSessions { get; set; }
        public double AverageScore { get; set; }
        public long TotalStudyTime { get; set; }
        public DateTime? LastStudied { get; set; }
    }
}
