using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.Data.Models;
using AISmartRecall.Data.Models.Questions;
using AISmartRecall.Services.AI;
using AISmartRecall.UI.Learning;
using AISmartRecall.API.Services;
using AISmartRecall.Systems.Learning;

namespace AISmartRecall.Managers
{
    /// <summary>
    /// Manager chính cho Learning Scene - quản lý toàn bộ learning flow
    /// </summary>
    public class LearningSceneManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private ContentInputUI _contentInputUI;
        [SerializeField] private QuestionSelectionUI _questionSelectionUI;
        [SerializeField] private GameObject _learningSessionPanel;
        [SerializeField] private GameObject _resultsPanel;

        [Header("Learning UI Components")]
        [SerializeField] private LearningSessionUI _learningSessionUI;
        [SerializeField] private QuestionDisplayUI _questionDisplayUI;
        [SerializeField] private ProgressTrackerUI _progressTrackerUI;
        [SerializeField] private ResultsUI _resultsUI;

        [Header("Systems")]
        [SerializeField] private OpenRouterClient _openRouterClient;
        [SerializeField] private AnswerValidationSystem _answerValidationSystem;

        // State Management
        private LearningSceneState _currentState;
        private ContentData _currentContent;
        private List<BaseQuestion> _generatedQuestions;
        private LearningSession _currentLearningSession;

        // Singleton
        public static LearningSceneManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeComponents();
        }

        private void Start()
        {
            SubscribeToEvents();
            SetInitialState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region Initialization

        /// <summary>
        /// Khởi tạo các components cần thiết
        /// </summary>
        private void InitializeComponents()
        {
            // Ensure OpenRouter client exists
            if (_openRouterClient == null)
            {
                _openRouterClient = FindObjectOfType<OpenRouterClient>();
                
                if (_openRouterClient == null)
                {
                    var clientGO = new GameObject("OpenRouterClient");
                    _openRouterClient = clientGO.AddComponent<OpenRouterClient>();
                }
            }

            // Lấy session data từ scene trước
            InitializeWithSessionData();
        }
        
        /// <summary>
        /// Khởi tạo với session data từ Authentication scene
        /// </summary>
        private void InitializeWithSessionData()
        {
            try
            {
                // Kiểm tra và lấy session data
                if (!SessionDataManager.HasValidSessionData())
                {
                    Debug.LogWarning("[LearningScene] No valid session data found! User may have accessed Learning scene directly.");
                    // Fallback: thử lấy API key từ APIKeyManager
                    string fallbackAPIKey = APIKeyManager.GetValidatedAPIKey();
                    if (!string.IsNullOrEmpty(fallbackAPIKey))
                    {
                        _openRouterClient.SetAPIKey(fallbackAPIKey);
                        Debug.Log("[LearningScene] Using fallback API key from APIKeyManager");
                    }
                    return;
                }
                
                var sessionData = SessionDataManager.GetSessionData();
                
                // Set API key cho OpenRouter client
                if (!string.IsNullOrEmpty(sessionData.APIKey))
                {
                    _openRouterClient.SetAPIKey(sessionData.APIKey);
                    Debug.Log($"[LearningScene] OpenRouter client initialized with API key from session");
                }
                else
                {
                    Debug.LogError("[LearningScene] No API key found in session data!");
                }
                
                Debug.Log($"[LearningScene] Initialized with session data: {SessionDataManager.GetSessionDataInfo()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LearningScene] Error initializing with session data: {ex.Message}");
            }
        }

        /// <summary>
        /// Đăng ký events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Content Input events
            ContentInputUI.OnContentReady += OnContentReady;
            ContentInputUI.OnBackRequested += OnBackToMainMenu;
            
            // Question Selection events
            QuestionSelectionUI.OnGenerateQuestionsRequested += OnGenerateQuestionsRequested;
            QuestionSelectionUI.OnBackRequested += OnBackToContentInput;
            
            // Learning Session events
            LearningSessionUI.OnQuestionAnswered += OnQuestionAnswered;
            LearningSessionUI.OnSessionPaused += OnSessionPaused;
            LearningSessionUI.OnSessionCompleted += OnSessionCompleted;
            LearningSessionUI.OnSessionExited += OnSessionExited;
            
            // Results events
            ResultsUI.OnRetryRequested += OnRetryRequested;
            ResultsUI.OnContinueRequested += OnContinueRequested;
            ResultsUI.OnResultsClosed += OnResultsClosed;
        }

        /// <summary>
        /// Hủy đăng ký events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // Content Input events
            ContentInputUI.OnContentReady -= OnContentReady;
            ContentInputUI.OnBackRequested -= OnBackToMainMenu;
            
            // Question Selection events
            QuestionSelectionUI.OnGenerateQuestionsRequested -= OnGenerateQuestionsRequested;
            QuestionSelectionUI.OnBackRequested -= OnBackToContentInput;

            // Learning Session events
            LearningSessionUI.OnQuestionAnswered -= OnQuestionAnswered;
            LearningSessionUI.OnSessionPaused -= OnSessionPaused;
            LearningSessionUI.OnSessionCompleted -= OnSessionCompleted;
            LearningSessionUI.OnSessionExited -= OnSessionExited;

            // Results events
            ResultsUI.OnRetryRequested -= OnRetryRequested;
            ResultsUI.OnContinueRequested -= OnContinueRequested;
            ResultsUI.OnResultsClosed -= OnResultsClosed;
        }

        /// <summary>
        /// Set trạng thái ban đầu
        /// </summary>
        private void SetInitialState()
        {
            // Kiểm tra xem có session data không
            if (SessionDataManager.HasValidSessionData())
            {
                var sessionData = SessionDataManager.GetSessionData();
                Debug.Log($"[LearningScene] Starting with valid session from {sessionData.SourceScene}");
                
                // Hiển thị welcome message nếu cần
                ShowWelcomeMessage(sessionData);
            }
            else
            {
                Debug.LogWarning("[LearningScene] No session data - user may have accessed directly");
            }
            
            ChangeState(LearningSceneState.ContentInput);
        }
        
        /// <summary>
        /// Hiển thị thông điệp chào mừng
        /// </summary>
        private void ShowWelcomeMessage(SessionData sessionData)
        {
            string welcomeMessage = $"Chào mừng {sessionData.DisplayName}! Sẵn sàng bắt đầu học tập với chế độ {sessionData.SelectedLearningMode}?";
            Debug.Log($"[LearningScene] {welcomeMessage}");
            
            // TODO: Hiển thị welcome popup hoặc notification nếu cần
        }

        #endregion

        #region State Management

        /// <summary>
        /// Thay đổi state của scene
        /// </summary>
        private void ChangeState(LearningSceneState newState)
        {
            Debug.Log($"[LearningScene] State changed: {_currentState} -> {newState}");
            
            _currentState = newState;
            UpdateUI();
        }

        /// <summary>
        /// Update UI theo state hiện tại
        /// </summary>
        private void UpdateUI()
        {
            // Hide all panels
            if (_contentInputUI) _contentInputUI.SetActive(false);
            if (_questionSelectionUI) _questionSelectionUI.gameObject.SetActive(false);
            if (_learningSessionPanel) _learningSessionPanel.SetActive(false);
            if (_resultsPanel) _resultsPanel.SetActive(false);

            // Show appropriate panel
            switch (_currentState)
            {
                case LearningSceneState.ContentInput:
                    if (_contentInputUI) _contentInputUI.SetActive(true);
                    break;

                case LearningSceneState.QuestionSelection:
                    if (_questionSelectionUI) 
                    {
                        _questionSelectionUI.gameObject.SetActive(true);
                        _questionSelectionUI.Setup(_currentContent);
                    }
                    break;

                case LearningSceneState.QuestionGeneration:
                    // Loading state - handled by QuestionSelectionUI
                    break;

                case LearningSceneState.LearningSession:
                    if (_learningSessionPanel) _learningSessionPanel.SetActive(true);
                    break;

                case LearningSceneState.Results:
                    if (_resultsPanel) _resultsPanel.SetActive(true);
                    break;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start learning flow với content đã có
        /// </summary>
        public void StartLearningWithContent(ContentData content)
        {
            _currentContent = content;
            ChangeState(LearningSceneState.QuestionSelection);
        }

        /// <summary>
        /// Start learning flow với content input
        /// </summary>
        public void StartLearningWithInput()
        {
            ChangeState(LearningSceneState.ContentInput);
        }

        /// <summary>
        /// Set API key cho OpenRouter client
        /// </summary>
        public void SetAPIKey(string apiKey)
        {
            if (_openRouterClient != null)
            {
                _openRouterClient.SetAPIKey(apiKey);
                Debug.Log("[LearningScene] API key updated");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Xử lý khi user yêu cầu generate questions
        /// </summary>
        private async void OnGenerateQuestionsRequested(ContentData content, List<QuestionGenerationRequest> requests)
        {
            _currentContent = content;
            ChangeState(LearningSceneState.QuestionGeneration);

            try
            {
                Debug.Log($"[LearningScene] Starting question generation for {requests.Count} types");

                // Generate questions using OpenRouter
                var questionsDict = await GenerateQuestionsAsync(content, requests);
                
                // Flatten questions from all types
                _generatedQuestions = new List<BaseQuestion>();
                foreach (var kvp in questionsDict)
                {
                    _generatedQuestions.AddRange(kvp.Value);
                }

                Debug.Log($"[LearningScene] Generated {_generatedQuestions.Count} questions total");

                // Hide loading và chuyển sang learning session
                if (_questionSelectionUI) _questionSelectionUI.HideLoading();
                
                if (_generatedQuestions.Count > 0)
                {
                    StartLearningSession();
                }
                else
                {
                    ShowError("Không thể tạo câu hỏi. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LearningScene] Question generation failed: {ex.Message}");
                ShowError($"Lỗi tạo câu hỏi: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý khi content input đã sẵn sàng
        /// </summary>
        private void OnContentReady(ContentData content)
        {
            _currentContent = content;
            ChangeState(LearningSceneState.QuestionSelection);
            
            Debug.Log($"[LearningScene] Content ready, moving to question selection: {content.Title}");
        }
        
        /// <summary>
        /// Xử lý khi user muốn quay lại main menu từ content input
        /// </summary>
        private void OnBackToMainMenu()
        {
            Debug.Log("[LearningScene] Back to main menu requested");
            
            try
            {
                // Clear current session data
                SessionDataManager.ClearSessionData();
                
                // Kiểm tra scene nào cần load
                string targetScene = "Feature"; // Default scene
                
                // Nếu có session data, sử dụng source scene
                if (SessionDataManager.HasValidSessionData())
                {
                    var sessionData = SessionDataManager.GetSessionData();
                    targetScene = sessionData.SourceScene ?? "Feature";
                }
                
                Debug.Log($"[LearningScene] Loading scene: {targetScene}");
                
                // Load scene bằng UnityEngine.SceneManagement
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LearningScene] Error loading main menu: {ex.Message}");
                
                // Fallback: load Authentication scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("Feature");
            }
        }
        
        /// <summary>
        /// Xử lý khi user muốn quay lại content input
        /// </summary>
        private void OnBackToContentInput()
        {
            ChangeState(LearningSceneState.ContentInput);
        }
        
        #region Learning Session Events
        
        /// <summary>
        /// Xử lý khi user trả lời câu hỏi
        /// </summary>
        private void OnQuestionAnswered(int questionIndex)
        {
            Debug.Log($"[LearningScene] Question {questionIndex} answered");
            // Additional logic if needed
        }
        
        /// <summary>
        /// Xử lý khi session được pause
        /// </summary>
        private void OnSessionPaused()
        {
            Debug.Log("[LearningScene] Session paused");
            // Could save progress, show pause menu, etc.
        }
        
        /// <summary>
        /// Xử lý khi session hoàn thành
        /// </summary>
        private void OnSessionCompleted(SessionResults results)
        {
            Debug.Log("[LearningScene] Session completed");
            ShowResults(results);
        }
        
        /// <summary>
        /// Xử lý khi user thoát session
        /// </summary>
        private void OnSessionExited()
        {
            Debug.Log("[LearningScene] Session exited");
            // Could show confirmation dialog, save progress, etc.
            ChangeState(LearningSceneState.QuestionSelection);
        }
        
        #endregion
        
        #region Results Events
        
        /// <summary>
        /// Xử lý khi user muốn retry session
        /// </summary>
        private void OnRetryRequested()
        {
            Debug.Log("[LearningScene] Retry requested");
            // Reset session and restart with same questions
            if (_generatedQuestions?.Count > 0)
            {
                StartLearningSession();
            }
            else
            {
                ChangeState(LearningSceneState.QuestionSelection);
            }
        }
        
        /// <summary>
        /// Xử lý khi user muốn continue (new session)
        /// </summary>
        private void OnContinueRequested()
        {
            Debug.Log("[LearningScene] Continue requested");
            // Start new session - go back to question selection
            ChangeState(LearningSceneState.QuestionSelection);
        }
        
        /// <summary>
        /// Xử lý khi results được đóng
        /// </summary>
        private void OnResultsClosed()
        {
            Debug.Log("[LearningScene] Results closed");
            // Default action - go back to question selection
            ChangeState(LearningSceneState.ContentInput);
        }
        
        #endregion

        #endregion

        #region Question Generation

        /// <summary>
        /// Generate questions bằng AI
        /// </summary>
        private async UniTask<Dictionary<QuestionType, List<BaseQuestion>>> GenerateQuestionsAsync(
            ContentData content, 
            List<QuestionGenerationRequest> requests)
        {
            if (_openRouterClient == null)
            {
                throw new Exception("OpenRouter client chưa được khởi tạo");
            }

            // Update progress
            int totalRequests = requests.Count;
            int completedRequests = 0;

            var results = new Dictionary<QuestionType, List<BaseQuestion>>();

            foreach (var request in requests)
            {
                try
                {
                    // Update loading progress
                    float progress = (float)completedRequests / totalRequests;
                    string message = $"Đang tạo câu hỏi {request.QuestionType.GetVietnameseName()}...";
                    
                    if (_questionSelectionUI) 
                        _questionSelectionUI.UpdateLoadingProgress(progress, message);

                    // Generate questions for this type
                    var questions = await _openRouterClient.GenerateQuestionsByType(content, request.QuestionType, request.Count);
                    results[request.QuestionType] = questions;

                    Debug.Log($"[LearningScene] Generated {questions.Count} questions for {request.QuestionType}");

                    completedRequests++;
                    
                    // Small delay to show progress
                    await UniTask.Delay(500);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LearningScene] Failed to generate {request.QuestionType}: {ex.Message}");
                    results[request.QuestionType] = new List<BaseQuestion>();
                    completedRequests++;
                }
            }

            // Final progress update
            if (_questionSelectionUI) 
                _questionSelectionUI.UpdateLoadingProgress(1f, "Hoàn thành tạo câu hỏi!");

            return results;
        }

        #endregion

        #region Learning Session

        /// <summary>
        /// Bắt đầu learning session
        /// </summary>
        private void StartLearningSession()
        {
            _currentLearningSession = new LearningSession(_currentContent, _generatedQuestions);
            ChangeState(LearningSceneState.LearningSession);
            
            Debug.Log($"[LearningScene] Started learning session with {_generatedQuestions.Count} questions");
            
            // Initialize LearningSessionUI với session
            if (_learningSessionUI)
            {
                _learningSessionUI.StartSession(_currentLearningSession);
            }
        }

        /// <summary>
        /// Kết thúc learning session
        /// </summary>
        private void EndLearningSession()
        {
            ChangeState(LearningSceneState.Results);
            // ShowResults();
            Debug.Log("[LearningScene] Learning session ended");
        }
        
        /// <summary>
        /// Hiển thị kết quả session
        /// </summary>
        private void ShowResults(SessionResults results)
        {
            if (_currentLearningSession == null || _resultsUI == null)
            {
                Debug.LogError("[LearningScene] Cannot show results - session or UI is null");
                return;
            }
            
            // Create SessionResults from LearningSession
            var sessionResults = results;
            
            // Show results UI
            _resultsUI.ShowResults(sessionResults, _currentLearningSession.Questions, sessionResults.UserAnswers);
            
            Debug.Log($"[LearningScene] Results displayed - Score: {sessionResults.CompletionPercentage:F1}%");
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Hiển thị error message
        /// </summary>
        private void ShowError(string errorMessage)
        {
            Debug.LogError($"[LearningScene] {errorMessage}");
            
            if (_questionSelectionUI) 
            {
                _questionSelectionUI.ShowError(errorMessage);
            }
            
            // Return to question selection after 3 seconds
            _ = ReturnToQuestionSelectionAfterDelay(3f);
        }

        /// <summary>
        /// Quay lại question selection sau delay
        /// </summary>
        private async UniTaskVoid ReturnToQuestionSelectionAfterDelay(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            if (_questionSelectionUI) 
            {
                _questionSelectionUI.HideLoading();
            }
        }

        #endregion
    }

    /// <summary>
    /// Enum định nghĩa các trạng thái của Learning Scene
    /// </summary>
    public enum LearningSceneState
    {
        /// <summary>
        /// Đang nhập nội dung học tập
        /// </summary>
        ContentInput,

        /// <summary>
        /// Đang chọn loại câu hỏi và số lượng
        /// </summary>
        QuestionSelection,

        /// <summary>
        /// Đang generate câu hỏi bằng AI
        /// </summary>
        QuestionGeneration,

        /// <summary>
        /// Đang trong phiên học tập
        /// </summary>
        LearningSession,

        /// <summary>
        /// Hiển thị kết quả học tập
        /// </summary>
        Results
    }

    /// <summary>
    /// Model cho learning session
    /// </summary>
    [Serializable]
    public class LearningSession
    {
        [Header("Session Info")]
        [SerializeField] private string _id;
        [SerializeField] private ContentData _content;
        [SerializeField] private List<BaseQuestion> _questions;
        [SerializeField] private DateTime _startTime;
        [SerializeField] private DateTime _endTime;

        [Header("Progress")]
        [SerializeField] private int _currentQuestionIndex;
        [SerializeField] private List<QuestionResult> _results;
        [SerializeField] private float _totalScore;

        // Properties
        public string Id => _id;
        public ContentData Content => _content;
        public List<BaseQuestion> Questions => _questions;
        public DateTime StartTime => _startTime;
        public DateTime EndTime => _endTime;
        public int CurrentQuestionIndex => _currentQuestionIndex;
        public List<QuestionResult> Results => _results;
        public float TotalScore => _totalScore;

        // Computed properties
        public bool IsCompleted => _currentQuestionIndex >= _questions.Count;
        public int TotalQuestions => _questions?.Count ?? 0;
        public float ProgressPercentage => TotalQuestions > 0 ? (float)_currentQuestionIndex / TotalQuestions : 0f;
        public TimeSpan Duration => _endTime > _startTime ? _endTime - _startTime : TimeSpan.Zero;

        /// <summary>
        /// Constructor
        /// </summary>
        public LearningSession(ContentData content, List<BaseQuestion> questions)
        {
            _id = Guid.NewGuid().ToString();
            _content = content;
            _questions = questions ?? new List<BaseQuestion>();
            _startTime = DateTime.Now;
            _currentQuestionIndex = 0;
            _results = new List<QuestionResult>();
            _totalScore = 0f;
        }

        /// <summary>
        /// Lấy câu hỏi hiện tại
        /// </summary>
        public BaseQuestion GetCurrentQuestion()
        {
            if (IsCompleted || _questions == null || _currentQuestionIndex < 0)
                return null;

            return _currentQuestionIndex < _questions.Count ? _questions[_currentQuestionIndex] : null;
        }

        /// <summary>
        /// Submit answer cho câu hỏi hiện tại
        /// </summary>
        public void SubmitAnswer(string userAnswer, float timeSpent)
        {
            var currentQuestion = GetCurrentQuestion();
            if (currentQuestion == null) return;

            var result = new QuestionResult
            {
                Question = currentQuestion,
                UserAnswer = userAnswer,
                TimeSpent = timeSpent,
                SubmittedAt = DateTime.Now
            };

            _results.Add(result);
            _currentQuestionIndex++;

            // Check if session completed
            if (IsCompleted)
            {
                _endTime = DateTime.Now;
                CalculateFinalScore();
            }
        }

        /// <summary>
        /// Tính điểm cuối cùng
        /// </summary>
        private void CalculateFinalScore()
        {
            _totalScore = 0f;
            
            foreach (var result in _results)
            {
                if (result.Score.HasValue)
                {
                    _totalScore += result.Score.Value;
                }
            }
        }
    }

    /// <summary>
    /// Kết quả cho một câu hỏi
    /// </summary>
    [Serializable]
    public class QuestionResult
    {
        public BaseQuestion Question { get; set; }
        public string UserAnswer { get; set; }
        public float TimeSpent { get; set; }
        public DateTime SubmittedAt { get; set; }
        public float? Score { get; set; } // Null nếu chưa được chấm
        public AIGradingResult GradingResult { get; set; } // Cho câu tự luận
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Kiểm tra xem câu trả lời có đúng không
        /// </summary>
        public bool CheckAnswer()
        {
            if (Question == null) return false;

            if (Question.RequiresAIGrading())
            {
                // Sẽ được chấm bằng AI sau
                return false;
            }
            else
            {
                // Chấm tự động
                IsCorrect = Question.ValidateAnswer(UserAnswer);
                Score = IsCorrect ? Question.GetMaxScore() : 0f;
                return IsCorrect;
            }
        }
    }
}
