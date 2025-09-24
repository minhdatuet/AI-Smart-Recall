using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.Data.Models.Questions;
using AISmartRecall.Services.AI;

namespace AISmartRecall.Systems.Learning
{
    /// <summary>
    /// Hệ thống chấm điểm tự động và AI grading cho câu tự luận
    /// </summary>
    public class AnswerValidationSystem : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool _enableAIGrading = true;
        [SerializeField] private float _partialCreditThreshold = 0.5f;
        [SerializeField] private bool _caseSensitive = false;
        [SerializeField] private bool _ignoreWhitespace = true;

        [Header("AI Grading Settings")]
        [SerializeField] private float _aiGradingTimeout = 30f;
        [SerializeField] private int _maxRetryAttempts = 2;
        [SerializeField] private bool _fallbackToAutoGrading = true;

        // Events
        public static event Action<ValidationResult> OnAnswerValidated;
        public static event Action<string> OnValidationError;
        public static event Action<float> OnAIGradingProgress;

        // Properties
        public bool IsValidating { get; private set; }
        public OpenRouterClient AIGradingService { get; private set; }

        // Private fields
        private Dictionary<string, ValidationResult> _cachedResults = new Dictionary<string, ValidationResult>();

        private void Awake()
        {
            InitializeSystem();
        }

        /// <summary>
        /// Khởi tạo validation system
        /// </summary>
        private void InitializeSystem()
        {
            // Initialize AI service if enabled
            if (_enableAIGrading)
            {
                AIGradingService = FindObjectOfType<OpenRouterClient>();
                if (AIGradingService == null)
                {
                    Debug.LogWarning("[AnswerValidationSystem] OpenRouterClient not found. AI grading disabled.");
                    _enableAIGrading = false;
                }
            }

            Debug.Log("[AnswerValidationSystem] System initialized");
        }

        /// <summary>
        /// Validate câu trả lời
        /// </summary>
        /// <param name="question">Câu hỏi</param>
        /// <param name="userAnswer">Câu trả lời của user</param>
        /// <param name="context">Context nội dung (cho AI grading)</param>
        /// <returns>Kết quả validation</returns>
        public async UniTask<ValidationResult> ValidateAnswerAsync(BaseQuestion question, string userAnswer, string context = null)
        {
            if (question == null)
            {
                return CreateErrorResult("Question is null");
            }

            if (string.IsNullOrWhiteSpace(userAnswer))
            {
                return CreateNotAnsweredResult();
            }

            IsValidating = true;

            try
            {
                // Check cache first
                string cacheKey = GenerateCacheKey(question, userAnswer);
                if (_cachedResults.ContainsKey(cacheKey))
                {
                    var cachedResult = _cachedResults[cacheKey];
                    OnAnswerValidated?.Invoke(cachedResult);
                    return cachedResult;
                }

                ValidationResult result;

                // Determine validation method
                if (question.RequiresAIGrading() && _enableAIGrading)
                {
                    result = await ValidateWithAI(question, userAnswer, context);
                }
                else
                {
                    result = ValidateAutomatically(question, userAnswer);
                }

                // Cache result
                _cachedResults[cacheKey] = result;

                OnAnswerValidated?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnswerValidationSystem] Validation error: {ex.Message}");
                OnValidationError?.Invoke(ex.Message);
                
                var errorResult = CreateErrorResult(ex.Message);
                return errorResult;
            }
            finally
            {
                IsValidating = false;
            }
        }

        /// <summary>
        /// Validate tự động (cho multiple choice, true/false, etc.)
        /// </summary>
        private ValidationResult ValidateAutomatically(BaseQuestion question, string userAnswer)
        {
            var result = new ValidationResult
            {
                Question = question,
                UserAnswer = userAnswer,
                ValidationMethod = ValidationMethod.Automatic,
                ValidatedAt = DateTime.Now
            };

            switch (question.QuestionType)
            {
                case QuestionType.ContentMultipleChoice:
                case QuestionType.UnderstandingMultipleChoice:
                case QuestionType.MissingWordChoice:
                    return ValidateMultipleChoice(question, userAnswer, result);
                    
                case QuestionType.TrueFalse:
                    return ValidateTrueFalse(question, userAnswer, result);
                
                case QuestionType.FillInTheBlank:
                    return ValidateFillInBlank(question, userAnswer, result);
                
                case QuestionType.MatchConcepts:
                    // return ValidateMatching(question, userAnswer, result);
                    return ValidateMultipleChoice(question, userAnswer, result);
                    
                case QuestionType.ExactTyping:
                case QuestionType.ShortAnswer:
                case QuestionType.ScenarioQuestion:
                    // For simple text input, try keyword matching
                    return ValidateTextInput(question, userAnswer, result);
                    
                default:
                    result.IsCorrect = false;
                    result.Score = 0f;
                    result.Feedback = "Unsupported question type for automatic validation";
                    return result;
            }
        }

        /// <summary>
        /// Validate multiple choice
        /// </summary>
        private ValidationResult ValidateMultipleChoice(BaseQuestion question, string userAnswer, ValidationResult result)
        {
            var options = question.GetOptions();
            if (options == null || options.Count == 0)
            {
                return CreateErrorResult("Invalid multiple choice question - no options available");
            }

            string normalizedUserAnswer = NormalizeAnswer(userAnswer);
            string correctAnswer = question.GetCorrectAnswer();
            string normalizedCorrectAnswer = NormalizeAnswer(correctAnswer);

            result.IsCorrect = normalizedUserAnswer.Equals(normalizedCorrectAnswer, GetStringComparison());
            result.Score = result.IsCorrect ? 100f : 0f;
            result.Feedback = result.IsCorrect ? 
                $"Chính xác! {question.Explanation}" : 
                $"Chưa đúng. Đáp án đúng: {correctAnswer}. {question.Explanation}";
            
            result.CorrectAnswer = correctAnswer;
            return result;
        }

        /// <summary>
        /// Validate true/false
        /// </summary>
        private ValidationResult ValidateTrueFalse(BaseQuestion question, string userAnswer, ValidationResult result)
        {
            if (question == null)
            {
                return CreateErrorResult("Invalid true/false question");
            }

            string normalizedUserAnswer = NormalizeAnswer(userAnswer).ToLower();
            bool userBool = normalizedUserAnswer == "true" || normalizedUserAnswer == "đúng";
            
            // Get correct answer from base question
            string correctAnswerStr = question.GetCorrectAnswer().ToLower();
            bool correctAnswer = correctAnswerStr == "true" || correctAnswerStr == "đúng";
            
            result.IsCorrect = userBool == correctAnswer;
            result.Score = result.IsCorrect ? 100f : 0f;
            result.Feedback = result.IsCorrect ?
                $"Chính xác! {question.Explanation}" :
                $"Chưa đúng. Đáp án đúng: {(correctAnswer ? "Đúng" : "Sai")}. {question.Explanation}";
                
            result.CorrectAnswer = correctAnswer ? "Đúng" : "Sai";
            return result;
        }

        /// <summary>
        /// Validate fill in blank
        /// </summary>
        private ValidationResult ValidateFillInBlank(BaseQuestion question, string userAnswer, ValidationResult result)
        {
            if (question == null)
            {
                return CreateErrorResult("Invalid fill in blank question");
            }

            // For now, use simple string matching as fallback for FillInBlank
            // This can be extended once concrete FillInBlankQuestion class is available
            string normalizedUserAnswer = NormalizeAnswer(userAnswer);
            string correctAnswer = question.GetCorrectAnswer();
            string normalizedCorrectAnswer = NormalizeAnswer(correctAnswer);

            // Check for partial matches by splitting on common separators
            var userParts = userAnswer.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(p => NormalizeAnswer(p)).ToList();
            var correctParts = correctAnswer.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(p => NormalizeAnswer(p)).ToList();

            int matchCount = 0;
            foreach (var userPart in userParts)
            {
                if (correctParts.Any(cp => cp.Equals(userPart, GetStringComparison()) || 
                                          CheckSimilarity(userPart, cp)))
                {
                    matchCount++;
                }
            }

            float accuracy = correctParts.Count > 0 ? (float)matchCount / correctParts.Count : 0f;
            
            result.IsCorrect = accuracy >= 0.8f; // 80% accuracy threshold
            result.Score = accuracy * 100f;
            result.Feedback = result.IsCorrect ? 
                $"Tốt! Đúng {matchCount}/{correctParts.Count} phần. {question.Explanation}" : 
                $"Đúng {matchCount}/{correctParts.Count} phần. Đáp án: {correctAnswer}. {question.Explanation}";
            result.CorrectAnswer = correctAnswer;
            
            return result;
        }

        /// <summary>
        /// Validate matching
        /// </summary>
        private ValidationResult ValidateMatching(BaseQuestion question, string userAnswer, ValidationResult result)
        {
            if (question == null)
            {
                return CreateErrorResult("Invalid matching question");
            }

            // Simple matching validation - use string comparison for now
            // This can be extended when concrete MatchingQuestion class is available
            string normalizedUserAnswer = NormalizeAnswer(userAnswer);
            string correctAnswer = question.GetCorrectAnswer();
            string normalizedCorrectAnswer = NormalizeAnswer(correctAnswer);

            // Parse user answer: "left1:right1|left2:right2|..."
            var userPairs = new Dictionary<string, string>();
            var pairs = userAnswer.Split('|');
            
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    userPairs[NormalizeAnswer(parts[0])] = NormalizeAnswer(parts[1]);
                }
            }

            // Parse correct pairs from answer
            var correctPairs = new Dictionary<string, string>();
            var correctPairsList = correctAnswer.Split('|');
            
            foreach (var pair in correctPairsList)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    correctPairs[NormalizeAnswer(parts[0])] = NormalizeAnswer(parts[1]);
                }
            }

            int correctCount = 0;
            foreach (var expectedPair in correctPairs)
            {
                if (userPairs.ContainsKey(expectedPair.Key) && 
                    userPairs[expectedPair.Key].Equals(expectedPair.Value, GetStringComparison()))
                {
                    correctCount++;
                }
            }

            float accuracy = correctPairs.Count > 0 ? (float)correctCount / correctPairs.Count : 0f;
                
            result.IsCorrect = accuracy >= 0.8f; // 80% accuracy threshold
            result.Score = accuracy * 100f;
            result.Feedback = result.IsCorrect ? 
                $"Tốt! Đúng {correctCount}/{correctPairs.Count} cặp. {question.Explanation}" : 
                $"Đúng {correctCount}/{correctPairs.Count} cặp. Đáp án: {correctAnswer}. {question.Explanation}";
            result.CorrectAnswer = correctAnswer;
            
            return result;
        }

        /// <summary>
        /// Validate text input (simple keyword matching)
        /// </summary>
        private ValidationResult ValidateTextInput(BaseQuestion question, string userAnswer, ValidationResult result)
        {
            if (question == null)
            {
                return CreateErrorResult("Invalid text input question");
            }

            // Simple keyword-based validation for text questions
            var keywords = ExtractKeywords(question.GetCorrectAnswer());
            var userKeywords = ExtractKeywords(userAnswer);
            
            int matchCount = keywords.Count(k => userKeywords.Contains(k));
            float accuracy = keywords.Count > 0 ? (float)matchCount / keywords.Count : 0f;
            
            // Apply partial credit threshold
            result.IsCorrect = accuracy >= _partialCreditThreshold;
            result.Score = accuracy * 100f;
            
            if (accuracy >= 0.8f)
            {
                result.Feedback = $"Rất tốt! Có {matchCount}/{keywords.Count} từ khóa chính xác. {question.Explanation}";
            }
            else if (accuracy >= _partialCreditThreshold)
            {
                result.Feedback = $"Có {matchCount}/{keywords.Count} từ khóa đúng. Cần bổ sung thêm. {question.Explanation}";
            }
            else
            {
                result.Feedback = $"Chưa đúng. Gợi ý: {string.Join(", ", keywords.Take(3))}. {question.Explanation}";
            }
            
            result.CorrectAnswer = question.GetCorrectAnswer();
            return result;
        }

        /// <summary>
        /// Validate với AI
        /// </summary>
        private async UniTask<ValidationResult> ValidateWithAI(BaseQuestion question, string userAnswer, string context)
        {
            if (AIGradingService == null)
            {
                Debug.LogWarning("[AnswerValidationSystem] AI service not available, falling back to auto grading");
                return _fallbackToAutoGrading ? 
                    ValidateAutomatically(question, userAnswer) : 
                    CreateErrorResult("AI grading service not available");
            }

            try
            {
                OnAIGradingProgress?.Invoke(0.1f);

                // Create AI grading prompt
                var prompt = CreateAIGradingPrompt(question, userAnswer, context);
                
                OnAIGradingProgress?.Invoke(0.3f);

                // Call AI service with timeout
                var aiResult = await CallAIServiceWithTimeout(prompt);
                
                OnAIGradingProgress?.Invoke(0.8f);

                // Parse AI response
                var result = ParseAIGradingResult(aiResult, question, userAnswer);
                
                OnAIGradingProgress?.Invoke(1.0f);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnswerValidationSystem] AI grading failed: {ex.Message}");
                
                if (_fallbackToAutoGrading)
                {
                    Debug.Log("[AnswerValidationSystem] Falling back to automatic grading");
                    return ValidateAutomatically(question, userAnswer);
                }
                
                return CreateErrorResult($"AI grading failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo prompt cho AI grading
        /// </summary>
        private string CreateAIGradingPrompt(BaseQuestion question, string userAnswer, string context)
        {
            var prompt = $@"
Hãy chấm điểm câu trả lời sau đây:

CÂU HỎI: {question.Question}

CONTEXT: {context ?? "Không có context cụ thể"}

CÂU TRẢ LỜI CỦA HỌC SINH: {userAnswer}

ĐÁP ÁN THAM KHẢO: {question.GetCorrectAnswer()}

GIẢI THÍCH: {question.Explanation}

Hãy đánh giá câu trả lời theo format JSON sau:
{{
    ""score"": [điểm từ 0-100],
    ""isCorrect"": [true/false],
    ""feedback"": ""[nhận xét chi tiết bằng tiếng Việt]"",
    ""reasoning"": ""[lý do chấm điểm]""
}}

Tiêu chí chấm điểm:
- 90-100: Hoàn toàn chính xác và đầy đủ
- 70-89: Đúng chủ yếu nhưng thiếu một số chi tiết
- 50-69: Có một phần đúng nhưng còn thiếu sót
- 30-49: Có hiểu biết cơ bản nhưng chưa chính xác
- 0-29: Sai hoặc không liên quan

Chú ý: Hãy chấm điểm khuyến khích và xây dựng, đưa ra feedback cụ thể để học sinh cải thiện.";

            return prompt;
        }

        /// <summary>
        /// Gọi AI service với timeout
        /// </summary>
        private async UniTask<string> CallAIServiceWithTimeout(string prompt)
        {
            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_aiGradingTimeout));
            var aiTask = AIGradingService.SendRequestAsync(prompt);

            var (hasResultLeft, result) = await UniTask.WhenAny(aiTask, timeoutTask);
            
            if (hasResultLeft) // AI task completed
            {
                return result;
            }
            else // Timeout
            {
                throw new TimeoutException($"AI grading timeout after {_aiGradingTimeout} seconds");
            }
        }

        /// <summary>
        /// Parse AI grading result
        /// </summary>
        private ValidationResult ParseAIGradingResult(string aiResponse, BaseQuestion question, string userAnswer)
        {
            try
            {
                // Clean AI response if it contains markdown
                string cleanedResponse = aiResponse.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                    int endIndex = cleanedResponse.LastIndexOf("```");
                    if (endIndex >= 0)
                        cleanedResponse = cleanedResponse.Substring(0, endIndex);
                }
                else if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                    int endIndex = cleanedResponse.LastIndexOf("```");
                    if (endIndex >= 0)
                        cleanedResponse = cleanedResponse.Substring(0, endIndex);
                }

                var aiResult = JsonUtility.FromJson<AIGradingResponse>(cleanedResponse.Trim());
                
                return new ValidationResult
                {
                    Question = question,
                    UserAnswer = userAnswer,
                    IsCorrect = aiResult.isCorrect,
                    Score = Mathf.Clamp(aiResult.score, 0f, 100f),
                    Feedback = aiResult.feedback ?? "Không có phản hồi từ AI",
                    CorrectAnswer = question.GetCorrectAnswer(),
                    ValidationMethod = ValidationMethod.AI,
                    ValidatedAt = DateTime.Now,
                    AIReasoning = aiResult.reasoning
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnswerValidationSystem] Failed to parse AI response: {ex.Message}");
                Debug.LogError($"AI Response: {aiResponse}");
                
                return new ValidationResult
                {
                    Question = question,
                    UserAnswer = userAnswer,
                    IsCorrect = false,
                    Score = 0f,
                    Feedback = "Không thể xử lý kết quả từ AI. Vui lòng thử lại.",
                    CorrectAnswer = question.GetCorrectAnswer(),
                    ValidationMethod = ValidationMethod.AI,
                    ValidatedAt = DateTime.Now,
                    Error = ex.Message
                };
            }
        }

        #region Helper Methods
        
        /// <summary>
        /// Normalize answer string
        /// </summary>
        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrEmpty(answer)) return "";
            
            string normalized = answer;
            
            if (_ignoreWhitespace)
            {
                normalized = normalized.Trim();
            }
            
            if (!_caseSensitive)
            {
                normalized = normalized.ToLowerInvariant();
            }
            
            return normalized;
        }

        /// <summary>
        /// Get string comparison type
        /// </summary>
        private StringComparison GetStringComparison()
        {
            return _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Check similarity between answers
        /// </summary>
        private bool CheckSimilarity(string answer1, string answer2)
        {
            if (string.IsNullOrEmpty(answer1) || string.IsNullOrEmpty(answer2))
                return false;
                
            // Simple Levenshtein distance check
            float similarity = 1.0f - (float)LevenshteinDistance(answer1, answer2) / Math.Max(answer1.Length, answer2.Length);
            return similarity >= 0.8f; // 80% similarity threshold
        }

        /// <summary>
        /// Calculate Levenshtein distance
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            if (s1 == s2) return 0;
            if (s1.Length == 0) return s2.Length;
            if (s2.Length == 0) return s1.Length;

            int[,] distance = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                distance[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                distance[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[s1.Length, s2.Length];
        }

        /// <summary>
        /// Extract keywords from text
        /// </summary>
        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            
            // Simple keyword extraction - split by common separators
            var keywords = text.Split(new[] { ' ', ',', '.', ';', ':', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(k => NormalizeAnswer(k))
                              .Where(k => k.Length > 2) // Ignore very short words
                              .Distinct()
                              .ToList();
                              
            return keywords;
        }

        /// <summary>
        /// Generate cache key
        /// </summary>
        private string GenerateCacheKey(BaseQuestion question, string userAnswer)
        {
            return $"{question.Id}_{userAnswer.GetHashCode()}";
        }

        /// <summary>
        /// Create error result
        /// </summary>
        private ValidationResult CreateErrorResult(string error)
        {
            return new ValidationResult
            {
                IsCorrect = false,
                Score = 0f,
                Feedback = "Có lỗi xảy ra khi chấm điểm",
                Error = error,
                ValidationMethod = ValidationMethod.Error,
                ValidatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Create not answered result
        /// </summary>
        private ValidationResult CreateNotAnsweredResult()
        {
            return new ValidationResult
            {
                IsCorrect = false,
                Score = 0f,
                Feedback = "Chưa có câu trả lời",
                ValidationMethod = ValidationMethod.NotAnswered,
                ValidatedAt = DateTime.Now
            };
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Clear validation cache
        /// </summary>
        public void ClearCache()
        {
            _cachedResults.Clear();
            Debug.Log("[AnswerValidationSystem] Cache cleared");
        }

        /// <summary>
        /// Set AI grading settings
        /// </summary>
        public void SetAIGradingSettings(bool enabled, float timeout, bool fallback)
        {
            _enableAIGrading = enabled;
            _aiGradingTimeout = timeout;
            _fallbackToAutoGrading = fallback;
        }

        /// <summary>
        /// Set validation settings
        /// </summary>
        public void SetValidationSettings(bool caseSensitive, bool ignoreWhitespace, float partialThreshold)
        {
            _caseSensitive = caseSensitive;
            _ignoreWhitespace = ignoreWhitespace;
            _partialCreditThreshold = Mathf.Clamp01(partialThreshold);
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Kết quả validation
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public BaseQuestion Question { get; set; }
        public string UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public float Score { get; set; } // 0-100
        public string Feedback { get; set; }
        public string CorrectAnswer { get; set; }
        public ValidationMethod ValidationMethod { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string Error { get; set; }
        public string AIReasoning { get; set; } // For AI validation
    }

    /// <summary>
    /// AI grading response
    /// </summary>
    [Serializable]
    public class AIGradingResponse
    {
        public float score;
        public bool isCorrect;
        public string feedback;
        public string reasoning;
    }

    /// <summary>
    /// Validation method
    /// </summary>
    public enum ValidationMethod
    {
        NotAnswered,
        Automatic,
        AI,
        Error
    }

    #endregion
}
