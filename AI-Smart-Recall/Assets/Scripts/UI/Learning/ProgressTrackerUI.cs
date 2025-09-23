using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// Component theo dõi và hiển thị tiến trình học tập - progress bar, timer, số câu đã làm
    /// </summary>
    public class ProgressTrackerUI : MonoBehaviour
    {
        [Header("Progress Display")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _progressPercentageText;

        [Header("Question Counter")]
        [SerializeField] private TextMeshProUGUI _currentQuestionText;
        [SerializeField] private TextMeshProUGUI _totalQuestionsText;
        [SerializeField] private TextMeshProUGUI _answeredQuestionsText;
        
        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI _elapsedTimeText;
        [SerializeField] private TextMeshProUGUI _estimatedTimeText;
        [SerializeField] private Image _timerIcon;
        [SerializeField] private Color _timerNormalColor = Color.white;
        [SerializeField] private Color _timerWarningColor = Color.yellow;
        [SerializeField] private Color _timerDangerColor = Color.red;

        [Header("Visual Elements")]
        [SerializeField] private Image _progressFillImage;
        [SerializeField] private Color _progressStartColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _progressEndColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private Animation _progressAnimation;

        [Header("Milestones")]
        [SerializeField] private Transform _milestonesParent;
        [SerializeField] private GameObject _milestonePrefab;
        [SerializeField] private float[] _milestonePercentages = { 25f, 50f, 75f, 100f };

        // Properties
        public float CurrentProgress { get; private set; }
        public int CurrentQuestionIndex { get; private set; }
        public int TotalQuestions { get; private set; }
        public int AnsweredQuestions { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }

        // Private fields
        private DateTime _sessionStartTime;
        private bool _isSessionActive = false;
        private float _averageTimePerQuestion = 30f; // seconds
        private GameObject[] _milestoneMarkers;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeUI();
        }

        private void Update()
        {
            if (_isSessionActive)
            {
                UpdateElapsedTime();
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Khởi tạo UI components
        /// </summary>
        private void InitializeUI()
        {
            // Initialize progress slider
            if (_progressSlider)
            {
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = 1f;
                _progressSlider.value = 0f;
            }

            // Create milestone markers
            CreateMilestoneMarkers();

            // Initial display
            ResetProgress();
        }

        /// <summary>
        /// Tạo milestone markers trên progress bar
        /// </summary>
        private void CreateMilestoneMarkers()
        {
            if (_milestonesParent == null || _milestonePrefab == null) return;

            // Clear existing milestones
            foreach (Transform child in _milestonesParent)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create new milestone markers
            _milestoneMarkers = new GameObject[_milestonePercentages.Length];
            
            for (int i = 0; i < _milestonePercentages.Length; i++)
            {
                var milestoneObj = Instantiate(_milestonePrefab, _milestonesParent);
                var rectTransform = milestoneObj.GetComponent<RectTransform>();
                
                if (rectTransform)
                {
                    // Position milestone marker
                    float normalizedPosition = _milestonePercentages[i] / 100f;
                    rectTransform.anchorMin = new Vector2(normalizedPosition, 0f);
                    rectTransform.anchorMax = new Vector2(normalizedPosition, 1f);
                    rectTransform.anchoredPosition = Vector2.zero;
                }

                // Add milestone text
                var milestoneText = milestoneObj.GetComponentInChildren<TextMeshProUGUI>();
                if (milestoneText)
                {
                    milestoneText.text = $"{_milestonePercentages[i]:F0}%";
                }

                _milestoneMarkers[i] = milestoneObj;
            }
        }
        #endregion

        #region Progress Management
        /// <summary>
        /// Reset progress về trạng thái ban đầu
        /// </summary>
        public void ResetProgress()
        {
            CurrentProgress = 0f;
            CurrentQuestionIndex = 0;
            TotalQuestions = 0;
            AnsweredQuestions = 0;
            ElapsedTime = TimeSpan.Zero;
            _isSessionActive = false;

            UpdateProgressDisplay();
            UpdateQuestionCounter();
            UpdateTimerDisplay();
        }

        /// <summary>
        /// Bắt đầu tracking session
        /// </summary>
        /// <param name="startTime">Thời gian bắt đầu session</param>
        public void StartSession(DateTime startTime)
        {
            _sessionStartTime = startTime;
            _isSessionActive = true;
            
            Debug.Log("[ProgressTrackerUI] Session tracking started");
        }

        /// <summary>
        /// Cập nhật progress
        /// </summary>
        /// <param name="currentQuestionIndex">Index câu hỏi hiện tại (0-based)</param>
        /// <param name="totalQuestions">Tổng số câu hỏi</param>
        /// <param name="answeredQuestions">Số câu đã trả lời</param>
        /// <param name="sessionStartTime">Thời gian bắt đầu session</param>
        public void UpdateProgress(int currentQuestionIndex, int totalQuestions, int answeredQuestions, DateTime sessionStartTime)
        {
            CurrentQuestionIndex = currentQuestionIndex;
            TotalQuestions = totalQuestions;
            AnsweredQuestions = answeredQuestions;
            _sessionStartTime = sessionStartTime;
            
            if (!_isSessionActive)
            {
                _isSessionActive = true;
            }

            // Calculate progress (based on current question position)
            CurrentProgress = TotalQuestions > 0 ? (float)currentQuestionIndex / TotalQuestions : 0f;
            
            // Update displays
            UpdateProgressDisplay();
            UpdateQuestionCounter();
            UpdateTimerDisplay();
            
            // Trigger milestone effects if needed
            CheckMilestones();
        }

        /// <summary>
        /// Tạm dừng session
        /// </summary>
        public void PauseSession()
        {
            _isSessionActive = false;
            Debug.Log("[ProgressTrackerUI] Session paused");
        }

        /// <summary>
        /// Tiếp tục session
        /// </summary>
        public void ResumeSession()
        {
            _isSessionActive = true;
            Debug.Log("[ProgressTrackerUI] Session resumed");
        }

        /// <summary>
        /// Kết thúc session
        /// </summary>
        public void EndSession()
        {
            _isSessionActive = false;
            CurrentProgress = 1f;
            UpdateProgressDisplay();
            
            Debug.Log("[ProgressTrackerUI] Session ended");
        }
        #endregion

        #region Display Updates
        /// <summary>
        /// Cập nhật hiển thị progress bar
        /// </summary>
        private void UpdateProgressDisplay()
        {
            // Update progress slider
            if (_progressSlider)
            {
                _progressSlider.value = CurrentProgress;
                
                // Animate progress fill color based on completion
                if (_progressFillImage)
                {
                    _progressFillImage.color = Color.Lerp(_progressStartColor, _progressEndColor, CurrentProgress);
                }
            }

            // Update progress text
            if (_progressText)
            {
                int percentage = Mathf.RoundToInt(CurrentProgress * 100f);
                _progressText.text = $"Tiến độ: {percentage}%";
            }

            // Update percentage text
            if (_progressPercentageText)
            {
                _progressPercentageText.text = $"{CurrentProgress * 100f:F1}%";
            }

            // Trigger progress animation if available
            if (_progressAnimation && CurrentProgress > 0)
            {
                _progressAnimation.Play();
            }
        }

        /// <summary>
        /// Cập nhật question counter
        /// </summary>
        private void UpdateQuestionCounter()
        {
            if (_currentQuestionText)
            {
                _currentQuestionText.text = $"Câu {CurrentQuestionIndex + 1}";
            }

            if (_totalQuestionsText)
            {
                _totalQuestionsText.text = $"/ {TotalQuestions}";
            }

            if (_answeredQuestionsText)
            {
                _answeredQuestionsText.text = $"Đã trả lời: {AnsweredQuestions}";
            }
        }

        /// <summary>
        /// Cập nhật timer display
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (_elapsedTimeText)
            {
                _elapsedTimeText.text = FormatTime(ElapsedTime);
            }

            // Calculate and display estimated time
            if (_estimatedTimeText && TotalQuestions > 0)
            {
                var estimatedTotal = TimeSpan.FromSeconds(_averageTimePerQuestion * TotalQuestions);
                var remaining = estimatedTotal.Subtract(ElapsedTime);
                
                if (remaining.TotalSeconds > 0)
                {
                    _estimatedTimeText.text = $"Còn lại: ~{FormatTime(remaining)}";
                }
                else
                {
                    _estimatedTimeText.text = "Vượt dự kiến";
                }
            }

            // Update timer color based on progress
            UpdateTimerColor();
        }

        /// <summary>
        /// Cập nhật màu timer dựa trên thời gian
        /// </summary>
        private void UpdateTimerColor()
        {
            if (_timerIcon == null || TotalQuestions == 0) return;

            float expectedTime = _averageTimePerQuestion * TotalQuestions;
            float timeRatio = (float)(ElapsedTime.TotalSeconds / expectedTime);

            Color timerColor = timeRatio switch
            {
                <= 0.7f => _timerNormalColor,
                <= 1.0f => _timerWarningColor,
                _ => _timerDangerColor
            };

            _timerIcon.color = timerColor;
        }
        #endregion

        #region Time Management
        /// <summary>
        /// Cập nhật elapsed time
        /// </summary>
        private void UpdateElapsedTime()
        {
            if (_sessionStartTime != default)
            {
                ElapsedTime = DateTime.Now - _sessionStartTime;
                UpdateTimerDisplay();
            }
        }

        /// <summary>
        /// Format time thành string dễ đọc
        /// </summary>
        /// <param name="time">TimeSpan cần format</param>
        /// <returns>Formatted time string</returns>
        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }

        /// <summary>
        /// Set average time per question (for estimation)
        /// </summary>
        /// <param name="seconds">Average seconds per question</param>
        public void SetAverageTimePerQuestion(float seconds)
        {
            _averageTimePerQuestion = Mathf.Max(10f, seconds); // Minimum 10 seconds
        }
        #endregion

        #region Milestones
        /// <summary>
        /// Kiểm tra và trigger milestone effects
        /// </summary>
        private void CheckMilestones()
        {
            int currentPercentage = Mathf.RoundToInt(CurrentProgress * 100f);
            
            foreach (float milestone in _milestonePercentages)
            {
                if (currentPercentage >= milestone && HasReachedMilestone(milestone))
                {
                    TriggerMilestoneEffect(milestone);
                }
            }
        }

        /// <summary>
        /// Kiểm tra đã đạt milestone chưa
        /// </summary>
        private bool HasReachedMilestone(float milestonePercentage)
        {
            // Simple check - could be improved with tracking which milestones were already triggered
            return CurrentProgress * 100f >= milestonePercentage;
        }

        /// <summary>
        /// Trigger milestone effect
        /// </summary>
        private void TriggerMilestoneEffect(float milestonePercentage)
        {
            Debug.Log($"[ProgressTrackerUI] Milestone reached: {milestonePercentage}%");
            
            // Find milestone marker and highlight it
            for (int i = 0; i < _milestonePercentages.Length; i++)
            {
                if (Math.Abs(_milestonePercentages[i] - milestonePercentage) < 0.1f)
                {
                    HighlightMilestone(i);
                    break;
                }
            }

            // Could add effects like:
            // - Animation
            // - Sound effects
            // - Celebration particles
            // - Achievement notifications
        }

        /// <summary>
        /// Highlight milestone marker
        /// </summary>
        private void HighlightMilestone(int milestoneIndex)
        {
            if (_milestoneMarkers != null && milestoneIndex < _milestoneMarkers.Length)
            {
                var milestoneObj = _milestoneMarkers[milestoneIndex];
                if (milestoneObj)
                {
                    // Simple highlight effect - change color
                    var image = milestoneObj.GetComponent<Image>();
                    if (image)
                    {
                        image.color = Color.yellow;
                        
                        // Could add animation here
                        // LeanTween.scale(milestoneObj, Vector3.one * 1.2f, 0.5f).setEaseOutBounce();
                    }
                }
            }
        }
        #endregion

        #region Public Utilities
        /// <summary>
        /// Lấy thông tin progress summary
        /// </summary>
        /// <returns>Progress summary string</returns>
        public string GetProgressSummary()
        {
            return $"Câu {CurrentQuestionIndex + 1}/{TotalQuestions} • " +
                   $"Đã trả lời: {AnsweredQuestions} • " +
                   $"Thời gian: {FormatTime(ElapsedTime)} • " +
                   $"Hoàn thành: {CurrentProgress * 100f:F1}%";
        }

        /// <summary>
        /// Lấy estimated completion time
        /// </summary>
        /// <returns>Estimated completion time</returns>
        public TimeSpan GetEstimatedCompletionTime()
        {
            if (TotalQuestions == 0 || CurrentProgress == 0) return TimeSpan.Zero;
            
            double totalEstimatedSeconds = ElapsedTime.TotalSeconds / CurrentProgress;
            return TimeSpan.FromSeconds(totalEstimatedSeconds);
        }

        /// <summary>
        /// Kiểm tra có đang chạy chậm không
        /// </summary>
        /// <returns>True nếu vượt thời gian dự kiến</returns>
        public bool IsRunningLate()
        {
            if (TotalQuestions == 0) return false;
            
            float expectedTime = _averageTimePerQuestion * (CurrentQuestionIndex + 1);
            return ElapsedTime.TotalSeconds > expectedTime;
        }

        /// <summary>
        /// Lấy performance rating
        /// </summary>
        /// <returns>Performance rating từ 1-5</returns>
        public int GetPerformanceRating()
        {
            if (TotalQuestions == 0) return 5;
            
            float efficiency = (float)(AnsweredQuestions) / (CurrentQuestionIndex + 1);
            float timeEfficiency = _averageTimePerQuestion * (CurrentQuestionIndex + 1) / (float)ElapsedTime.TotalSeconds;
            
            float overallScore = (efficiency + timeEfficiency) / 2f;
            
            return overallScore switch
            {
                >= 0.9f => 5,
                >= 0.8f => 4,
                >= 0.7f => 3,
                >= 0.6f => 2,
                _ => 1
            };
        }
        #endregion

        #region Debug & Testing
#if UNITY_EDITOR
        [ContextMenu("Test Progress 25%")]
        private void TestProgress25()
        {
            UpdateProgress(2, 10, 2, DateTime.Now.AddMinutes(-2));
        }

        [ContextMenu("Test Progress 50%")]
        private void TestProgress50()
        {
            UpdateProgress(5, 10, 4, DateTime.Now.AddMinutes(-5));
        }

        [ContextMenu("Test Progress 75%")]
        private void TestProgress75()
        {
            UpdateProgress(7, 10, 6, DateTime.Now.AddMinutes(-8));
        }

        [ContextMenu("Test Complete")]
        private void TestComplete()
        {
            UpdateProgress(9, 10, 10, DateTime.Now.AddMinutes(-12));
            EndSession();
        }
#endif
        #endregion
    }
}
