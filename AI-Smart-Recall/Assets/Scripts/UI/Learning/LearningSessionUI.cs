using System;
using System.Collections.Generic;
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
        public static event Action OnSessionCompleted;
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
            
            OnSessionCompleted?.Invoke();
            Debug.Log("[LearningSessionUI] Session completed");
        }

        /// <summary>
        /// Tạo kết quả session
        /// </summary>
        private SessionResults CreateSessionResults()
        {
            // TODO: Calculate detailed results
            return new SessionResults
            {
                SessionId = CurrentSession?.Id,
                TotalQuestions = _questions?.Count ?? 0,
                AnsweredQuestions = _userAnswers.Count,
                UserAnswers = new Dictionary<int, string>(_userAnswers),
                QuestionTimes = new Dictionary<int, float>(_questionTimes),
                CompletedAt = DateTime.Now
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
        public string SessionId { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public Dictionary<int, string> UserAnswers { get; set; }
        public Dictionary<int, float> QuestionTimes { get; set; }
        public DateTime CompletedAt { get; set; }
        
        public float CompletionPercentage => TotalQuestions > 0 ? (float)AnsweredQuestions / TotalQuestions * 100f : 0f;
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
}
