using AISmartRecallAPI.Models;
using MongoDB.Bson;

namespace AISmartRecallAPI.Repositories
{
    public interface IContentRepository : IBaseRepository<Content>
    {
        Task<IEnumerable<Content>> GetByUserIdAsync(ObjectId userId);
        Task<IEnumerable<Content>> GetPublicContentsAsync();
        Task<IEnumerable<Content>> SearchContentsAsync(string searchTerm, ObjectId? userId = null, bool publicOnly = false);
        Task<IEnumerable<Content>> GetContentsByTagsAsync(List<string> tags, ObjectId? userId = null, bool publicOnly = false);
        Task<IEnumerable<Content>> GetContentsByLearningModeAsync(string learningMode, ObjectId? userId = null, bool publicOnly = false);
        Task<IEnumerable<Content>> GetUserContentsPaginatedAsync(ObjectId userId, int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? learningMode = null, string? sortBy = null, string? sortOrder = null);
        Task<IEnumerable<Content>> GetPublicContentsPaginatedAsync(int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? learningMode = null, string? sortBy = null, string? sortOrder = null);
        Task<long> GetTotalCountAsync(ObjectId? userId = null, bool publicOnly = false, string? searchTerm = null, List<string>? tags = null, string? learningMode = null);
        Task<IEnumerable<string>> GetAllTagsAsync(ObjectId? userId = null);
        Task<IEnumerable<string>> GetAllLearningModesAsync(ObjectId? userId = null);
        Task<bool> IsContentOwnedByUserAsync(ObjectId contentId, ObjectId userId);
        Task<IEnumerable<Content>> GetRecentContentsAsync(ObjectId userId, int count = 10);
        Task<IEnumerable<Content>> GetPopularPublicContentsAsync(int count = 10);
    }
}
