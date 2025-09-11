using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using AISmartRecallAPI.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace AISmartRecallAPI.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly MongoDBContext _context;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger<QuestionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public QuestionService(
            MongoDBContext context,
            IContentRepository contentRepository,
            ILogger<QuestionService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _context = context;
            _contentRepository = contentRepository;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<QuestionDTO?> CreateQuestionAsync(string userId, CreateQuestionRequestDTO request)
        {
            try
            {
                // Check if user has access to content
                if (!await HasAccessToContentAsync(request.ContentId, userId))
                {
                    return null;
                }

                if (!ObjectId.TryParse(request.ContentId, out var contentObjectId))
                {
                    return null;
                }

                var question = new Question
                {
                    Id = ObjectId.GenerateNewId(),
                    ContentId = contentObjectId,
                    Type = request.Type,
                    QuestionText = request.Question,
                    Options = request.Options,
                    CorrectAnswer = request.CorrectAnswer,
                    Explanation = request.Explanation,
                    Difficulty = request.Difficulty,
                    AIProvider = "manual", // Manual creation
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Questions.InsertOneAsync(question);

                _logger.LogInformation("Question created: {QuestionId} by user: {UserId}", question.Id, userId);

                return MapToQuestionDTO(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question for user: {UserId}", userId);
                return null;
            }
        }

        public async Task<QuestionDTO?> GetQuestionByIdAsync(string questionId)
        {
            try
            {
                if (!ObjectId.TryParse(questionId, out var objectId))
                {
                    return null;
                }

                var question = await _context.Questions
                    .Find(q => q.Id == objectId)
                    .FirstOrDefaultAsync();

                return question == null ? null : MapToQuestionDTO(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question: {QuestionId}", questionId);
                return null;
            }
        }

        public async Task<GetQuestionsResponseDTO> GetQuestionsByContentAsync(string contentId, GetQuestionsRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId))
                {
                    return new GetQuestionsResponseDTO();
                }

                var builder = Builders<Question>.Filter;
                var filter = builder.Eq(q => q.ContentId, contentObjectId);

                // Apply question type filters
                if (request.QuestionTypes?.Any() == true)
                {
                    var typeFilter = builder.In(q => q.Type, request.QuestionTypes);
                    filter = builder.And(filter, typeFilter);
                }

                // Get total count
                var totalCount = await _context.Questions.CountDocumentsAsync(filter);

                // Apply limit if specified
                var findFluent = _context.Questions.Find(filter);
                if (request.Limit.HasValue && request.Limit.Value > 0)
                {
                    findFluent = findFluent.Limit(request.Limit.Value);
                }

                var questions = await findFluent.ToListAsync();

                return new GetQuestionsResponseDTO
                {
                    Questions = questions.Select(MapToQuestionDTO).ToList(),
                    TotalCount = (int)totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for content: {ContentId}", contentId);
                return new GetQuestionsResponseDTO();
            }
        }

        public async Task<QuestionDTO?> UpdateQuestionAsync(string questionId, string userId, UpdateQuestionRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(questionId, out var questionObjectId))
                {
                    return null;
                }

                // Check if user has access to this question (through content ownership)
                if (!await HasAccessToQuestionAsync(questionId, userId))
                {
                    return null;
                }

                var update = Builders<Question>.Update
                    .Set(q => q.Type, request.Type)
                    .Set(q => q.QuestionText, request.Question)
                    .Set(q => q.Options, request.Options)
                    .Set(q => q.CorrectAnswer, request.CorrectAnswer)
                    .Set(q => q.Explanation, request.Explanation)
                    .Set(q => q.Difficulty, request.Difficulty);

                var filter = Builders<Question>.Filter.Eq(q => q.Id, questionObjectId);
                var result = await _context.Questions.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<Question> { ReturnDocument = ReturnDocument.After }
                );

                if (result == null)
                {
                    return null;
                }

                _logger.LogInformation("Question updated: {QuestionId} by user: {UserId}", questionId, userId);

                return MapToQuestionDTO(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question: {QuestionId}", questionId);
                return null;
            }
        }

        public async Task<bool> DeleteQuestionAsync(string questionId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(questionId, out var questionObjectId))
                {
                    return false;
                }

                // Check if user has access to this question
                if (!await HasAccessToQuestionAsync(questionId, userId))
                {
                    return false;
                }

                var result = await _context.Questions.DeleteOneAsync(q => q.Id == questionObjectId);

                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation("Question deleted: {QuestionId} by user: {UserId}", questionId, userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question: {QuestionId}", questionId);
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

                // Get content details
                var content = await GetContentDtoByIdAsync(contentId);
                if (content == null)
                {
                    return null;
                }

                // Generate questions using AI (placeholder implementation)
                var generatedQuestions = await GenerateQuestionsWithAI(content, request);

                if (generatedQuestions?.Any() == true)
                {
                    // Save generated questions to database
                    var savedQuestions = await BulkCreateQuestionsAsync(contentId, userId, generatedQuestions);

                    _logger.LogInformation("Generated {Count} questions for content: {ContentId}", savedQuestions.Count, contentId);

                    return new GenerateQuestionsResponseDTO
                    {
                        Questions = savedQuestions,
                        Success = true,
                        Message = $"Successfully generated {savedQuestions.Count} questions",
                        AIProvider = request.AIProvider
                    };
                }

                return new GenerateQuestionsResponseDTO
                {
                    Success = false,
                    Message = "Failed to generate questions",
                    AIProvider = request.AIProvider
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions for content: {ContentId}", contentId);
                return new GenerateQuestionsResponseDTO
                {
                    Success = false,
                    Message = "Internal error occurred while generating questions"
                };
            }
        }

        public async Task<ValidateAnswerResponseDTO?> ValidateAnswerAsync(string questionId, ValidateAnswerRequestDTO request)
        {
            try
            {
                var question = await GetQuestionByIdAsync(questionId);
                if (question == null)
                {
                    return null;
                }

                var userAnswer = request.UserAnswer;
                var correctAnswer = question.CorrectAnswer;

                // Apply text processing options
                if (request.TrimWhitespace)
                {
                    userAnswer = userAnswer.Trim();
                    correctAnswer = correctAnswer.Trim();
                }

                if (!request.CaseSensitive)
                {
                    userAnswer = userAnswer.ToLowerInvariant();
                    correctAnswer = correctAnswer.ToLowerInvariant();
                }

                // Basic validation
                var isCorrect = userAnswer == correctAnswer;
                var similarity = CalculateSimilarity(userAnswer, correctAnswer);

                return new ValidateAnswerResponseDTO
                {
                    IsCorrect = isCorrect,
                    CorrectAnswer = question.CorrectAnswer,
                    Explanation = question.Explanation,
                    UserAnswer = request.UserAnswer,
                    Similarity = similarity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating answer for question: {QuestionId}", questionId);
                return null;
            }
        }

        public async Task<GetRandomQuestionsResponseDTO> GetRandomQuestionsAsync(string userId, GetRandomQuestionsRequestDTO request)
        {
            try
            {
                var builder = Builders<Question>.Filter;
                var filters = new List<FilterDefinition<Question>>();

                // Filter by content IDs if specified
                if (request.ContentIds?.Any() == true)
                {
                    var contentObjectIds = request.ContentIds
                        .Where(id => ObjectId.TryParse(id, out _))
                        .Select(ObjectId.Parse)
                        .ToList();

                    if (contentObjectIds.Any())
                    {
                        filters.Add(builder.In(q => q.ContentId, contentObjectIds));
                    }
                }

                // Filter by question types
                if (request.QuestionTypes?.Any() == true)
                {
                    filters.Add(builder.In(q => q.Type, request.QuestionTypes));
                }

                // Filter by difficulty range
                if (request.MinDifficulty.HasValue)
                {
                    filters.Add(builder.Gte(q => q.Difficulty, request.MinDifficulty.Value));
                }

                if (request.MaxDifficulty.HasValue)
                {
                    filters.Add(builder.Lte(q => q.Difficulty, request.MaxDifficulty.Value));
                }

                // Exclude specific questions
                if (request.ExcludeQuestionIds?.Any() == true)
                {
                    var excludeObjectIds = request.ExcludeQuestionIds
                        .Where(id => ObjectId.TryParse(id, out _))
                        .Select(ObjectId.Parse)
                        .ToList();

                    if (excludeObjectIds.Any())
                    {
                        filters.Add(builder.Nin(q => q.Id, excludeObjectIds));
                    }
                }

                // Combine all filters
                var finalFilter = filters.Any() ? builder.And(filters) : builder.Empty;

                // Get total available count
                var totalAvailable = await _context.Questions.CountDocumentsAsync(finalFilter);

                // Get random questions
                var questions = await _context.Questions
                    .Find(finalFilter)
                    .Limit(request.Count * 2) // Get more than needed for randomization
                    .ToListAsync();

                // Randomize and take requested count
                var random = new Random();
                var selectedQuestions = questions
                    .OrderBy(q => random.Next())
                    .Take(request.Count)
                    .Select(MapToQuestionDTO)
                    .ToList();

                return new GetRandomQuestionsResponseDTO
                {
                    Questions = selectedQuestions,
                    TotalAvailable = (int)totalAvailable
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random questions");
                return new GetRandomQuestionsResponseDTO();
            }
        }

        public async Task<List<QuestionDTO>> BulkCreateQuestionsAsync(string contentId, string userId, List<QuestionDTO> questions)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId))
                {
                    return new List<QuestionDTO>();
                }

                var questionModels = questions.Select(dto => new Question
                {
                    Id = ObjectId.GenerateNewId(),
                    ContentId = contentObjectId,
                    Type = dto.Type,
                    QuestionText = dto.Question,
                    Options = dto.Options,
                    CorrectAnswer = dto.CorrectAnswer,
                    Explanation = dto.Explanation,
                    Difficulty = dto.Difficulty,
                    AIProvider = dto.AIProvider,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _context.Questions.InsertManyAsync(questionModels);

                _logger.LogInformation("Bulk created {Count} questions for content: {ContentId}", questionModels.Count, contentId);

                return questionModels.Select(MapToQuestionDTO).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating questions for content: {ContentId}", contentId);
                return new List<QuestionDTO>();
            }
        }

        public async Task<List<QuestionDTO>> GetQuestionsByIdsAsync(List<string> questionIds)
        {
            try
            {
                var objectIds = questionIds
                    .Where(id => ObjectId.TryParse(id, out _))
                    .Select(ObjectId.Parse)
                    .ToList();

                if (!objectIds.Any())
                {
                    return new List<QuestionDTO>();
                }

                var questions = await _context.Questions
                    .Find(q => objectIds.Contains(q.Id))
                    .ToListAsync();

                return questions.Select(MapToQuestionDTO).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions by IDs");
                return new List<QuestionDTO>();
            }
        }

        public async Task<bool> HasAccessToQuestionAsync(string questionId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(questionId, out var questionObjectId))
                {
                    return false;
                }

                var question = await _context.Questions
                    .Find(q => q.Id == questionObjectId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    return false;
                }

                // Check access through content ownership
                return await HasAccessToContentAsync(question.ContentId.ToString(), userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking question access: {QuestionId}", questionId);
                return false;
            }
        }

        #region Private Helper Methods

        private QuestionDTO MapToQuestionDTO(Question question)
        {
            return new QuestionDTO
            {
                Id = question.Id.ToString(),
                ContentId = question.ContentId.ToString(),
                Type = question.Type,
                Question = question.QuestionText,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Explanation = question.Explanation,
                Difficulty = question.Difficulty,
                AIProvider = question.AIProvider,
                CreatedAt = question.CreatedAt
            };
        }

        private async Task<List<QuestionDTO>?> GenerateQuestionsWithAI(ContentDTO content, GenerateQuestionsRequestDTO request)
        {
            try
            {
                // This is a placeholder implementation
                // In a real implementation, you would call your AI service here
                // For now, we'll return some sample questions

                var questions = new List<QuestionDTO>();

                for (int i = 1; i <= Math.Min(request.QuestionCount, 5); i++)
                {
                    var questionTypes = request.QuestionTypes.Any() ? request.QuestionTypes : new List<string> { QuestionTypes.MultipleChoice };
                    var randomType = questionTypes[new Random().Next(questionTypes.Count)];

                    var question = new QuestionDTO
                    {
                        ContentId = content.Id,
                        Type = randomType,
                        Question = $"Sample question {i} about: {content.Title}",
                        CorrectAnswer = $"Answer {i}",
                        Explanation = $"This is explanation for question {i}",
                        Difficulty = new Random().Next(1, 6),
                        AIProvider = request.AIProvider,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (randomType == QuestionTypes.MultipleChoice)
                    {
                        question.Options = new List<string>
                        {
                            $"Answer {i}",
                            $"Wrong answer {i}A",
                            $"Wrong answer {i}B",
                            $"Wrong answer {i}C"
                        };
                    }

                    questions.Add(question);
                }

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions with AI");
                return null;
            }
        }

        private double CalculateSimilarity(string answer1, string answer2)
        {
            // Simple similarity calculation using Levenshtein distance
            if (string.IsNullOrEmpty(answer1) && string.IsNullOrEmpty(answer2))
                return 1.0;

            if (string.IsNullOrEmpty(answer1) || string.IsNullOrEmpty(answer2))
                return 0.0;

            var maxLength = Math.Max(answer1.Length, answer2.Length);
            var distance = LevenshteinDistance(answer1, answer2);
            
            return 1.0 - (double)distance / maxLength;
        }

        private int LevenshteinDistance(string source, string target)
        {
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            var matrix = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }

        private async Task<bool> HasAccessToContentAsync(string contentId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId) ||
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return false;
                }

                var content = await _contentRepository.GetByIdAsync(contentObjectId);
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

        private async Task<ContentDTO?> GetContentDtoByIdAsync(string contentId)
        {
            try
            {
                if (!ObjectId.TryParse(contentId, out var contentObjectId))
                {
                    return null;
                }

                var content = await _contentRepository.GetByIdAsync(contentObjectId);
                if (content == null)
                {
                    return null;
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content: {ContentId}", contentId);
                return null;
            }
        }

        #endregion
    }
}
