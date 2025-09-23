using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AISmartRecall.Data.Models;
using AISmartRecall.Managers;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// UI Controller cho việc nhập nội dung học tập
    /// </summary>
    public class ContentInputUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _headerTitleText;
        [SerializeField] private Button _backButton;

        [Header("Content Form")]
        [SerializeField] private TMP_InputField _titleInput;
        [SerializeField] private TMP_InputField _contentInput;
        [SerializeField] private ToggleGroup _contentTypeToggleGroup;
        [SerializeField] private Toggle _memorizationToggle;
        [SerializeField] private Toggle _understandingToggle;

        [Header("Stats and Validation")]
        [SerializeField] private TMP_Text _contentStatsText;
        [SerializeField] private TMP_Text _validationMessageText;

        [Header("Action Buttons")]
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _nextButton;

        // Current content data
        private ContentData _currentContent;

        // Events
        public static event Action<ContentData> OnContentReady;
        public static event Action OnBackRequested;

        private void Start()
        {
            SetupUI();
        }

        /// <summary>
        /// Setup UI components và event listeners
        /// </summary>
        private void SetupUI()
        {
            // Header setup
            if (_headerTitleText) _headerTitleText.text = "Nhập nội dung học tập";

            // Button listeners
            if (_backButton) _backButton.onClick.AddListener(() => OnBackRequested?.Invoke());
            if (_clearButton) _clearButton.onClick.AddListener(ClearForm);
            if (_nextButton) _nextButton.onClick.AddListener(OnNextButtonClicked);

            // Input listeners cho real-time validation
            if (_titleInput) _titleInput.onValueChanged.AddListener(OnTitleChanged);
            if (_contentInput) _contentInput.onValueChanged.AddListener(OnContentChanged);

            // Content type toggles
            if (_memorizationToggle) _memorizationToggle.onValueChanged.AddListener(OnContentTypeChanged);
            if (_understandingToggle) _understandingToggle.onValueChanged.AddListener(OnContentTypeChanged);

            // Set default values
            SetDefaultValues();
            ValidateAndUpdateUI();
        }

        /// <summary>
        /// Set giá trị mặc định cho form
        /// </summary>
        private void SetDefaultValues()
        {
            // Clear inputs
            if (_titleInput) _titleInput.text = "";
            if (_contentInput) _contentInput.text = "";

            // Default to Understanding mode
            if (_understandingToggle) _understandingToggle.isOn = true;

            // Setup placeholder texts
            if (_titleInput && _titleInput.placeholder is TMP_Text titlePlaceholder)
                titlePlaceholder.text = "Nhập tiêu đề bài học...";

            if (_contentInput && _contentInput.placeholder is TMP_Text contentPlaceholder)
                contentPlaceholder.text = "Dán hoặc nhập nội dung cần học...\n\nVí dụ:\n- Đoạn văn từ sách giáo khoa\n- Danh sách từ vựng\n- Công thức toán học\n- Khái niệm cần ghi nhớ";

            // Create initial content data
            _currentContent = new ContentData();
        }

        /// <summary>
        /// Xử lý khi title thay đổi
        /// </summary>
        private void OnTitleChanged(string newTitle)
        {
            if (_currentContent != null)
            {
                _currentContent.Title = newTitle;
                ValidateAndUpdateUI();
            }
        }

        /// <summary>
        /// Xử lý khi content thay đổi
        /// </summary>
        private void OnContentChanged(string newContent)
        {
            if (_currentContent != null)
            {
                _currentContent.Content = newContent;
                UpdateContentStats();
                ValidateAndUpdateUI();
            }
        }

        /// <summary>
        /// Xử lý khi content type thay đổi
        /// </summary>
        private void OnContentTypeChanged(bool _)
        {
            if (_currentContent != null)
            {
                _currentContent.ContentType = GetSelectedContentType();
                ValidateAndUpdateUI();
            }
        }

        /// <summary>
        /// Lấy content type được chọn
        /// </summary>
        private ContentType GetSelectedContentType()
        {
            if (_memorizationToggle != null && _memorizationToggle.isOn)
                return ContentType.Memorization;
            
            return ContentType.Understanding;
        }

        /// <summary>
        /// Update hiển thị thống kê content
        /// </summary>
        private void UpdateContentStats()
        {
            if (_contentStatsText == null || _currentContent == null) return;

            string statsText = $"{_currentContent.WordCount} từ";
            
            if (_currentContent.EstimatedReadingTime > 0)
            {
                statsText += $" • ~{_currentContent.EstimatedReadingTime} phút đọc";
            }

            // Add content type indicator with icon
            string typeIcon = _currentContent.ContentType.GetIcon();
            string typeName = _currentContent.ContentType.GetDisplayName();
            statsText += $" • {typeIcon} {typeName}";

            _contentStatsText.text = statsText;

            // Update color based on content length
            if (_currentContent.WordCount == 0)
                _contentStatsText.color = Color.gray;
            else if (_currentContent.WordCount < 20)
                _contentStatsText.color = Color.yellow;
            else
                _contentStatsText.color = Color.green;
        }

        /// <summary>
        /// Validate form và update UI state
        /// </summary>
        private void ValidateAndUpdateUI()
        {
            if (_currentContent == null) return;

            bool isValid = _currentContent.IsValid();
            string validationMessage = GetValidationMessage();

            // Update validation message
            if (_validationMessageText)
            {
                _validationMessageText.text = validationMessage;
                _validationMessageText.color = isValid ? Color.green : Color.red;
                _validationMessageText.gameObject.SetActive(!string.IsNullOrEmpty(validationMessage));
            }

            // Update next button state
            if (_nextButton)
            {
                _nextButton.interactable = isValid;
                
                var buttonText = _nextButton.GetComponentInChildren<TMP_Text>();
                if (buttonText)
                {
                    buttonText.text = isValid ? "Tiếp tục →" : "Cần nhập đủ thông tin";
                }
            }

            UpdateContentStats();
        }

        /// <summary>
        /// Lấy validation message
        /// </summary>
        private string GetValidationMessage()
        {
            if (_currentContent == null) return "";

            if (string.IsNullOrEmpty(_currentContent.Title))
                return "Vui lòng nhập tiêu đề";

            if (string.IsNullOrEmpty(_currentContent.Content))
                return "Vui lòng nhập nội dung";

            if (_currentContent.Content.Length < 50)
                return "Nội dung cần ít nhất 50 ký tự";

            if (_currentContent.WordCount < 10)
                return "Nội dung cần ít nhất 10 từ";

            // Success message
            return "Nội dung hợp lệ, sẵn sàng tạo câu hỏi";
        }

        /// <summary>
        /// Clear toàn bộ form
        /// </summary>
        private void ClearForm()
        {
            if (_titleInput) _titleInput.text = "";
            if (_contentInput) _contentInput.text = "";
            
            // Reset to default content type
            if (_understandingToggle) _understandingToggle.isOn = true;

            // Reset content data
            _currentContent = new ContentData();
            ValidateAndUpdateUI();

            Debug.Log("[ContentInput] Form cleared");
        }

        /// <summary>
        /// Xử lý khi nhấn Next button
        /// </summary>
        private void OnNextButtonClicked()
        {
            if (_currentContent == null || !_currentContent.IsValid())
            {
                Debug.LogWarning("[ContentInput] Cannot proceed - content invalid");
                return;
            }

            Debug.Log($"[ContentInput] Content ready: {_currentContent.Title} ({_currentContent.WordCount} words)");

            // Trigger event để LearningSceneManager chuyển sang QuestionSelection
            OnContentReady?.Invoke(_currentContent);
        }

        /// <summary>
        /// Load content data vào form (cho edit mode)
        /// </summary>
        public void LoadContent(ContentData content)
        {
            if (content == null) return;

            _currentContent = content;

            // Update UI with loaded data
            if (_titleInput) _titleInput.text = content.Title;
            if (_contentInput) _contentInput.text = content.Content;

            // Set content type toggle
            bool isMemorization = content.ContentType == ContentType.Memorization;
            if (_memorizationToggle) _memorizationToggle.isOn = isMemorization;
            if (_understandingToggle) _understandingToggle.isOn = !isMemorization;

            ValidateAndUpdateUI();

            Debug.Log($"[ContentInput] Loaded content: {content.Title}");
        }

        /// <summary>
        /// Get current content data
        /// </summary>
        public ContentData GetCurrentContent()
        {
            return _currentContent;
        }

        /// <summary>
        /// Focus vào content input field
        /// </summary>
        public void FocusContentInput()
        {
            if (_contentInput != null)
            {
                _contentInput.Select();
                _contentInput.ActivateInputField();
            }
        }

        /// <summary>
        /// Show/hide panel
        /// </summary>
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            
            if (active && _contentInput != null)
            {
                // Auto focus content input when panel becomes active
                FocusContentInput();
            }
        }

        #region Development/Testing Methods

        /// <summary>
        /// Load sample content cho testing
        /// </summary>
        [ContextMenu("Load Sample Content")]
        public void LoadSampleContent()
        {
            var sampleContent = new ContentData(
                "Bài học mẫu về lịch sử",
                "Cuộc cách mạng tháng Tám năm 1945 là một sự kiện lịch sử quan trọng của Việt Nam. " +
                "Dưới sự lãnh đạo của Đảng Cộng sản Việt Nam và Chủ tịch Hồ Chí Minh, nhân dân Việt Nam " +
                "đã đứng lên đấu tranh giành độc lập từ thực dân Pháp và phát xít Nhật. " +
                "Ngày 2 tháng 9 năm 1945, Chủ tịch Hồ Chí Minh đã đọc bản Tuyên ngôn độc lập, " +
                "khai sinh ra nước Việt Nam Dân chủ Cộng hòa.",
                ContentType.Understanding
            );

            LoadContent(sampleContent);
        }

        /// <summary>
        /// Load sample memorization content
        /// </summary>
        [ContextMenu("Load Sample Memorization Content")]
        public void LoadSampleMemorizationContent()
        {
            var sampleContent = new ContentData(
                "Từ vựng tiếng Anh cơ bản",
                "Apple - Quả táo\n" +
                "Book - Cuốn sách\n" +
                "Cat - Con mèo\n" +
                "Dog - Con chó\n" +
                "House - Ngôi nhà\n" +
                "Water - Nước\n" +
                "Fire - Lửa\n" +
                "Tree - Cây\n" +
                "Car - Xe hơi\n" +
                "Phone - Điện thoại",
                ContentType.Memorization
            );

            LoadContent(sampleContent);
        }

        #endregion
    }
}
