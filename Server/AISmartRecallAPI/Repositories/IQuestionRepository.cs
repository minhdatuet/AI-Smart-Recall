using AISmartRecallAPI.Models;
using MongoDB.Bson;

namespace AISmartRecallAPI.Repositories
{
    public interface IQuestionRepository : IBaseRepository<Question>
    {
        Task<IEnumerable<Question>> GetByContentIdAsync(ObjectId contentId);
        Task<IEnumerable<Question>> GetByContentIdsAsync(List<ObjectId> contentIds);
        Task<IEnumerable<Question>> GetByTypeAsync(string questionType, ObjectId? contentId = null);
        Task<IEnumerable<Question>> GetByDifficultyRangeAsync(int minDifficulty, int maxDifficulty, ObjectId? contentId = null);
        Task<IEnumerable<Question>> GetRandomQuestionsAsync(int count, List<ObjectId>? contentIds = null, List<string>? questionTypes = null, int? minDifficulty = null, int? maxDifficulty = null, List<ObjectId>? excludeIds = null);
        Task<long> GetCountByContentIdAsync(ObjectId contentId);
        Task<long> GetTotalCountByUserAsync(ObjectId userId);
        Task<IEnumerable<Question>> GetQuestionsPaginatedAsync(ObjectId contentId, int page, int pageSize, List<string>? questionTypes = null);
        Task<bool> IsQuestionOwnedByUserAsync(ObjectId questionId, ObjectId userId);
        Task<Dictionary<string, long>> GetQuestionTypeDistributionAsync(ObjectId? contentId = null, ObjectId? userId = null);
        Task<Dictionary<int, long>> GetDifficultyDistributionAsync(ObjectId? contentId = null, ObjectId? userId = null);
        Task<IEnumerable<Question>> GetQuestionsByAIProviderAsync(string aiProvider, ObjectId? contentId = null);
        Task<bool> DeleteByContentIdAsync(ObjectId contentId);
        Task<long> GetCountByAIProviderAsync(string aiProvider, ObjectId? contentId = null);
    }
}
