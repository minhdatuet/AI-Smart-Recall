using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AISmartRecall.Data.Models;
using AISmartRecall.Data.Models.Questions;
using AISmartRecall.Services.AI;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// UI Controller cho việc chọn loại câu hỏi và số lượng từng loại
    /// </summary>
    public class QuestionSelectionUI : MonoBehaviour
    {
        [Header("Content Info Display")]
        [SerializeField] private TMP_Text _contentTitleText;
        [SerializeField] private TMP_Text _contentSummaryText;
        [SerializeField] private TMP_Text _contentStatsText;
        [SerializeField] private Image _contentTypeIcon;
        [SerializeField] private TMP_Text _contentTypeText;

        [Header("Question Selection")]
        [SerializeField] private Transform _questionTypesContainer;
        [SerializeField] private GameObject _questionTypeItemPrefab;
        [SerializeField] private TMP_Text _totalQuestionsText;
        [SerializeField] private Button _generateButton;
        [SerializeField] private Button _backButton;

        [Header("Loading")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private Slider _loadingProgressBar;

        // Data
        private ContentData _currentContent;
        private List<QuestionTypeSelectionItem> _questionTypeItems = new List<QuestionTypeSelectionItem>();
        private Dictionary<QuestionType, int> _selectedQuestionCounts = new Dictionary<QuestionType, int>();

        // Events
        public static event Action<ContentData, List<QuestionGenerationRequest>> OnGenerateQuestionsRequested;
        public static event Action OnBackRequested;

        private void Start()
        {
            SetupUI();
        }

        /// <summary>
        /// Setup UI ban đầu
        /// </summary>
        private void SetupUI()
        {
            // Setup buttons
            if (_generateButton) _generateButton.onClick.AddListener(OnGenerateButtonClicked);
            if (_backButton) _backButton.onClick.AddListener(() => OnBackRequested?.Invoke());

            // Hide loading panel
            if (_loadingPanel) _loadingPanel.SetActive(false);

            // Initial state
            UpdateGenerateButton();
        }

        /// <summary>
        /// Setup UI với content data
        /// </summary>
        /// <param name="content">Content data để tạo câu hỏi</param>
        public void Setup(ContentData content)
        {
            _currentContent = content;
            
            // Update content display
            UpdateContentDisplay();
            
            // Create question type selection items
            CreateQuestionTypeItems();
            
            // Reset selection
            _selectedQuestionCounts.Clear();
            UpdateTotalQuestionsDisplay();
            UpdateGenerateButton();
        }

        /// <summary>
        /// Update hiển thị thông tin content
        /// </summary>
        private void UpdateContentDisplay()
        {
            if (_currentContent == null) return;

            // Basic info
            if (_contentTitleText) _contentTitleText.text = _currentContent.Title;
            if (_contentSummaryText) _contentSummaryText.text = _currentContent.GetSummary();
            
            // Content type
            if (_contentTypeIcon) _contentTypeIcon.color = _currentContent.ContentType.GetColor();
            if (_contentTypeText) _contentTypeText.text = _currentContent.ContentType.GetDisplayName();

            // Stats
            if (_contentStatsText)
            {
                _contentStatsText.text = $"{_currentContent.WordCount} từ • " +
                                      $"~{_currentContent.EstimatedReadingTime} phút đọc • " +
                                      $"{_currentContent.FormattedCreatedAt}";
            }
        }

        /// <summary>
        /// Tạo các item selection cho từng loại câu hỏi
        /// </summary>
        private void CreateQuestionTypeItems()
        {
            // Clear existing items
            foreach (var item in _questionTypeItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _questionTypeItems.Clear();

            // Get available question types for this content type
            var availableQuestionTypes = QuestionTypeExtensions.GetQuestionTypesForContentType(_currentContent.ContentType);

            // Create item for each question type
            foreach (var questionType in availableQuestionTypes)
            {
                CreateQuestionTypeItem(questionType);
            }
        }

        /// <summary>
        /// Tạo một item selection cho loại câu hỏi cụ thể
        /// </summary>
        private void CreateQuestionTypeItem(QuestionType questionType)
        {
            if (_questionTypeItemPrefab == null || _questionTypesContainer == null) return;

            var itemObject = Instantiate(_questionTypeItemPrefab, _questionTypesContainer);
            var item = itemObject.GetComponent<QuestionTypeSelectionItem>();

            if (item != null)
            {
                item.Setup(questionType, OnQuestionTypeCountChanged);
                _questionTypeItems.Add(item);
            }
        }

        /// <summary>
        /// Callback khi số lượng câu hỏi của một loại thay đổi
        /// </summary>
        private void OnQuestionTypeCountChanged(QuestionType questionType, int count)
        {
            if (count > 0)
            {
                _selectedQuestionCounts[questionType] = count;
            }
            else
            {
                _selectedQuestionCounts.Remove(questionType);
            }

            UpdateTotalQuestionsDisplay();
            UpdateGenerateButton();
        }

        /// <summary>
        /// Update hiển thị tổng số câu hỏi
        /// </summary>
        private void UpdateTotalQuestionsDisplay()
        {
            int totalQuestions = _selectedQuestionCounts.Values.Sum();
            
            if (_totalQuestionsText)
            {
                _totalQuestionsText.text = $"Tổng cộng: {totalQuestions} câu hỏi";
                
                // Change color based on count
                _totalQuestionsText.color = totalQuestions > 0 ? Color.green : Color.gray;
            }
        }

        /// <summary>
        /// Update trạng thái button Generate
        /// </summary>
        private void UpdateGenerateButton()
        {
            bool hasSelection = _selectedQuestionCounts.Count > 0;
            
            if (_generateButton) 
            {
                _generateButton.interactable = hasSelection;
                
                // Update button text
                var buttonText = _generateButton.GetComponentInChildren<TMP_Text>();
                if (buttonText)
                {
                    buttonText.text = hasSelection ? 
                        $"Tạo {_selectedQuestionCounts.Values.Sum()} câu hỏi" : 
                        "Chọn ít nhất 1 loại câu hỏi";
                }
            }
        }

        /// <summary>
        /// Xử lý khi nhấn button Generate
        /// </summary>
        private void OnGenerateButtonClicked()
        {
            if (_selectedQuestionCounts.Count == 0) return;

            // Convert selection to generation requests
            var requests = new List<QuestionGenerationRequest>();
            foreach (var kvp in _selectedQuestionCounts)
            {
                requests.Add(new QuestionGenerationRequest(kvp.Key, kvp.Value));
            }

            // Show loading
            ShowLoading();

            // Trigger event
            OnGenerateQuestionsRequested?.Invoke(_currentContent, requests);
        }

        /// <summary>
        /// Hiển thị loading state
        /// </summary>
        private void ShowLoading()
        {
            if (_loadingPanel) _loadingPanel.SetActive(true);
            if (_loadingText) _loadingText.text = "Đang tạo câu hỏi bằng AI...";
            if (_loadingProgressBar) _loadingProgressBar.value = 0f;
        }

        /// <summary>
        /// Update loading progress
        /// </summary>
        /// <param name="progress">Progress từ 0 đến 1</param>
        /// <param name="message">Message hiển thị</param>
        public void UpdateLoadingProgress(float progress, string message = "")
        {
            if (_loadingProgressBar) _loadingProgressBar.value = progress;
            if (_loadingText && !string.IsNullOrEmpty(message)) _loadingText.text = message;
        }

        /// <summary>
        /// Ẩn loading state
        /// </summary>
        public void HideLoading()
        {
            if (_loadingPanel) _loadingPanel.SetActive(false);
        }

        /// <summary>
        /// Hiển thị error message
        /// </summary>
        public void ShowError(string errorMessage)
        {
            HideLoading();
            Debug.LogError($"[QuestionSelection] {errorMessage}");
            
            // TODO: Show proper error dialog
            if (_loadingText)
            {
                _loadingText.text = $"Lỗi: {errorMessage}";
                _loadingText.color = Color.red;
            }
        }
    }
}
