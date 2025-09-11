using AISmartRecall.SharedModels.DTOs;
using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Services
{
    public class LearningService : ILearningService
    {
        private readonly MongoDBContext _context;
        private readonly IContentService _contentService;
        private readonly IQuestionService _questionService;
        private readonly IUserService _userService;
        private readonly ILogger<LearningService> _logger;
        private readonly IConfiguration _configuration;

        public LearningService(
            MongoDBContext context,
            IContentService contentService,
            IQuestionService questionService,
            IUserService userService,
            ILogger<LearningService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _contentService = contentService;
            _questionService = questionService;
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
        }

        #region Learning Sessions

        public async Task<LearningSessionDTO?> StartLearningSessionAsync(string userId, StartLearningSessionRequestDTO request)
        {
            try
            {
                // Check if user has access to content
                if (!await _contentService.HasAccessToContentAsync(request.ContentId, userId))
                {
                    return null;
                }

                // Get questions for the content
                var questionsRequest = new GetQuestionsRequestDTO { ContentId = request.ContentId };
                var questionsResponse = await _questionService.GetQuestionsByContentAsync(request.ContentId, questionsRequest);

                var availableQuestions = questionsResponse.Questions;
                if (!availableQuestions.Any())
                {
                    return null; // No questions available
                }

                // Select questions based on request
                var selectedQuestions = new List<QuestionDTO>();
                
                if (request.QuestionIds?.Any() == true)
                {
                    // Use specific question IDs
                    selectedQuestions = await _questionService.GetQuestionsByIdsAsync(request.QuestionIds);
                }
                else
                {
                    // Use all available questions or random selection
                    selectedQuestions = availableQuestions;
                }

                if (!selectedQuestions.Any())
                {
                    return null;
                }

                // Create learning session
                var session = new LearningSession
                {
                    Id = ObjectId.GenerateNewId(),
                    UserId = ObjectId.Parse(userId),
                    ContentId = request.ContentId,
                    RoomId = request.RoomId,
                    SessionType = request.SessionType,
                    TotalQuestions = selectedQuestions.Count,
                    CorrectAnswers = 0,
                    Score = 0.0,
                    TotalTimeSeconds = 0,
                    StartedAt = DateTime.UtcNow,
                    Status = "active",
                    Questions = selectedQuestions.Select(q => new SessionQuestion
                    {
                        QuestionId = q.Id,
                        UserAnswer = string.Empty,
                        IsCorrect = false,
                        TimeSpentSeconds = 0,
                        AttemptCount = 0
                    }).ToList()
                };

                await _context.LearningSessions.InsertOneAsync(session);

                _logger.LogInformation("Learning session started: {SessionId} for user: {UserId}", session.Id, userId);

                return MapToLearningSessionDTO(session, selectedQuestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting learning session for user: {UserId}", userId);
                return null;
            }
        }

        public async Task<SubmitAnswerResponseDTO?> SubmitAnswerAsync(string userId, SubmitAnswerRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(request.SessionId, out var sessionObjectId) ||
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return null;
                }

                var session = await _context.LearningSessions
                    .Find(s => s.Id == sessionObjectId && s.UserId == userObjectId && s.Status == "active")
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    return null;
                }

                // Find the question in session
                var sessionQuestion = session.Questions.FirstOrDefault(q => q.QuestionId == request.QuestionId);
                if (sessionQuestion == null)
                {
                    return null;
                }

                // Get full question details
                var question = await _questionService.GetQuestionByIdAsync(request.QuestionId);
                if (question == null)
                {
                    return null;
                }

                // Validate answer
                var validationRequest = new ValidateAnswerRequestDTO
                {
                    UserAnswer = request.Answer,
                    CaseSensitive = false,
                    TrimWhitespace = true
                };

                var validation = await _questionService.ValidateAnswerAsync(request.QuestionId, validationRequest);
                if (validation == null)
                {
                    return null;
                }

                // Update session question
                sessionQuestion.UserAnswer = request.Answer;
                sessionQuestion.IsCorrect = validation.IsCorrect;
                sessionQuestion.TimeSpentSeconds = request.TimeSpentSeconds;
                sessionQuestion.AttemptCount++;
                sessionQuestion.AnsweredAt = DateTime.UtcNow;

                // Update session stats
                if (validation.IsCorrect)
                {
                    session.CorrectAnswers++;
                }

                session.Score = session.TotalQuestions > 0 ? 
                    (double)session.CorrectAnswers / session.TotalQuestions * 100.0 : 0.0;

                // Check if session is completed
                var answeredQuestions = session.Questions.Count(q => q.AnsweredAt.HasValue);
                var sessionCompleted = answeredQuestions >= session.TotalQuestions;

                if (sessionCompleted)
                {
                    session.Status = "completed";
                    session.CompletedAt = DateTime.UtcNow;
                    session.TotalTimeSeconds = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;
                }

                // Update session in database
                await _context.LearningSessions.ReplaceOneAsync(
                    s => s.Id == sessionObjectId,
                    session
                );

                _logger.LogInformation("Answer submitted for session: {SessionId}, Question: {QuestionId}, Correct: {IsCorrect}",
                    request.SessionId, request.QuestionId, validation.IsCorrect);

                return new SubmitAnswerResponseDTO
                {
                    IsCorrect = validation.IsCorrect,
                    CorrectAnswer = validation.CorrectAnswer,
                    Explanation = validation.Explanation,
                    CurrentScore = session.Score,
                    QuestionsRemaining = session.TotalQuestions - answeredQuestions,
                    SessionCompleted = sessionCompleted
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer");
                return null;
            }
        }

        public async Task<CompleteLearningSessionResponseDTO?> CompleteLearningSessionAsync(string userId, CompleteLearningSessionRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(request.SessionId, out var sessionObjectId) ||
                    !ObjectId.TryParse(userId, out var userObjectId))
                {
                    return null;
                }

                var session = await _context.LearningSessions
                    .Find(s => s.Id == sessionObjectId && s.UserId == userObjectId)
                    .FirstOrDefaultAsync();

                if (session == null || session.Status == "completed")
                {
                    return null;
                }

                // Complete the session
                session.Status = "completed";
                session.CompletedAt = DateTime.UtcNow;
                session.TotalTimeSeconds = request.TotalTimeSeconds;

                // Recalculate final score
                var answeredQuestions = session.Questions.Count(q => q.AnsweredAt.HasValue);
                session.Score = session.TotalQuestions > 0 ? 
                    (double)session.CorrectAnswers / session.TotalQuestions * 100.0 : 0.0;

                await _context.LearningSessions.ReplaceOneAsync(
                    s => s.Id == sessionObjectId,
                    session
                );

                // Update user progress
                var updatedProgress = await UpdateUserProgressAsync(userId, MapToLearningSessionDTO(session, null));

                // Check for level up
                var previousLevel = updatedProgress.Level - 1; // Simplified level up check
                var levelUp = updatedProgress.Level > previousLevel;
                var experienceGained = (int)(session.Score * session.TotalQuestions / 10); // Simplified XP calculation

                _logger.LogInformation("Learning session completed: {SessionId} for user: {UserId}, Score: {Score}",
                    request.SessionId, userId, session.Score);

                return new CompleteLearningSessionResponseDTO
                {
                    Session = MapToLearningSessionDTO(session, null),
                    UpdatedProgress = updatedProgress,
                    LevelUp = levelUp,
                    ExperienceGained = experienceGained
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing learning session");
                return null;
            }
        }

        public async Task<LearningSessionDTO?> GetActiveLearningSessionAsync(string userId)
        {
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId))
                {
                    return null;
                }

                var session = await _context.LearningSessions
                    .Find(s => s.UserId == userObjectId && s.Status == "active")
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    return null;
                }

                // Get question details
                var questionIds = session.Questions.Select(q => q.QuestionId).ToList();
                var questions = await _questionService.GetQuestionsByIdsAsync(questionIds);

                return MapToLearningSessionDTO(session, questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active learning session for user: {UserId}", userId);
                return null;
            }
        }

        #endregion

        #region Progress & Statistics

        public async Task<GetProgressResponseDTO> GetProgressAsync(string userId, GetProgressRequestDTO? request)
        {
            try
            {
                var progress = await GetUserProgressAsync(userId);
                var recentSessions = await GetRecentSessionsAsync(userId, request);
                var statistics = await GetBasicStatisticsAsync(userId, request);

                return new GetProgressResponseDTO
                {
                    Progress = progress,
                    RecentSessions = recentSessions,
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for user: {UserId}", userId);
                return new GetProgressResponseDTO
                {
                    Progress = new UserProgressDTO { UserId = userId }
                };
            }
        }

        public async Task<LearningStatisticsDTO> GetLearningStatisticsAsync(string userId, GetProgressRequestDTO? request)
        {
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId))
                {
                    return new LearningStatisticsDTO();
                }

                var builder = Builders<LearningSession>.Filter;
                var filter = builder.Eq(s => s.UserId, userObjectId);

                // Apply date filters
                if (request?.FromDate.HasValue == true)
                {
                    filter = builder.And(filter, builder.Gte(s => s.StartedAt, request.FromDate.Value));
                }

                if (request?.ToDate.HasValue == true)
                {
                    filter = builder.And(filter, builder.Lte(s => s.StartedAt, request.ToDate.Value));
                }

                var sessions = await _context.LearningSessions
                    .Find(filter)
                    .ToListAsync();

                var completedSessions = sessions.Where(s => s.Status == "completed").ToList();

                var statistics = new LearningStatisticsDTO();

                if (completedSessions.Any())
                {
                    statistics.TotalStudyTimeMinutes = completedSessions.Sum(s => s.TotalTimeSeconds) / 60;
                    statistics.AverageSessionScore = completedSessions.Average(s => s.Score);
                    statistics.WeeklyProgress = CalculateWeeklyProgress(completedSessions);

                    // Calculate daily progress
                    statistics.DailyProgress = CalculateDailyProgress(completedSessions);

                    // Strength and weak areas (placeholder)
                    statistics.StrengthAreas = new List<string> { "Multiple Choice", "True/False" };
                    statistics.WeakAreas = new List<string> { "Fill in the Blank", "Short Answer" };
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning statistics for user: {UserId}", userId);
                return new LearningStatisticsDTO();
            }
        }

        public async Task<UserProgressDTO> UpdateUserProgressAsync(string userId, LearningSessionDTO session)
        {
            try
            {
                var progress = await GetUserProgressAsync(userId);

                // Update progress based on session
                progress.TotalSessions++;
                progress.TotalQuestionsAnswered += session.TotalQuestions;
                progress.TotalCorrectAnswers += session.CorrectAnswers;
                progress.OverallAccuracy = progress.TotalQuestionsAnswered > 0 ?
                    (double)progress.TotalCorrectAnswers / progress.TotalQuestionsAnswered * 100.0 : 0.0;

                // Add experience points (simplified)
                var experienceGained = (int)(session.Score * session.TotalQuestions / 10);
                progress.Experience += experienceGained;

                // Calculate level (simplified: every 1000 XP = 1 level)
                progress.Level = Math.Max(1, progress.Experience / 1000);
                progress.ExperienceToNextLevel = 1000 - (progress.Experience % 1000);

                // Update last study date
                progress.LastStudyDate = DateTime.UtcNow;

                // Update streak (simplified)
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                if (progress.LastStudyDate.Date == yesterday || progress.LastStudyDate.Date == DateTime.UtcNow.Date)
                {
                    progress.StreakDays = Math.Max(1, progress.StreakDays + 1);
                }
                else
                {
                    progress.StreakDays = 1;
                }

                // Update study time by content
                if (!progress.StudyTimeByContent.ContainsKey(session.ContentId))
                {
                    progress.StudyTimeByContent[session.ContentId] = 0;
                }
                progress.StudyTimeByContent[session.ContentId] += session.TotalTimeSeconds;

                // Save progress to user profile (this would typically be in UserService)
                await SaveUserProgressAsync(userId, progress);

                return progress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user progress for user: {UserId}", userId);
                return new UserProgressDTO { UserId = userId };
            }
        }

        #endregion

        #region Room Learning

        public async Task<LearningRoomDTO?> CreateRoomAsync(string userId, CreateRoomRequestDTO request)
        {
            try
            {
                // Check if content exists and user has access
                var content = await _contentService.GetContentByIdAsync(request.ContentId);
                if (content == null || !await _contentService.HasAccessToContentAsync(request.ContentId, userId))
                {
                    return null;
                }

                var room = new LearningRoom
                {
                    Id = ObjectId.GenerateNewId(),
                    Code = GenerateRoomCode(),
                    HostId = ObjectId.Parse(userId),
                    Name = request.Name,
                    ContentId = request.ContentId,
                    MaxParticipants = request.MaxParticipants,
                    TimeLimitMinutes = request.TimeLimitMinutes,
                    QuestionCount = request.QuestionCount,
                    IsPrivate = request.IsPrivate,
                    Status = "waiting",
                    CreatedAt = DateTime.UtcNow,
                    Participants = new List<RoomParticipant>
                    {
                        new RoomParticipant
                        {
                            UserId = ObjectId.Parse(userId),
                            IsHost = true,
                            IsReady = true,
                            Status = "ready",
                            JoinedAt = DateTime.UtcNow
                        }
                    }
                };

                await _context.LearningRooms.InsertOneAsync(room);

                _logger.LogInformation("Learning room created: {RoomId} by user: {UserId}", room.Id, userId);

                return await MapToLearningRoomDTO(room, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating learning room for user: {UserId}", userId);
                return null;
            }
        }

        public async Task<JoinRoomResponseDTO> JoinRoomAsync(string userId, JoinRoomRequestDTO request)
        {
            try
            {
                var room = await _context.LearningRooms
                    .Find(r => r.Code == request.RoomCode && r.Status == "waiting")
                    .FirstOrDefaultAsync();

                if (room == null)
                {
                    return new JoinRoomResponseDTO
                    {
                        Success = false,
                        Message = "Room not found or no longer accepting participants"
                    };
                }

                // Check if room is full
                if (room.Participants.Count >= room.MaxParticipants)
                {
                    return new JoinRoomResponseDTO
                    {
                        Success = false,
                        Message = "Room is full"
                    };
                }

                // Check if user is already in room
                if (room.Participants.Any(p => p.UserId == ObjectId.Parse(userId)))
                {
                    return new JoinRoomResponseDTO
                    {
                        Success = false,
                        Message = "Already in this room"
                    };
                }

                // Add user to room
                var userInfo = await _userService.GetUserByIdAsync(userId);
                room.Participants.Add(new RoomParticipant
                {
                    UserId = ObjectId.Parse(userId),
                    Username = userInfo?.Username ?? "Unknown",
                    DisplayName = userInfo?.DisplayName ?? "Unknown",
                    IsHost = false,
                    IsReady = false,
                    Status = "joined",
                    JoinedAt = DateTime.UtcNow
                });

                await _context.LearningRooms.ReplaceOneAsync(r => r.Id == room.Id, room);

                var content = await _contentService.GetContentByIdAsync(room.ContentId);
                var roomDto = await MapToLearningRoomDTO(room, content);

                _logger.LogInformation("User joined room: {RoomCode} by user: {UserId}", request.RoomCode, userId);

                return new JoinRoomResponseDTO
                {
                    Success = true,
                    Message = "Successfully joined room",
                    Room = roomDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room: {RoomCode}", request.RoomCode);
                return new JoinRoomResponseDTO
                {
                    Success = false,
                    Message = "Failed to join room"
                };
            }
        }

        public async Task<LearningRoomDTO?> StartRoomSessionAsync(string userId, StartRoomSessionRequestDTO request)
        {
            try
            {
                if (!ObjectId.TryParse(request.RoomId, out var roomObjectId))
                {
                    return null;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                if (room == null || room.HostId != ObjectId.Parse(userId))
                {
                    return null;
                }

                // Update room status
                room.Status = "active";
                room.StartedAt = DateTime.UtcNow;

                // Update all participants status
                foreach (var participant in room.Participants)
                {
                    participant.Status = "playing";
                }

                await _context.LearningRooms.ReplaceOneAsync(r => r.Id == roomObjectId, room);

                var content = await _contentService.GetContentByIdAsync(room.ContentId);
                var roomDto = await MapToLearningRoomDTO(room, content);

                _logger.LogInformation("Room session started: {RoomId} by user: {UserId}", request.RoomId, userId);

                return roomDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting room session: {RoomId}", request.RoomId);
                return null;
            }
        }

        public async Task<RoomLeaderboardDTO?> GetRoomLeaderboardAsync(string roomId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(roomId, out var roomObjectId))
                {
                    return null;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                if (room == null || !room.Participants.Any(p => p.UserId == ObjectId.Parse(userId)))
                {
                    return null;
                }

                // Sort participants by score (descending)
                var sortedParticipants = room.Participants
                    .OrderByDescending(p => p.CurrentScore)
                    .ThenBy(p => p.JoinedAt)
                    .ToList();

                return new RoomLeaderboardDTO
                {
                    Rankings = sortedParticipants.Select(MapToRoomParticipantDTO).ToList(),
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room leaderboard: {RoomId}", roomId);
                return null;
            }
        }

        public async Task<bool> LeaveRoomAsync(string roomId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(roomId, out var roomObjectId))
                {
                    return false;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                if (room == null)
                {
                    return false;
                }

                var participant = room.Participants.FirstOrDefault(p => p.UserId == ObjectId.Parse(userId));
                if (participant == null)
                {
                    return false;
                }

                room.Participants.Remove(participant);

                // If host left, assign new host or close room
                if (participant.IsHost && room.Participants.Any())
                {
                    room.Participants.First().IsHost = true;
                }
                else if (!room.Participants.Any())
                {
                    room.Status = "completed";
                }

                await _context.LearningRooms.ReplaceOneAsync(r => r.Id == roomObjectId, room);

                _logger.LogInformation("User left room: {RoomId} by user: {UserId}", roomId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving room: {RoomId}", roomId);
                return false;
            }
        }

        public async Task<LearningRoomDTO?> GetRoomAsync(string roomId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(roomId, out var roomObjectId))
                {
                    return null;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                if (room == null || !room.Participants.Any(p => p.UserId == ObjectId.Parse(userId)))
                {
                    return null;
                }

                var content = await _contentService.GetContentByIdAsync(room.ContentId);
                return await MapToLearningRoomDTO(room, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room: {RoomId}", roomId);
                return null;
            }
        }

        #endregion

        #region Utilities

        public string GenerateRoomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<bool> IsUserInRoomAsync(string roomId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(roomId, out var roomObjectId))
                {
                    return false;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                return room?.Participants.Any(p => p.UserId == ObjectId.Parse(userId)) == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is in room: {RoomId}", roomId);
                return false;
            }
        }

        public async Task<bool> IsUserRoomHostAsync(string roomId, string userId)
        {
            try
            {
                if (!ObjectId.TryParse(roomId, out var roomObjectId))
                {
                    return false;
                }

                var room = await _context.LearningRooms
                    .Find(r => r.Id == roomObjectId)
                    .FirstOrDefaultAsync();

                return room?.HostId == ObjectId.Parse(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is room host: {RoomId}", roomId);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        private LearningSessionDTO MapToLearningSessionDTO(LearningSession session, List<QuestionDTO>? questions)
        {
            return new LearningSessionDTO
            {
                Id = session.Id.ToString(),
                UserId = session.UserId.ToString(),
                ContentId = session.ContentId,
                RoomId = session.RoomId,
                Questions = session.Questions.Select(q => new SessionQuestionDTO
                {
                    QuestionId = q.QuestionId,
                    UserAnswer = q.UserAnswer,
                    IsCorrect = q.IsCorrect,
                    TimeSpentSeconds = q.TimeSpentSeconds,
                    AttemptCount = q.AttemptCount,
                    AnsweredAt = q.AnsweredAt
                }).ToList(),
                TotalQuestions = session.TotalQuestions,
                CorrectAnswers = session.CorrectAnswers,
                Score = session.Score,
                TotalTimeSeconds = session.TotalTimeSeconds,
                StartedAt = session.StartedAt,
                CompletedAt = session.CompletedAt,
                Status = session.Status
            };
        }

        private async Task<LearningRoomDTO> MapToLearningRoomDTO(LearningRoom room, ContentDTO? content)
        {
            return new LearningRoomDTO
            {
                Id = room.Id.ToString(),
                Code = room.Code,
                HostId = room.HostId.ToString(),
                Name = room.Name,
                ContentId = room.ContentId,
                Content = content ?? new ContentDTO(),
                Participants = room.Participants.Select(MapToRoomParticipantDTO).ToList(),
                Settings = new RoomSettingsDTO
                {
                    MaxParticipants = room.MaxParticipants,
                    TimeLimitMinutes = room.TimeLimitMinutes,
                    QuestionCount = room.QuestionCount,
                    IsPrivate = room.IsPrivate
                },
                Status = room.Status,
                CreatedAt = room.CreatedAt,
                StartedAt = room.StartedAt,
                CompletedAt = room.CompletedAt
            };
        }

        private RoomParticipantDTO MapToRoomParticipantDTO(RoomParticipant participant)
        {
            return new RoomParticipantDTO
            {
                UserId = participant.UserId.ToString(),
                Username = participant.Username,
                DisplayName = participant.DisplayName,
                CurrentScore = participant.CurrentScore,
                QuestionsAnswered = participant.QuestionsAnswered,
                JoinedAt = participant.JoinedAt,
                IsHost = participant.IsHost,
                IsReady = participant.IsReady,
                Status = participant.Status
            };
        }

        private async Task<UserProgressDTO> GetUserProgressAsync(string userId)
        {
            // This would typically get from user profile or separate progress collection
            // For now, calculate from sessions
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId))
                {
                    return new UserProgressDTO { UserId = userId };
                }

                var sessions = await _context.LearningSessions
                    .Find(s => s.UserId == userObjectId && s.Status == "completed")
                    .ToListAsync();

                var progress = new UserProgressDTO
                {
                    UserId = userId,
                    Level = 1,
                    Experience = 0,
                    ExperienceToNextLevel = 1000,
                    TotalSessions = sessions.Count,
                    TotalQuestionsAnswered = sessions.Sum(s => s.TotalQuestions),
                    TotalCorrectAnswers = sessions.Sum(s => s.CorrectAnswers),
                    OverallAccuracy = 0.0,
                    StreakDays = 1,
                    LastStudyDate = sessions.LastOrDefault()?.CompletedAt ?? DateTime.MinValue
                };

                if (progress.TotalQuestionsAnswered > 0)
                {
                    progress.OverallAccuracy = (double)progress.TotalCorrectAnswers / progress.TotalQuestionsAnswered * 100.0;
                }

                // Simple experience calculation
                progress.Experience = sessions.Sum(s => (int)(s.Score * s.TotalQuestions / 10));
                progress.Level = Math.Max(1, progress.Experience / 1000);
                progress.ExperienceToNextLevel = 1000 - (progress.Experience % 1000);

                return progress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user progress: {UserId}", userId);
                return new UserProgressDTO { UserId = userId };
            }
        }

        private async Task<List<LearningSessionDTO>> GetRecentSessionsAsync(string userId, GetProgressRequestDTO? request)
        {
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId))
                {
                    return new List<LearningSessionDTO>();
                }

                var builder = Builders<LearningSession>.Filter;
                var filter = builder.Eq(s => s.UserId, userObjectId);

                if (request?.FromDate.HasValue == true)
                {
                    filter = builder.And(filter, builder.Gte(s => s.StartedAt, request.FromDate.Value));
                }

                if (request?.ToDate.HasValue == true)
                {
                    filter = builder.And(filter, builder.Lte(s => s.StartedAt, request.ToDate.Value));
                }

                var sessions = await _context.LearningSessions
                    .Find(filter)
                    .SortByDescending(s => s.StartedAt)
                    .Limit(10)
                    .ToListAsync();

                return sessions.Select(s => MapToLearningSessionDTO(s, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sessions for user: {UserId}", userId);
                return new List<LearningSessionDTO>();
            }
        }

        private async Task<Dictionary<string, object>> GetBasicStatisticsAsync(string userId, GetProgressRequestDTO? request)
        {
            // Basic statistics for progress response
            return new Dictionary<string, object>
            {
                ["averageScore"] = 0.0,
                ["totalStudyTime"] = 0,
                ["sessionsThisWeek"] = 0,
                ["favoriteTopics"] = new List<string>()
            };
        }

        private double CalculateWeeklyProgress(List<LearningSession> sessions)
        {
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var thisWeekSessions = sessions.Where(s => s.StartedAt >= lastWeek).ToList();
            var previousWeekSessions = sessions.Where(s => s.StartedAt >= lastWeek.AddDays(-7) && s.StartedAt < lastWeek).ToList();

            if (!previousWeekSessions.Any()) return 100.0;

            var thisWeekAvg = thisWeekSessions.Any() ? thisWeekSessions.Average(s => s.Score) : 0.0;
            var prevWeekAvg = previousWeekSessions.Average(s => s.Score);

            return prevWeekAvg > 0 ? ((thisWeekAvg - prevWeekAvg) / prevWeekAvg) * 100.0 : 0.0;
        }

        private List<DailyProgressDTO> CalculateDailyProgress(List<LearningSession> sessions)
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            return last7Days.Select(date =>
            {
                var daySessions = sessions.Where(s => s.StartedAt.Date == date).ToList();

                return new DailyProgressDTO
                {
                    Date = date,
                    SessionsCompleted = daySessions.Count,
                    QuestionsAnswered = daySessions.Sum(s => s.TotalQuestions),
                    AverageScore = daySessions.Any() ? daySessions.Average(s => s.Score) : 0.0,
                    StudyTimeMinutes = daySessions.Sum(s => s.TotalTimeSeconds) / 60
                };
            }).ToList();
        }

        private async Task SaveUserProgressAsync(string userId, UserProgressDTO progress)
        {
            // In a real implementation, this would save to a separate progress collection
            // or update user profile. For now, it's a placeholder
            await Task.CompletedTask;
        }

        #endregion
    }
}
