using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Services
{
    public class ContentService : IContentService
    {
        private readonly MongoDBContext _context;
        private readonly IQuestionService _questionService;
        private readonly ILogger<ContentService> _logger;
        private readonly IConfiguration _configuration;

        public ContentService(
            MongoDBContext context,
            IQuestionService questionService,
            ILogger<ContentService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _questionService = questionService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ContentDTO?> CreateContentAsync(string userId, CreateContentRequestDTO request)
        {
            try
            {
                var content = new Content
                {
                    Id = ObjectId.GenerateNewId(),
                    UserId = ObjectId.Parse(userId),
                    Title = request.Title,
                    ContentText = request.Content,
                    LearningMode = request.LearningMode,
                    Tags = request.Tags,
                    IsPublic = request.IsPublic,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Contents.InsertOneAsync(content);

                _logger.LogInformation("Content created: {ContentId} by user: {UserId}", content.Id, userId);

                return MapToContentDTO(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating content for user: {UserId}", userId);
                return null;
            }
        }

        public async Task<ContentDTO?> GetContentByIdAsync(string contentId)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var objectId))
                {
                    return null;
                }

                var content = await _context.Contents
                    .Find(c => c.Id == objectId)
                    .FirstOrDefaultAsync();

                if (content == null)
                {
                    return null;
                }

                var contentDto = MapToContentDTO(content);

                // Get question count and statistics
                var questionCount = await _context.Questions
                    .CountDocumentsAsync(q => q.ContentId == objectId);

                contentDto.TotalQuestions = (int)questionCount;

                // Get learning statistics
                var sessions = await _context.LearningSessions
                    .Find(s => s.ContentId == contentId && s.Status == "completed")
                    .ToListAsync();

                if (sessions.Any())
                {
                    contentDto.TimesStudied = sessions.Count;
                    contentDto.AverageScore = sessions.Average(s => s.Score);
                }

                return contentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content: {ContentId}", contentId);
                return null;
            }
        }

        public async Task<GetContentsResponseDTO> GetUserContentsAsync(string userId, GetContentsRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId))
                {
                    return new GetContentsResponseDTO();
                }

                var builder = Builders<Content>.Filter;
                var filter = builder.Eq(c => c.UserId, userObjectId);

                // Apply search filters
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchFilter = builder.Or(
                        builder.Regex("title", new BsonRegularExpression(request.SearchTerm, "i")),
                        builder.Regex("contentText", new BsonRegularExpression(request.SearchTerm, "i"))
                    );
                    filter = builder.And(filter, searchFilter);
                }

                if (request.Tags?.Any() == true)
                {
                    var tagFilter = builder.AnyIn("tags", request.Tags);
                    filter = builder.And(filter, tagFilter);
                }

                if (!string.IsNullOrEmpty(request.LearningMode))
                {
                    var modeFilter = builder.Eq(c => c.LearningMode, request.LearningMode);
                    filter = builder.And(filter, modeFilter);
                }

                // Get total count
                var totalCount = await _context.Contents.CountDocumentsAsync(filter);

                // Apply sorting
                var sort = GetSortDefinition(request.SortBy, request.SortOrder);

                // Get paginated results
                var contents = await _context.Contents
                    .Find(filter)
                    .Sort(sort)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Limit(request.PageSize)
                    .ToListAsync();

                var contentDtos = new List<ContentDTO>();
                foreach (var content in contents)
                {
                    var dto = MapToContentDTO(content);
                    
                    // Add statistics
                    var questionCount = await _context.Questions
                        .CountDocumentsAsync(q => q.ContentId == content.Id);
                    dto.TotalQuestions = (int)questionCount;

                    var sessions = await _context.LearningSessions
                        .Find(s => s.ContentId == content.Id.ToString() && s.Status == "completed")
                        .ToListAsync();

                    if (sessions.Any())
                    {
                        dto.TimesStudied = sessions.Count;
                        dto.AverageScore = sessions.Average(s => s.Score);
                    }

                    contentDtos.Add(dto);
                }

                return new GetContentsResponseDTO
                {
                    Contents = contentDtos,
                    TotalCount = (int)totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contents for user: {UserId}", userId);
                return new GetContentsResponseDTO();
            }
        }

        public async Task<ContentDTO?> UpdateContentAsync(string contentId, string userId, UpdateContentRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId) || 
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return null;
                }

                var filter = Builders<Content>.Filter.And(
                    Builders<Content>.Filter.Eq(c => c.Id, contentObjectId),
                    Builders<Content>.Filter.Eq(c => c.UserId, userObjectId)
                );

                var update = Builders<Content>.Update
                    .Set(c => c.Title, request.Title)
                    .Set(c => c.ContentText, request.Content)
                    .Set(c => c.Tags, request.Tags)
                    .Set(c => c.IsPublic, request.IsPublic)
                    .Set(c => c.UpdatedAt, DateTime.UtcNow);

                var result = await _context.Contents.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<Content> { ReturnDocument = ReturnDocument.After }
                );

                if (result == null)
                {
                    return null;
                }

                _logger.LogInformation("Content updated: {ContentId} by user: {UserId}", contentId, userId);

                return MapToContentDTO(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content: {ContentId}", contentId);
                return null;
            }
        }

        public async Task<bool> DeleteContentAsync(string contentId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId) || 
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return false;
                }

                var filter = Builders<Content>.Filter.And(
                    Builders<Content>.Filter.Eq(c => c.Id, contentObjectId),
                    Builders<Content>.Filter.Eq(c => c.UserId, userObjectId)
                );

                var result = await _context.Contents.DeleteOneAsync(filter);

                if (result.DeletedCount > 0)
                {
                    // Also delete associated questions
                    await _context.Questions.DeleteManyAsync(q => q.ContentId == contentObjectId);

                    _logger.LogInformation("Content deleted: {ContentId} by user: {UserId}", contentId, userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting content: {ContentId}", contentId);
                return false;
            }
        }

        public async Task<GenerateQuestionsResponseDTO?> GenerateQuestionsAsync(string contentId, string userId, GenerateQuestionsRequestDTO request)
        {
            try
            {
                // Check if user has access to content
                if (!await HasAccessToContentAsync(contentId, userId))
                {
                    return null;
                }

                // Get content
                var content = await GetContentByIdAsync(contentId);
                if (content == null)
                {
                    return null;
                }

                // Use QuestionService to generate questions
                var result = await _questionService.GenerateQuestionsAsync(contentId, userId, request);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions for content: {ContentId}", contentId);
                return new GenerateQuestionsResponseDTO
                {
                    Success = false,
                    Message = "Failed to generate questions"
                };
            }
        }

        public async Task<GetPublicContentsResponseDTO> GetPublicContentsAsync(GetPublicContentsRequestDTO request)
        {
            try
            {
                var builder = Builders<Content>.Filter;
                var filter = builder.Eq(c => c.IsPublic, true);

                // Apply search filters
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchFilter = builder.Or(
                        builder.Regex("title", new BsonRegularExpression(request.SearchTerm, "i")),
                        builder.Regex("contentText", new BsonRegularExpression(request.SearchTerm, "i"))
                    );
                    filter = builder.And(filter, searchFilter);
                }

                if (request.Tags?.Any() == true)
                {
                    var tagFilter = builder.AnyIn("tags", request.Tags);
                    filter = builder.And(filter, tagFilter);
                }

                if (!string.IsNullOrEmpty(request.LearningMode))
                {
                    var modeFilter = builder.Eq(c => c.LearningMode, request.LearningMode);
                    filter = builder.And(filter, modeFilter);
                }

                // Get total count
                var totalCount = await _context.Contents.CountDocumentsAsync(filter);

                // Apply sorting
                var sort = GetSortDefinition(request.SortBy, request.SortOrder);

                // Get paginated results
                var contents = await _context.Contents
                    .Find(filter)
                    .Sort(sort)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Limit(request.PageSize)
                    .ToListAsync();

                var contentDtos = new List<ContentDTO>();
                foreach (var content in contents)
                {
                    var dto = MapToContentDTO(content);
                    
                    // Add statistics for public contents
                    var questionCount = await _context.Questions
                        .CountDocumentsAsync(q => q.ContentId == content.Id);
                    dto.TotalQuestions = (int)questionCount;

                    var sessions = await _context.LearningSessions
                        .Find(s => s.ContentId == content.Id.ToString() && s.Status == "completed")
                        .ToListAsync();

                    if (sessions.Any())
                    {
                        dto.TimesStudied = sessions.Count;
                        dto.AverageScore = sessions.Average(s => s.Score);
                    }

                    contentDtos.Add(dto);
                }

                return new GetPublicContentsResponseDTO
                {
                    Contents = contentDtos,
                    TotalCount = (int)totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public contents");
                return new GetPublicContentsResponseDTO();
            }
        }

        public async Task<bool> HasAccessToContentAsync(string contentId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId) || 
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return false;
                }

                var content = await _context.Contents
                    .Find(c => c.Id == contentObjectId)
                    .FirstOrDefaultAsync();

                if (content == null)
                {
                    return false;
                }

                // User has access if they own the content or it's public
                return content.UserId == userObjectId || content.IsPublic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content access: {ContentId}", contentId);
                return false;
            }
        }

        public async Task<ContentStatisticsDTO?> GetContentStatisticsAsync(string contentId, string userId)
        {
            try
            {
                if (!await HasAccessToContentAsync(contentId, userId))
                {
                    return null;
                }

                if (!ObjectId.TryParse(contentId, out var contentObjectId))
                {
                    return null;
                }

                var questionCount = await _context.Questions
                    .CountDocumentsAsync(q => q.ContentId == contentObjectId);

                var sessions = await _context.LearningSessions
                    .Find(s => s.ContentId == contentId && s.Status == "completed")
                    .ToListAsync();

                var statistics = new ContentStatisticsDTO
                {
                    ContentId = contentId,
                    TotalQuestions = (int)questionCount
                };

                if (sessions.Any())
                {
                    statistics.TimesStudied = sessions.Count;
                    statistics.AverageScore = sessions.Average(s => s.Score);
                    statistics.TotalStudyTime = sessions.Sum(s => s.TotalTimeSeconds);
                    statistics.LastStudied = sessions.Max(s => s.CompletedAt ?? DateTime.MinValue);

                    // Question type breakdown
                    var questions = await _context.Questions
                        .Find(q => q.ContentId == contentObjectId)
                        .ToListAsync();

                    statistics.QuestionTypeBreakdown = questions
                        .GroupBy(q => q.Type)
                        .ToDictionary(g => g.Key, g => g.Count());
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content statistics: {ContentId}", contentId);
                return null;
            }
        }

        #region Private Helper Methods

        private ContentDTO MapToContentDTO(Content content)
        {
            return new ContentDTO
            {
                Id = content.Id.ToString(),
                UserId = content.UserId.ToString(),
                Title = content.Title,
                Content = content.ContentText,
                LearningMode = content.LearningMode,
                Tags = content.Tags,
                IsPublic = content.IsPublic,
                CreatedAt = content.CreatedAt,
                UpdatedAt = content.UpdatedAt
            };
        }

        private SortDefinition<Content> GetSortDefinition(string sortBy, string sortOrder)
        {
            var ascending = sortOrder.ToLower() == "asc";

            return sortBy.ToLower() switch
            {
                "title" => ascending ? 
                    Builders<Content>.Sort.Ascending(c => c.Title) : 
                    Builders<Content>.Sort.Descending(c => c.Title),
                "updatedat" => ascending ? 
                    Builders<Content>.Sort.Ascending(c => c.UpdatedAt) : 
                    Builders<Content>.Sort.Descending(c => c.UpdatedAt),
                _ => ascending ? 
                    Builders<Content>.Sort.Ascending(c => c.CreatedAt) : 
                    Builders<Content>.Sort.Descending(c => c.CreatedAt)
            };
        }

        #endregion
    }
}
