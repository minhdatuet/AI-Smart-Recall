using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using AISmartRecall.Data.Models.Questions;
using AISmartRecall.Data.Models;
using AISmartRecall.Managers;
using AISmartRecall.Services.AI;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// UI Controller cho Learning Session - quản lý toàn bộ giao diện học tập
    /// </summary>
    public class LearningSessionUI : MonoBehaviour
    {
        [Header("Main UI References")]
        [SerializeField] private GameObject _sessionPanel;
        [SerializeField] private QuestionDisplayUI _questionDisplay;
        [SerializeField] private ProgressTrackerUI _progressTracker;
        [SerializeField] private GameObject _loadingPanel;
        
        [Header("Navigation")]
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _exitButton;
        
        [Header("Session Info")]
        [SerializeField] private TextMeshProUGUI _sessionTitleText;
        [SerializeField] private TextMeshProUGUI _contentInfoText;

        [Header("Answer Feedback")] 
        [SerializeField] private OpenRouterClient _openRouterClient;
        [SerializeField] private GameObject _feedbackPanel;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private Image _feedbackIcon;
        [SerializeField] private Button _continueButton;

        // Events
        public static event Action<int> OnQuestionAnswered;
        public static event Action OnSessionPaused;
        public static event Action<SessionResults> OnSessionCompleted;
        public static event Action OnSessionExited;

        // Properties
        public bool IsActive { get; private set; }
        public LearningSession CurrentSession { get; private set; }

        // Private fields
        private List<BaseQuestion> _questions;
        private int _currentQuestionIndex;
        private Dictionary<int, string> _userAnswers = new Dictionary<int, string>();
        private Dictionary<int, float> _questionTimes = new Dictionary<int, float>();
        private float _currentQuestionStartTime;
        private bool _isAnswerSubmitted = false;

        private void Awake()
        {
            SetupUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// Thiết lập UI ban đầu
        /// </summary>
        private void SetupUI()
        {
            // Setup button listeners
            if (_previousButton) _previousButton.onClick.AddListener(GoToPreviousQuestion);
            if (_nextButton) _nextButton.onClick.AddListener(GoToNextQuestion);
            if (_submitButton) _submitButton.onClick.AddListener(() => SubmitCurrentAnswer().Forget());
            if (_pauseButton) _pauseButton.onClick.AddListener(PauseSession);
            if (_exitButton) _exitButton.onClick.AddListener(ExitSession);
            if (_continueButton) _continueButton.onClick.AddListener(ContinueAfterFeedback);

            // Subscribe to question display events
            if (_questionDisplay)
                _questionDisplay.OnAnswerChanged += OnAnswerChanged;

            // Initially hide panels
            SetActive(false);
            if (_feedbackPanel) _feedbackPanel.SetActive(false);
            if (_loadingPanel) _loadingPanel.SetActive(false);
        }

        /// <summary>
        /// Bắt đầu learning session
        /// </summary>
        public void StartSession(LearningSession session)
        {
            CurrentSession = session;
            _questions = session.Questions;
            _currentQuestionIndex = session.CurrentQuestionIndex;
            
            // Reset state
            _userAnswers.Clear();
            _questionTimes.Clear();
            _isAnswerSubmitted = false;

            // Setup UI
            SetActive(true);
            UpdateSessionInfo();
            UpdateProgressTracker();
            
            // Show first question
            ShowCurrentQuestion();
            
            Debug.Log($"[LearningSessionUI] Started session with {_questions.Count} questions");
        }

        /// <summary>
        /// Kích hoạt/tắt UI
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            if (_sessionPanel) _sessionPanel.SetActive(active);
            
            if (active)
            {
                _currentQuestionStartTime = Time.time;
            }
        }

        /// <summary>
        /// Hiển thị câu hỏi hiện tại
        /// </summary>
        private void ShowCurrentQuestion()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Count)
            {
                CompleteSession();
                return;
            }

            var currentQuestion = _questions[_currentQuestionIndex];
            
            // Reset timer
            _currentQuestionStartTime = Time.time;
            _isAnswerSubmitted = false;
            
            // Update question display
            if (_questionDisplay)
            {
                _questionDisplay.DisplayQuestion(currentQuestion);
                
                // Restore previous answer if exists
                if (_userAnswers.ContainsKey(_currentQuestionIndex))
                {
                    _questionDisplay.SetAnswer(_userAnswers[_currentQuestionIndex]);
                }
                else
                {
                    _questionDisplay.ClearAnswer();
                }
            }
            
            // Update navigation buttons
            UpdateNavigationButtons();
            
            // Update progress
            UpdateProgressTracker();
            
            // Hide feedback
            if (_feedbackPanel) _feedbackPanel.SetActive(false);
            
            Debug.Log($"[LearningSessionUI] Showing question {_currentQuestionIndex + 1}/{_questions.Count}: {currentQuestion.QuestionType}");
        }

        /// <summary>
        /// Cập nhật thông tin session
        /// </summary>
        private void UpdateSessionInfo()
        {
            if (CurrentSession == null) return;

            if (_sessionTitleText)
                _sessionTitleText.text = CurrentSession.Content?.Title ?? "Learning Session";

            if (_contentInfoText)
            {
                var content = CurrentSession.Content;
                if (content != null)
                {
                    _contentInfoText.text = $"{content.ContentType.GetDisplayName()} • {content.WordCount} từ • {content.EstimatedReadingTime} phút";
                }
            }
        }

        /// <summary>
        /// Cập nhật progress tracker
        /// </summary>
        private void UpdateProgressTracker()
        {
            if (_progressTracker && CurrentSession != null)
            {
                _progressTracker.UpdateProgress(
                    _currentQuestionIndex,
                    _questions.Count,
                    _userAnswers.Count,
                    CurrentSession.StartTime
                );
            }
        }

        /// <summary>
        /// Cập nhật trạng thái navigation buttons
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (_previousButton)
                _previousButton.interactable = _currentQuestionIndex > 0;

            if (_nextButton)
                _nextButton.interactable = _currentQuestionIndex < _questions.Count - 1;

            if (_submitButton)
            {
                bool hasAnswer = _questionDisplay?.HasAnswer() ?? false;
                _submitButton.interactable = hasAnswer && !_isAnswerSubmitted;
                _submitButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                    _currentQuestionIndex == _questions.Count - 1 ? "Hoàn thành" : "Kiểm tra";
            }
        }

        /// <summary>
        /// Xử lý khi answer thay đổi
        /// </summary>
        private void OnAnswerChanged(string answer)
        {
            // Save current answer
            _userAnswers[_currentQuestionIndex] = answer;
            
            // Update navigation
            UpdateNavigationButtons();
        }

        /// <summary>
        /// Chuyển đến câu hỏi trước
        /// </summary>
        private void GoToPreviousQuestion()
        {
            if (_currentQuestionIndex > 0)
            {
                SaveCurrentQuestionTime();
                _currentQuestionIndex--;
                ShowCurrentQuestion();
            }
        }

        /// <summary>
        /// Chuyển đến câu hỏi tiếp theo
        /// </summary>
        private void GoToNextQuestion()
        {
            if (_currentQuestionIndex < _questions.Count - 1)
            {
                SaveCurrentQuestionTime();
                _currentQuestionIndex++;
                ShowCurrentQuestion();
            }
        }

        /// <summary>
        /// Submit câu trả lời hiện tại
        /// </summary>
        private async UniTaskVoid SubmitCurrentAnswer()
        {
            if (!_questionDisplay || !_questionDisplay.HasAnswer() || _isAnswerSubmitted)
                return;

            _isAnswerSubmitted = true;
            string userAnswer = _questionDisplay.GetAnswer();
            SaveCurrentQuestionTime();
            
            // Validate answer
            var currentQuestion = _questions[_currentQuestionIndex];
            bool needsAIGrading = currentQuestion.RequiresAIGrading();
            
            ShowLoading(true);
            
            try
            {
                if (needsAIGrading)
                {
                    // Send to AI for grading
                    await ProcessAIGrading(currentQuestion, userAnswer);
                }
                else
                {
                    // Auto-validate
                    bool isCorrect = currentQuestion.ValidateAnswer(userAnswer);
                    ShowFeedback(isCorrect, currentQuestion.GetCorrectAnswer(), currentQuestion.Explanation);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LearningSessionUI] Error processing answer: {ex.Message}");
                ShowFeedback(false, "Lỗi xử lý", "Có lỗi xảy ra khi chấm điểm. Vui lòng thử lại.");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Xử lý chấm điểm bằng AI
        /// </summary>
        private async UniTask ProcessAIGrading(BaseQuestion question, string userAnswer)
        {
            try
            {
                // Validate inputs
                if (question == null || string.IsNullOrWhiteSpace(userAnswer))
                {
                    ShowFeedback(false, "Lỗi đầu vào", "Câu hỏi hoặc câu trả lời không hợp lệ.");
                    return;
                }

                // Get OpenRouter client - prioritize from field first, then from manager
                OpenRouterClient aiClient = _openRouterClient;
                
                if (aiClient == null)
                {
                    // Try to get from LearningSceneManager
                    var learningManager = LearningSceneManager.Instance;
                    aiClient = learningManager?.GetComponent<OpenRouterClient>();
                    
                    // Fallback: find in scene
                    if (aiClient == null)
                    {
                        aiClient = FindObjectOfType<OpenRouterClient>();
                    }
                }

                if (aiClient == null)
                {
                    Debug.LogError("[ProcessAIGrading] OpenRouterClient not found");
                    ShowFeedback(false, "Lỗi hệ thống", "Không thể tìm thấy AI service. Vui lòng kiểm tra cấu hình.");
                    return;
                }

                // Create grading prompt based on question type and content
                string originalContent = CurrentSession?.Content?.Content ?? "";
                string gradingPrompt = question.CreateGradingPrompt(userAnswer, originalContent);
                
                Debug.Log($"[ProcessAIGrading] Processing {question.QuestionType} question with AI...");
                
                // Call AI grading service
                AIGradingResult aiResult = null;
                
                // Use specific grading method if available, otherwise fallback to generic
                if (aiClient.GetType().GetMethod("GradeAnswerAsync") != null)
                {
                    aiResult = await aiClient.GradeAnswerAsync(question, userAnswer, originalContent);
                }
                else
                {
                    // Fallback: use generic request with custom prompt
                    var response = await aiClient.SendRequestAsync(gradingPrompt);
                    aiResult = ParseAIGradingResponse(response);
                }

                // Process AI result
                if (aiResult != null && aiResult.IsValid)
                {
                    // Determine if answer is correct based on percentage
                    bool isCorrect = aiResult.Percentage >= 70f; // 70% threshold for "correct"
                    
                    // Create detailed feedback
                    string detailedFeedback = CreateDetailedFeedback(aiResult, question);
                    
                    // Store AI grading result for analytics
                    StoreAIGradingResult(_currentQuestionIndex, aiResult);
                    
                    ShowFeedback(isCorrect, question.GetCorrectAnswer(), detailedFeedback);
                    
                    Debug.Log($"[ProcessAIGrading] AI grading completed: {aiResult.Percentage}% ({aiResult.GetFinalScore()}/10 points)");
                }
                else
                {
                    // AI grading failed, fallback to basic validation
                    Debug.LogWarning("[ProcessAIGrading] AI grading failed, falling back to basic validation");
                    await FallbackToBasicValidation(question, userAnswer);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProcessAIGrading] Exception during AI grading: {ex.Message}\nStackTrace: {ex.StackTrace}");
                
                // Fallback to basic validation on error
                await FallbackToBasicValidation(question, userAnswer);
            }
        }
        
        /// <summary>
        /// Fallback về validation cơ bản khi AI grading thất bại
        /// </summary>
        private async UniTask FallbackToBasicValidation(BaseQuestion question, string userAnswer)
        {
            await UniTask.Delay(500); // Small delay to simulate processing
            
            bool isCorrect = question.ValidateAnswer(userAnswer);
            string explanation = isCorrect ? 
                "Chính xác! " + question.Explanation :
                "Chưa đúng. " + question.Explanation + "\n\n[Lưu ý: AI chấm điểm tạm thời không khả dụng]";
                
            ShowFeedback(isCorrect, question.GetCorrectAnswer(), explanation);
        }
        
        /// <summary>
        /// Parse AI response thành AIGradingResult
        /// </summary>
        private AIGradingResult ParseAIGradingResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                    return new AIGradingResult();
                
                // Try to parse JSON response
                var jsonResponse = JsonUtility.FromJson<AIGradingJsonResponse>(response);
                
                if (jsonResponse != null)
                {
                    return new AIGradingResult(
                        jsonResponse.percentage,
                        jsonResponse.explanation ?? "Không có giải thích",
                        jsonResponse.suggestions ?? "Không có gợi ý"
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ParseAIGradingResponse] Failed to parse JSON: {ex.Message}");
            }
            
            // Fallback: simple text analysis
            return AnalyzeTextResponse(response);
        }
        
        /// <summary>
        /// Phân tích text response đơn giản
        /// </summary>
        private AIGradingResult AnalyzeTextResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new AIGradingResult();
            
            // Simple keyword-based analysis
            var lowerResponse = response.ToLower();
            
            float percentage = 50f; // Default neutral
            
            if (lowerResponse.Contains("đúng") || lowerResponse.Contains("correct") || 
                lowerResponse.Contains("chính xác") || lowerResponse.Contains("tốt"))
            {
                percentage = 85f;
            }
            else if (lowerResponse.Contains("sai") || lowerResponse.Contains("incorrect") || 
                     lowerResponse.Contains("không đúng") || lowerResponse.Contains("chưa đúng"))
            {
                percentage = 25f;
            }
            else if (lowerResponse.Contains("một phần") || lowerResponse.Contains("partially"))
            {
                percentage = 60f;
            }
            
            return new AIGradingResult(
                percentage,
                response.Length > 200 ? response.Substring(0, 200) + "..." : response,
                "Hãy xem lại đáp án và giải thích để hiểu rõ hơn."
            );
        }
        
        /// <summary>
        /// Tạo feedback chi tiết từ AI result
        /// </summary>
        private string CreateDetailedFeedback(AIGradingResult aiResult, BaseQuestion question)
        {
            var feedback = new System.Text.StringBuilder();
            
            // Score and status
            string status = aiResult.Percentage >= 90f ? "Xuất sắc!" :
                           aiResult.Percentage >= 70f ? "Tốt!" :
                           aiResult.Percentage >= 50f ? "Khá!" :
                           "Cần cải thiện";
                           
            feedback.AppendLine($"<b>{status}</b> ({aiResult.Percentage:F1}% - {aiResult.GetFinalScore():F1}/10 điểm)");
            feedback.AppendLine();
            
            // AI explanation
            if (!string.IsNullOrWhiteSpace(aiResult.Explanation))
            {
                feedback.AppendLine($"<b>Đánh giá AI:</b> {aiResult.Explanation}");
                feedback.AppendLine();
            }
            
            // Correct answer reference
            feedback.AppendLine($"<b>Đáp án tham khảo:</b> {question.GetCorrectAnswer()}");
            
            // Additional explanation
            if (!string.IsNullOrWhiteSpace(question.Explanation))
            {
                feedback.AppendLine($"<b>Giải thích:</b> {question.Explanation}");
            }
            
            // Suggestions for improvement
            if (!string.IsNullOrWhiteSpace(aiResult.Suggestions) && aiResult.Percentage < 90f)
            {
                feedback.AppendLine();
                feedback.AppendLine($"<b>Gợi ý cải thiện:</b> {aiResult.Suggestions}");
            }
            
            return feedback.ToString();
        }
        
        /// <summary>
        /// Lưu trữ kết quả AI grading để phân tích
        /// </summary>
        private void StoreAIGradingResult(int questionIndex, AIGradingResult result)
        {
            // TODO: Store to analytics system or session data
            // This can be used for learning analytics and performance tracking
            Debug.Log($"[StoreAIGradingResult] Q{questionIndex + 1}: {result.Percentage}% - {result.Explanation}");
        }

        /// <summary>
        /// Hiển thị feedback
        /// </summary>
        private void ShowFeedback(bool isCorrect, string correctAnswer, string explanation)
        {
            if (!_feedbackPanel) return;

            _feedbackPanel.SetActive(true);
            
            if (_feedbackIcon)
            {
                _feedbackIcon.color = isCorrect ? Color.green : Color.red;
                // TODO: Set appropriate icon sprite
            }
            
            if (_feedbackText)
            {
                string result = isCorrect ? "Chính xác!" : "Chưa đúng";
                _feedbackText.text = $"{result}\n\n<b>Đáp án:</b> {correctAnswer}\n\n<b>Giải thích:</b> {explanation}";
            }
            
            // Update question result
            OnQuestionAnswered?.Invoke(_currentQuestionIndex);
        }

        /// <summary>
        /// Tiếp tục sau khi xem feedback
        /// </summary>
        private void ContinueAfterFeedback()
        {
            if (_feedbackPanel) _feedbackPanel.SetActive(false);
            
            // Move to next question or complete session
            if (_currentQuestionIndex < _questions.Count - 1)
            {
                _currentQuestionIndex++;
                ShowCurrentQuestion();
            }
            else
            {
                CompleteSession();
            }
        }

        /// <summary>
        /// Lưu thời gian làm câu hỏi hiện tại
        /// </summary>
        private void SaveCurrentQuestionTime()
        {
            if (_currentQuestionStartTime > 0)
            {
                float timeSpent = Time.time - _currentQuestionStartTime;
                _questionTimes[_currentQuestionIndex] = timeSpent;
            }
        }

        /// <summary>
        /// Tạm dừng session
        /// </summary>
        private void PauseSession()
        {
            SaveCurrentQuestionTime();
            OnSessionPaused?.Invoke();
        }

        /// <summary>
        /// Thoát session
        /// </summary>
        private void ExitSession()
        {
            SaveCurrentQuestionTime();
            OnSessionExited?.Invoke();
        }

        /// <summary>
        /// Hoàn thành session
        /// </summary>
        private void CompleteSession()
        {
            SaveCurrentQuestionTime();
            
            // Create session results
            var results = CreateSessionResults();
            
            OnSessionCompleted?.Invoke(results);
            Debug.Log("[LearningSessionUI] Session completed");
        }

        /// <summary>
        /// Tạo kết quả session chi tiết cho ResultsUI
        /// </summary>
        private SessionResults CreateSessionResults()
        {
            if (_questions == null || CurrentSession == null)
            {
                return new SessionResults
                {
                    SessionId = Guid.NewGuid().ToString(),
                    TotalQuestions = 0,
                    AnsweredQuestions = 0,
                    UserAnswers = new Dictionary<int, string>(),
                    QuestionTimes = new Dictionary<int, float>(),
                    CompletedAt = DateTime.Now,
                    OverallScore = 0f,
                    OverallAccuracy = 0f,
                    Grade = "F",
                    QuestionResults = new List<QuestionAnalysis>(),
                    PerformanceByType = new Dictionary<QuestionType, PerformanceMetrics>(),
                    TotalTimeSpent = 0f,
                    AverageTimePerQuestion = 0f,
                    FastestQuestion = 0f,
                    SlowestQuestion = 0f,
                    CompletionRate = 0f
                };
            }

            // Tính toán kết quả chi tiết
            var questionResults = new List<QuestionAnalysis>();
            var typePerformance = new Dictionary<QuestionType, PerformanceMetrics>();
            
            float totalScore = 0f;
            int correctAnswers = 0;
            int totalAnswered = 0;
            float totalTime = 0f;
            
            List<float> questionTimes = new List<float>();

            // Xử lý từng câu hỏi
            for (int i = 0; i < _questions.Count; i++)
            {
                var question = _questions[i];
                string userAnswer = _userAnswers.ContainsKey(i) ? _userAnswers[i] : "";
                float timeSpent = _questionTimes.ContainsKey(i) ? _questionTimes[i] : 0f;
                
                // Đánh giá câu trả lời
                var analysis = EvaluateQuestionDetailed(question, userAnswer, timeSpent);
                analysis.QuestionIndex = i;
                questionResults.Add(analysis);
                
                // Cập nhật thống kê tổng thể
                totalScore += analysis.Score;
                if (analysis.IsCorrect) correctAnswers++;
                if (!string.IsNullOrWhiteSpace(userAnswer)) totalAnswered++;
                totalTime += timeSpent;
                questionTimes.Add(timeSpent);
                
                // Cập nhật thống kê theo loại câu hỏi
                UpdateTypePerformance(typePerformance, question.QuestionType, analysis);
            }

            // Tính toán metrics tổng quan
            int totalQuestions = _questions.Count;
            float overallAccuracy = totalQuestions > 0 ? (float)correctAnswers / totalQuestions * 100f : 0f;
            float overallScore = totalQuestions > 0 ? totalScore / totalQuestions : 0f;
            string grade = CalculateGrade(overallScore);
            float averageTime = totalQuestions > 0 ? totalTime / totalQuestions : 0f;
            float completionRate = totalQuestions > 0 ? (float)totalAnswered / totalQuestions * 100f : 0f;
            
            // Thời gian nhanh nhất/chậm nhất
            float fastestTime = questionTimes.Count > 0 ? questionTimes.Where(t => t > 0).DefaultIfEmpty(0).Min() : 0f;
            float slowestTime = questionTimes.Count > 0 ? questionTimes.Max() : 0f;

            return new SessionResults
            {
                SessionId = CurrentSession.Id,
                TotalQuestions = totalQuestions,
                AnsweredQuestions = totalAnswered,
                UserAnswers = new Dictionary<int, string>(_userAnswers),
                QuestionTimes = new Dictionary<int, float>(_questionTimes),
                CompletedAt = DateTime.Now,
                
                // Mới: Thêm thông tin chi tiết cho ResultsUI
                OverallScore = overallScore,
                OverallAccuracy = overallAccuracy,
                Grade = grade,
                QuestionResults = questionResults,
                PerformanceByType = typePerformance,
                TotalTimeSpent = totalTime,
                AverageTimePerQuestion = averageTime,
                FastestQuestion = fastestTime,
                SlowestQuestion = slowestTime,
                CompletionRate = completionRate,
                
                // Thêm metadata
                StartTime = CurrentSession.StartTime,
                ContentTitle = CurrentSession.Content?.Title ?? "",
                ContentType = CurrentSession.Content?.ContentType ?? ContentType.Understanding
            };
        }
        
        /// <summary>
        /// Đánh giá chi tiết một câu hỏi
        /// </summary>
        private QuestionAnalysis EvaluateQuestionDetailed(BaseQuestion question, string userAnswer, float timeSpent)
        {
            var analysis = new QuestionAnalysis
            {
                Question = question,
                UserAnswer = userAnswer,
                TimeSpent = timeSpent,
                SubmittedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(userAnswer))
            {
                analysis.ResultType = QuestionResultType.NotAnswered;
                analysis.Score = 0f;
                analysis.IsCorrect = false;
                analysis.Feedback = "Chưa trả lời";
                return analysis;
            }

            // Kiểm tra xem câu hỏi có cần AI chấm điểm không
            if (question.RequiresAIGrading())
            {
                // Tạm thời giả định 50% cho câu AI grading (sẽ được cập nhật sau)
                analysis.Score = 5f; // 50% of 10 points
                analysis.IsCorrect = false; // Will be determined by AI
                analysis.ResultType = QuestionResultType.Partial;
                analysis.Feedback = "Câu trả lời đang được AI đánh giá...";
            }
            else
            {
                // Chấm điểm tự động
                bool isCorrect = question.ValidateAnswer(userAnswer);
                analysis.IsCorrect = isCorrect;
                analysis.Score = isCorrect ? question.GetMaxScore() : 0f;
                analysis.ResultType = isCorrect ? QuestionResultType.Correct : QuestionResultType.Incorrect;
                analysis.Feedback = isCorrect ? 
                    "Chính xác! " + question.Explanation : 
                    $"Chưa đúng. Đáp án: {question.GetCorrectAnswer()}. {question.Explanation}";
            }

            return analysis;
        }
        
        /// <summary>
        /// Cập nhật performance theo loại câu hỏi
        /// </summary>
        private void UpdateTypePerformance(Dictionary<QuestionType, PerformanceMetrics> typePerformance, 
                                         QuestionType questionType, QuestionAnalysis analysis)
        {
            if (!typePerformance.ContainsKey(questionType))
            {
                typePerformance[questionType] = new PerformanceMetrics
                {
                    QuestionType = questionType,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    IncorrectAnswers = 0,
                    PartialAnswers = 0,
                    NotAnswered = 0,
                    TotalScore = 0f,
                    TotalTime = 0f
                };
            }

            var metrics = typePerformance[questionType];
            metrics.TotalQuestions++;
            metrics.TotalScore += analysis.Score;
            metrics.TotalTime += analysis.TimeSpent;

            switch (analysis.ResultType)
            {
                case QuestionResultType.Correct:
                    metrics.CorrectAnswers++;
                    break;
                case QuestionResultType.Incorrect:
                    metrics.IncorrectAnswers++;
                    break;
                case QuestionResultType.Partial:
                    metrics.PartialAnswers++;
                    break;
                case QuestionResultType.NotAnswered:
                    metrics.NotAnswered++;
                    break;
            }

            // Tính lại accuracy
            metrics.Accuracy = metrics.TotalQuestions > 0 ? 
                (float)metrics.CorrectAnswers / metrics.TotalQuestions * 100f : 0f;
            metrics.AverageScore = metrics.TotalQuestions > 0 ? 
                metrics.TotalScore / metrics.TotalQuestions : 0f;
            metrics.AverageTime = metrics.TotalQuestions > 0 ? 
                metrics.TotalTime / metrics.TotalQuestions : 0f;
        }
        
        /// <summary>
        /// Tính grade dựa trên điểm số
        /// </summary>
        private string CalculateGrade(float averageScore)
        {
            return averageScore switch
            {
                >= 9.0f => "A+",
                >= 8.5f => "A",
                >= 8.0f => "A-",
                >= 7.5f => "B+",
                >= 7.0f => "B",
                >= 6.5f => "B-",
                >= 6.0f => "C+",
                >= 5.5f => "C",
                >= 5.0f => "C-",
                >= 4.0f => "D",
                _ => "F"
            };
        }

        /// <summary>
        /// Hiển thị/ẩn loading
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (_loadingPanel) _loadingPanel.SetActive(show);
        }

        private void UnsubscribeEvents()
        {
            if (_questionDisplay)
                _questionDisplay.OnAnswerChanged -= OnAnswerChanged;
        }

        /// <summary>
        /// Get session progress info
        /// </summary>
        public string GetProgressInfo()
        {
            if (_questions == null) return "0/0";
            return $"{_currentQuestionIndex + 1}/{_questions.Count}";
        }
    }

    /// <summary>
    /// Kết quả session
    /// </summary>
    [Serializable]
    public class SessionResults
    {
        // Basic session info
        public string SessionId { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public Dictionary<int, string> UserAnswers { get; set; }
        public Dictionary<int, float> QuestionTimes { get; set; }
        public DateTime CompletedAt { get; set; }
        
        // Detailed results - NEW
        public float OverallScore { get; set; } // Average score out of 10
        public float OverallAccuracy { get; set; } // Percentage correct
        public string Grade { get; set; } // A+, A, B+, etc.
        public List<QuestionAnalysis> QuestionResults { get; set; }
        public Dictionary<QuestionType, PerformanceMetrics> PerformanceByType { get; set; }
        
        // Time analytics - NEW
        public float TotalTimeSpent { get; set; } // Total seconds
        public float AverageTimePerQuestion { get; set; } // Average seconds per question
        public float FastestQuestion { get; set; } // Fastest question time
        public float SlowestQuestion { get; set; } // Slowest question time
        public float CompletionRate { get; set; } // Percentage of questions answered
        
        // Session metadata - NEW
        public DateTime StartTime { get; set; }
        public string ContentTitle { get; set; }
        public ContentType ContentType { get; set; }
        
        // Calculated properties
        public float CompletionPercentage => TotalQuestions > 0 ? (float)AnsweredQuestions / TotalQuestions * 100f : 0f;
        
        /// <summary>
        /// Số câu đúng
        /// </summary>
        public int CorrectAnswers => QuestionResults?.Count(q => q.IsCorrect) ?? 0;
        
        /// <summary>
        /// Số câu sai
        /// </summary>
        public int IncorrectAnswers => QuestionResults?.Count(q => q.ResultType == QuestionResultType.Incorrect) ?? 0;
        
        /// <summary>
        /// Số câu đúng một phần
        /// </summary>
        public int PartialAnswers => QuestionResults?.Count(q => q.ResultType == QuestionResultType.Partial) ?? 0;
        
        /// <summary>
        /// Số câu chưa trả lời
        /// </summary>
        public int UnansweredQuestions => QuestionResults?.Count(q => q.ResultType == QuestionResultType.NotAnswered) ?? 0;
        
        /// <summary>
        /// Có phải session hoàn thành tốt không (>= 70% accuracy)
        /// </summary>
        public bool IsGoodSession => OverallAccuracy >= 70f;
        
        /// <summary>
        /// Màu sắc đại diện cho grade
        /// </summary>
        public Color GradeColor
        {
            get
            {
                return Grade switch
                {
                    "A+" or "A" => new Color(0f, 0.8f, 0f, 1f), // Green
                    "A-" or "B+" or "B" => new Color(0.5f, 0.8f, 0f, 1f), // Yellow-Green
                    "B-" or "C+" or "C" => new Color(0.8f, 0.8f, 0f, 1f), // Yellow
                    "C-" or "D" => new Color(0.8f, 0.5f, 0f, 1f), // Orange
                    _ => new Color(0.8f, 0f, 0f, 1f) // Red
                };
            }
        }
        
        /// <summary>
        /// Thời gian làm bài định dạng readable (VD: "5 phút 30 giây")
        /// </summary>
        public string FormattedTotalTime
        {
            get
            {
                var totalSeconds = (int)TotalTimeSpent;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                
                if (minutes > 0)
                    return $"{minutes} phút {seconds} giây";
                else
                    return $"{seconds} giây";
            }
        }
        
        /// <summary>
        /// Thời gian trung bình mỗi câu định dạng readable
        /// </summary>
        public string FormattedAverageTime
        {
            get
            {
                var avgSeconds = (int)AverageTimePerQuestion;
                if (avgSeconds >= 60)
                    return $"{avgSeconds / 60} phút {avgSeconds % 60} giây";
                else
                    return $"{avgSeconds} giây";
            }
        }
    }
    
    /// <summary>
    /// JSON response format cho AI grading
    /// </summary>
    [Serializable]
    public class AIGradingJsonResponse
    {
        public float percentage;
        public string explanation;
        public string suggestions;
    }
    
    /// <summary>
    /// Phân tích chi tiết kết quả từng câu hỏi
    /// </summary>
    [Serializable]
    public class QuestionAnalysis
    {
        public int QuestionIndex { get; set; }
        public BaseQuestion Question { get; set; }
        public string UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public float Score { get; set; }
        public QuestionResultType ResultType { get; set; }
        public string Feedback { get; set; }
        public float TimeSpent { get; set; }
        public DateTime SubmittedAt { get; set; }
        public AIGradingResult AIResult { get; set; } // Nếu có AI grading
        
        /// <summary>
        /// Tỷ lệ phần trăm điểm số (0-100)
        /// </summary>
        public float ScorePercentage => Question != null ? (Score / Question.GetMaxScore()) * 100f : 0f;
        
        /// <summary>
        /// Có phải câu trả lời tốt không (>= 70%)
        /// </summary>
        public bool IsGoodAnswer => ScorePercentage >= 70f;
    }
    
    /// <summary>
    /// Metrics hiệu suất theo loại câu hỏi
    /// </summary>
    [Serializable]
    public class PerformanceMetrics
    {
        public QuestionType QuestionType { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int PartialAnswers { get; set; }
        public int NotAnswered { get; set; }
        public float TotalScore { get; set; }
        public float TotalTime { get; set; }
        
        // Calculated properties
        public float Accuracy { get; set; }
        public float AverageScore { get; set; }
        public float AverageTime { get; set; }
        public float CompletionRate => TotalQuestions > 0 ? (float)(TotalQuestions - NotAnswered) / TotalQuestions * 100f : 0f;
        
        /// <summary>
        /// Tên hiển thị của loại câu hỏi
        /// </summary>
        public string DisplayName => QuestionType.GetVietnameseName();
        
        /// <summary>
        /// Có phải performance tốt không (>= 70% accuracy)
        /// </summary>
        public bool IsGoodPerformance => Accuracy >= 70f;
        
        /// <summary>
        /// Level hiệu suất: Excellent, Good, Fair, Poor
        /// </summary>
        public string PerformanceLevel
        {
            get
            {
                return Accuracy switch
                {
                    >= 90f => "Xuất sắc",
                    >= 70f => "Tốt",
                    >= 50f => "Trung bình",
                    _ => "Cần cải thiện"
                };
            }
        }
        
        /// <summary>
        /// Màu sắc đại diện cho performance level
        /// </summary>
        public Color PerformanceColor
        {
            get
            {
                return Accuracy switch
                {
                    >= 90f => new Color(0f, 0.8f, 0f, 1f), // Green
                    >= 70f => new Color(0.5f, 0.8f, 0f, 1f), // Yellow-Green  
                    >= 50f => new Color(0.8f, 0.8f, 0f, 1f), // Yellow
                    _ => new Color(0.8f, 0f, 0f, 1f) // Red
                };
            }
        }
    }
    
    /// <summary>
    /// Enum cho loại kết quả câu hỏi
    /// </summary>
    public enum QuestionResultType
    {
        Correct,
        Incorrect, 
        Partial,
        NotAnswered
    }
}
