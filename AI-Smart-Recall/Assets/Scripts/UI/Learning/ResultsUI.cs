using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AISmartRecall.Data.Models.Questions;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// UI hiển thị kết quả học tập, điểm số, phân tích, và recommendations
    /// </summary>
    public class ResultsUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _continueButton;

        [Header("Overall Results")]
        [SerializeField] private TextMeshProUGUI _overallScoreText;
        [SerializeField] private TextMeshProUGUI _gradeText;
        [SerializeField] private Image _gradeIcon;
        [SerializeField] private Slider _scoreProgressBar;
        [SerializeField] private Image _scoreFillImage;

        [Header("Performance Stats")]
        [SerializeField] private TextMeshProUGUI _totalQuestionsText;
        [SerializeField] private TextMeshProUGUI _correctAnswersText;
        [SerializeField] private TextMeshProUGUI _incorrectAnswersText;
        [SerializeField] private TextMeshProUGUI _accuracyPercentageText;
        [SerializeField] private TextMeshProUGUI _totalTimeText;
        [SerializeField] private TextMeshProUGUI _averageTimeText;

        [Header("Question Type Breakdown")]
        [SerializeField] private Transform _questionTypesParent;
        [SerializeField] private GameObject _questionTypeItemPrefab;

        [Header("Performance Chart")]
        [SerializeField] private Transform _chartParent;
        [SerializeField] private GameObject _chartBarPrefab;
        [SerializeField] private RectTransform _chartArea;

        [Header("Detailed Results")]
        [SerializeField] private Transform _detailedResultsParent;
        [SerializeField] private GameObject _questionResultItemPrefab;
        [SerializeField] private ScrollRect _detailedScrollRect;

        [Header("Recommendations")]
        [SerializeField] private TextMeshProUGUI _recommendationsText;
        [SerializeField] private Transform _improvementAreasParent;
        [SerializeField] private GameObject _improvementAreaPrefab;

        [Header("Achievements")]
        [SerializeField] private Transform _achievementsParent;
        [SerializeField] private GameObject _achievementPrefab;

        [Header("Visual Elements")]
        [SerializeField] private Color _correctColor = Color.green;
        [SerializeField] private Color _incorrectColor = Color.red;
        [SerializeField] private Color _partialColor = Color.yellow;
        [SerializeField] private Color _gradeAColor = new Color(0f, 0.8f, 0f, 1f);
        [SerializeField] private Color _gradeBColor = new Color(0.5f, 0.8f, 0f, 1f);
        [SerializeField] private Color _gradeCColor = new Color(0.8f, 0.8f, 0f, 1f);
        [SerializeField] private Color _gradeDColor = new Color(0.8f, 0.5f, 0f, 1f);
        [SerializeField] private Color _gradeFColor = new Color(0.8f, 0f, 0f, 1f);

        // Events
        public static event Action OnRetryRequested;
        public static event Action OnContinueRequested;
        public static event Action OnResultsClosed;

        // Properties
        public bool IsVisible { get; private set; }
        public SessionResults CurrentResults { get; private set; }

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
            if (_closeButton) _closeButton.onClick.AddListener(CloseResults);
            if (_retryButton) _retryButton.onClick.AddListener(RequestRetry);
            if (_continueButton) _continueButton.onClick.AddListener(RequestContinue);

            // Initially hide panel
            SetVisible(false);
        }

        /// <summary>
        /// Hiển thị kết quả
        /// </summary>
        /// <param name="sessionResults">Kết quả session</param>
        /// <param name="questions">Danh sách câu hỏi</param>
        /// <param name="userAnswers">Câu trả lời của user</param>
        public void ShowResults(SessionResults sessionResults, List<BaseQuestion> questions, Dictionary<int, string> userAnswers)
        {
            CurrentResults = sessionResults;
            
            SetVisible(true);
            
            // Calculate detailed results
            var detailedResults = CalculateDetailedResults(questions, userAnswers);
            
            // Update all UI components
            UpdateOverallResults(detailedResults);
            UpdatePerformanceStats(detailedResults, sessionResults);
            UpdateQuestionTypeBreakdown(detailedResults);
            UpdatePerformanceChart(detailedResults);
            UpdateDetailedResults(detailedResults, questions);
            UpdateRecommendations(detailedResults);
            UpdateAchievements(detailedResults, sessionResults);
            
            Debug.Log("[ResultsUI] Results displayed");
        }

        /// <summary>
        /// Tính toán kết quả chi tiết
        /// </summary>
        private DetailedResults CalculateDetailedResults(List<BaseQuestion> questions, Dictionary<int, string> userAnswers)
        {
            var results = new DetailedResults
            {
                QuestionResults = new List<QuestionResult>(),
                QuestionTypeStats = new Dictionary<QuestionType, QuestionTypeStats>()
            };

            // Process each question
            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                string userAnswer = userAnswers.ContainsKey(i) ? userAnswers[i] : "";
                
                var questionResult = EvaluateQuestion(question, userAnswer);
                questionResult.QuestionIndex = i;
                questionResult.Question = question;
                questionResult.UserAnswer = userAnswer;
                
                results.QuestionResults.Add(questionResult);
                
                // Update question type stats
                if (!results.QuestionTypeStats.ContainsKey(question.QuestionType))
                {
                    results.QuestionTypeStats[question.QuestionType] = new QuestionTypeStats
                    {
                        QuestionType = question.QuestionType,
                        Total = 0,
                        Correct = 0,
                        Incorrect = 0,
                        Partial = 0,
                        Accuracy = 0f
                    };
                }

                var typeStats = results.QuestionTypeStats[question.QuestionType];
                typeStats.Total++;
                
                switch (questionResult.ResultType)
                {
                    case QuestionResultType.Correct:
                        typeStats.Correct++;
                        break;
                    case QuestionResultType.Incorrect:
                        typeStats.Incorrect++;
                        break;
                    case QuestionResultType.Partial:
                        typeStats.Partial++;
                        break;
                }
                
                typeStats.Accuracy = typeStats.Total > 0 ? (float)typeStats.Correct / typeStats.Total * 100f : 0f;
            }

            // Calculate overall stats
            results.TotalQuestions = questions.Count;
            results.CorrectAnswers = results.QuestionResults.Count(r => r.ResultType == QuestionResultType.Correct);
            results.IncorrectAnswers = results.QuestionResults.Count(r => r.ResultType == QuestionResultType.Incorrect);
            results.PartialAnswers = results.QuestionResults.Count(r => r.ResultType == QuestionResultType.Partial);
            results.OverallAccuracy = results.TotalQuestions > 0 ? (float)results.CorrectAnswers / results.TotalQuestions * 100f : 0f;
            results.Score = CalculateScore(results);
            results.Grade = CalculateGrade(results.Score);

            return results;
        }

        /// <summary>
        /// Đánh giá câu trả lời của user
        /// </summary>
        private QuestionResult EvaluateQuestion(BaseQuestion question, string userAnswer)
        {
            var result = new QuestionResult();

            if (string.IsNullOrWhiteSpace(userAnswer))
            {
                result.ResultType = QuestionResultType.NotAnswered;
                result.Score = 0f;
                result.Feedback = "Chưa trả lời";
                return result;
            }

            // Simple validation for now - could be enhanced with AI grading
            bool isCorrect = question.ValidateAnswer(userAnswer);
            
            if (isCorrect)
            {
                result.ResultType = QuestionResultType.Correct;
                result.Score = 100f;
                result.Feedback = "Chính xác! " + question.Explanation;
            }
            else
            {
                result.ResultType = QuestionResultType.Incorrect;
                result.Score = 0f;
                result.Feedback = $"Chưa đúng. Đáp án: {question.GetCorrectAnswer()}. {question.Explanation}";
            }

            return result;
        }

        /// <summary>
        /// Tính điểm tổng
        /// </summary>
        private float CalculateScore(DetailedResults results)
        {
            if (results.TotalQuestions == 0) return 0f;
            
            float totalScore = results.QuestionResults.Sum(r => r.Score);
            return totalScore / results.TotalQuestions;
        }

        /// <summary>
        /// Tính grade (A, B, C, D, F)
        /// </summary>
        private string CalculateGrade(float score)
        {
            return score switch
            {
                >= 90f => "A",
                >= 80f => "B", 
                >= 70f => "C",
                >= 60f => "D",
                _ => "F"
            };
        }

        /// <summary>
        /// Cập nhật overall results
        /// </summary>
        private void UpdateOverallResults(DetailedResults results)
        {
            if (_overallScoreText)
                _overallScoreText.text = $"{results.Score:F1}";

            if (_gradeText)
                _gradeText.text = results.Grade;

            if (_scoreProgressBar)
            {
                _scoreProgressBar.value = results.Score / 100f;
                
                if (_scoreFillImage)
                {
                    _scoreFillImage.color = GetGradeColor(results.Grade);
                }
            }

            if (_gradeIcon)
            {
                _gradeIcon.color = GetGradeColor(results.Grade);
            }
        }

        /// <summary>
        /// Cập nhật performance stats
        /// </summary>
        private void UpdatePerformanceStats(DetailedResults results, SessionResults sessionResults)
        {
            if (_totalQuestionsText)
                _totalQuestionsText.text = results.TotalQuestions.ToString();

            if (_correctAnswersText)
                _correctAnswersText.text = results.CorrectAnswers.ToString();

            if (_incorrectAnswersText)
                _incorrectAnswersText.text = results.IncorrectAnswers.ToString();

            if (_accuracyPercentageText)
                _accuracyPercentageText.text = $"{results.OverallAccuracy:F1}%";

            if (_totalTimeText && sessionResults.QuestionTimes != null)
            {
                var totalTime = TimeSpan.FromSeconds(sessionResults.QuestionTimes.Values.Sum());
                _totalTimeText.text = FormatTime(totalTime);
            }

            if (_averageTimeText && sessionResults.QuestionTimes != null && sessionResults.QuestionTimes.Count > 0)
            {
                var averageTime = TimeSpan.FromSeconds(sessionResults.QuestionTimes.Values.Average());
                _averageTimeText.text = FormatTime(averageTime);
            }
        }

        /// <summary>
        /// Cập nhật question type breakdown
        /// </summary>
        private void UpdateQuestionTypeBreakdown(DetailedResults results)
        {
            if (_questionTypesParent == null || _questionTypeItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in _questionTypesParent)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create breakdown items
            foreach (var typeStats in results.QuestionTypeStats.Values)
            {
                var itemObj = Instantiate(_questionTypeItemPrefab, _questionTypesParent);
                var typeText = itemObj.transform.Find("TypeText")?.GetComponent<TextMeshProUGUI>();
                var statsText = itemObj.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
                var accuracyText = itemObj.transform.Find("AccuracyText")?.GetComponent<TextMeshProUGUI>();

                if (typeText) typeText.text = GetQuestionTypeDisplayName(typeStats.QuestionType);
                if (statsText) statsText.text = $"{typeStats.Correct}/{typeStats.Total}";
                if (accuracyText) accuracyText.text = $"{typeStats.Accuracy:F1}%";
            }
        }

        /// <summary>
        /// Cập nhật performance chart
        /// </summary>
        private void UpdatePerformanceChart(DetailedResults results)
        {
            if (_chartParent == null || _chartBarPrefab == null) return;

            // Clear existing bars
            foreach (Transform child in _chartParent)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create bars for each question type
            float maxBarHeight = _chartArea ? _chartArea.rect.height - 20f : 100f;
            float barWidth = 60f;
            float spacing = 10f;
            float startX = 10f;

            int index = 0;
            foreach (var typeStats in results.QuestionTypeStats.Values)
            {
                var barObj = Instantiate(_chartBarPrefab, _chartParent);
                var barRect = barObj.GetComponent<RectTransform>();
                var barImage = barObj.GetComponent<Image>();

                if (barRect)
                {
                    float barHeight = (typeStats.Accuracy / 100f) * maxBarHeight;
                    barRect.anchoredPosition = new Vector2(startX + index * (barWidth + spacing), 0f);
                    barRect.sizeDelta = new Vector2(barWidth, barHeight);
                }

                if (barImage)
                {
                    barImage.color = GetAccuracyColor(typeStats.Accuracy);
                }

                index++;
            }
        }

        /// <summary>
        /// Cập nhật detailed results
        /// </summary>
        private void UpdateDetailedResults(DetailedResults results, List<BaseQuestion> questions)
        {
            if (_detailedResultsParent == null || _questionResultItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in _detailedResultsParent)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create result items
            for (int i = 0; i < results.QuestionResults.Count; i++)
            {
                var questionResult = results.QuestionResults[i];
                var itemObj = Instantiate(_questionResultItemPrefab, _detailedResultsParent);
                
                var questionNumberText = itemObj.transform.Find("QuestionNumber")?.GetComponent<TextMeshProUGUI>();
                var questionText = itemObj.transform.Find("QuestionText")?.GetComponent<TextMeshProUGUI>();
                var userAnswerText = itemObj.transform.Find("UserAnswer")?.GetComponent<TextMeshProUGUI>();
                var resultIcon = itemObj.transform.Find("ResultIcon")?.GetComponent<Image>();
                var feedbackText = itemObj.transform.Find("Feedback")?.GetComponent<TextMeshProUGUI>();

                if (questionNumberText) questionNumberText.text = $"Câu {i + 1}:";
                if (questionText) questionText.text = questions[i].Question;
                if (userAnswerText) userAnswerText.text = $"Bạn trả lời: {questionResult.UserAnswer}";
                if (feedbackText) feedbackText.text = questionResult.Feedback;
                
                if (resultIcon)
                {
                    resultIcon.color = questionResult.ResultType switch
                    {
                        QuestionResultType.Correct => _correctColor,
                        QuestionResultType.Incorrect => _incorrectColor,
                        QuestionResultType.Partial => _partialColor,
                        _ => Color.gray
                    };
                }
            }
        }

        /// <summary>
        /// Cập nhật recommendations
        /// </summary>
        private void UpdateRecommendations(DetailedResults results)
        {
            var recommendations = GenerateRecommendations(results);
            
            if (_recommendationsText)
                _recommendationsText.text = string.Join("\n\n", recommendations.Take(3));

            // Update improvement areas
            if (_improvementAreasParent && _improvementAreaPrefab)
            {
                foreach (Transform child in _improvementAreasParent)
                {
                    DestroyImmediate(child.gameObject);
                }

                var improvementAreas = GetImprovementAreas(results);
                foreach (var area in improvementAreas.Take(3))
                {
                    var areaObj = Instantiate(_improvementAreaPrefab, _improvementAreasParent);
                    var areaText = areaObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (areaText) areaText.text = area;
                }
            }
        }

        /// <summary>
        /// Cập nhật achievements
        /// </summary>
        private void UpdateAchievements(DetailedResults results, SessionResults sessionResults)
        {
            if (_achievementsParent == null || _achievementPrefab == null) return;

            foreach (Transform child in _achievementsParent)
            {
                DestroyImmediate(child.gameObject);
            }

            var achievements = GenerateAchievements(results, sessionResults);
            foreach (var achievement in achievements)
            {
                var achievementObj = Instantiate(_achievementPrefab, _achievementsParent);
                var achievementText = achievementObj.GetComponentInChildren<TextMeshProUGUI>();
                if (achievementText) achievementText.text = achievement;
            }
        }

        /// <summary>
        /// Tạo recommendations
        /// </summary>
        private List<string> GenerateRecommendations(DetailedResults results)
        {
            var recommendations = new List<string>();

            if (results.OverallAccuracy < 70f)
            {
                recommendations.Add("• Hãy dành thêm thời gian để ôn tập nội dung trước khi làm bài");
                recommendations.Add("• Đọc kỹ câu hỏi và suy nghĩ trước khi trả lời");
            }

            var weakestType = results.QuestionTypeStats.Values
                .Where(s => s.Total > 0)
                .OrderBy(s => s.Accuracy)
                .FirstOrDefault();

            if (weakestType != null)
            {
                recommendations.Add($"• Cần tập trung cải thiện kỹ năng làm bài {GetQuestionTypeDisplayName(weakestType.QuestionType).ToLower()}");
            }

            if (results.OverallAccuracy >= 90f)
            {
                recommendations.Add("• Xuất sắc! Hãy thử thách bản thân với nội dung khó hơn");
            }

            return recommendations;
        }

        /// <summary>
        /// Lấy improvement areas
        /// </summary>
        private List<string> GetImprovementAreas(DetailedResults results)
        {
            var areas = new List<string>();

            var weakTypes = results.QuestionTypeStats.Values
                .Where(s => s.Total > 0 && s.Accuracy < 80f)
                .OrderBy(s => s.Accuracy);

            foreach (var type in weakTypes)
            {
                areas.Add($"{GetQuestionTypeDisplayName(type.QuestionType)} ({type.Accuracy:F1}%)");
            }

            return areas;
        }

        /// <summary>
        /// Tạo achievements
        /// </summary>
        private List<string> GenerateAchievements(DetailedResults results, SessionResults sessionResults)
        {
            var achievements = new List<string>();

            if (results.OverallAccuracy >= 100f)
                achievements.Add("Hoàn hảo - 100% chính xác!");
            else if (results.OverallAccuracy >= 90f)
                achievements.Add("Xuất sắc - Trên 90%!");
            else if (results.OverallAccuracy >= 80f)
                achievements.Add("Tốt - Trên 80%!");

            if (results.CorrectAnswers >= 10)
                achievements.Add("Chuyên gia - 10+ câu đúng");

            var fastestTime = sessionResults.QuestionTimes?.Values.Min() ?? 0f;
            if (fastestTime > 0 && fastestTime < 10f)
                achievements.Add("Tốc độ - Câu nhanh nhất < 10 giây");

            return achievements;
        }

        /// <summary>
        /// Lấy màu cho grade
        /// </summary>
        private Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "A" => _gradeAColor,
                "B" => _gradeBColor,
                "C" => _gradeCColor,
                "D" => _gradeDColor,
                _ => _gradeFColor
            };
        }

        /// <summary>
        /// Lấy màu cho accuracy
        /// </summary>
        private Color GetAccuracyColor(float accuracy)
        {
            return accuracy switch
            {
                >= 90f => _correctColor,
                >= 70f => _partialColor,
                _ => _incorrectColor
            };
        }

        /// <summary>
        /// Get question type display name
        /// </summary>
        private string GetQuestionTypeDisplayName(QuestionType type)
        {
            return type switch
            {
                QuestionType.ContentMultipleChoice => "Trắc nghiệm nội dung",
                QuestionType.UnderstandingMultipleChoice => "Trắc nghiệm hiểu biết",
                QuestionType.MissingWordChoice => "Trắc nghiệm từ thiếu",
                QuestionType.FillInTheBlank => "Điền chỗ trống",
                QuestionType.ShortAnswer => "Tự luận ngắn",
                QuestionType.ScenarioQuestion => "Câu hỏi tình huống",
                QuestionType.ExactTyping => "Gõ chính xác",
                QuestionType.TrueFalse => "Đúng/Sai",
                QuestionType.MatchConcepts => "Ghép khái niệm",
                QuestionType.Flashcard => "Thẻ ghi nhớ",
                _ => "Khác"
            };
        }

        /// <summary>
        /// Format time
        /// </summary>
        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            else
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        /// <summary>
        /// Set visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (_resultsPanel) _resultsPanel.SetActive(visible);
        }

        /// <summary>
        /// Đóng results
        /// </summary>
        private void CloseResults()
        {
            SetVisible(false);
            OnResultsClosed?.Invoke();
        }

        /// <summary>
        /// Request retry
        /// </summary>
        private void RequestRetry()
        {
            OnRetryRequested?.Invoke();
        }

        /// <summary>
        /// Request continue
        /// </summary>
        private void RequestContinue()
        {
            OnContinueRequested?.Invoke();
        }

        private void UnsubscribeEvents()
        {
            if (_closeButton) _closeButton.onClick.RemoveAllListeners();
            if (_retryButton) _retryButton.onClick.RemoveAllListeners();
            if (_continueButton) _continueButton.onClick.RemoveAllListeners();
        }
    }

    #region Data Classes
    /// <summary>
    /// Kết quả chi tiết của session
    /// </summary>
    [Serializable]
    public class DetailedResults
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int PartialAnswers { get; set; }
        public float OverallAccuracy { get; set; }
        public float Score { get; set; }
        public string Grade { get; set; }
        public List<QuestionResult> QuestionResults { get; set; }
        public Dictionary<QuestionType, QuestionTypeStats> QuestionTypeStats { get; set; }
    }

    /// <summary>
    /// Kết quả từng câu hỏi
    /// </summary>
    [Serializable]
    public class QuestionResult
    {
        public int QuestionIndex { get; set; }
        public BaseQuestion Question { get; set; }
        public string UserAnswer { get; set; }
        public QuestionResultType ResultType { get; set; }
        public float Score { get; set; }
        public string Feedback { get; set; }
    }

    /// <summary>
    /// Stats theo loại câu hỏi
    /// </summary>
    [Serializable]
    public class QuestionTypeStats
    {
        public QuestionType QuestionType { get; set; }
        public int Total { get; set; }
        public int Correct { get; set; }
        public int Incorrect { get; set; }
        public int Partial { get; set; }
        public float Accuracy { get; set; }
    }

    /// <summary>
    /// Loại kết quả câu hỏi
    /// </summary>
    public enum QuestionResultType
    {
        NotAnswered,
        Correct,
        Incorrect,
        Partial
    }
    #endregion
}
