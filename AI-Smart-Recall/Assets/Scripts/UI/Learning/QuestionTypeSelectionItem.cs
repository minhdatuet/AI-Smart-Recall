using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AISmartRecall.Data.Models.Questions;
using AISmartRecall.Services.AI;

namespace AISmartRecall.UI.Learning
{
    /// <summary>
    /// UI Component cho từng item lựa chọn loại câu hỏi trong Question Selection Panel
    /// </summary>
    /// <summary>
    /// Component cho từng item selection loại câu hỏi
    /// </summary>
    public class QuestionTypeSelectionItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text _typeNameText;
        [SerializeField] private TMP_Text _typeDescriptionText;
        [SerializeField] private TMP_Text _typeIconText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Slider _countSlider;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Button _decreaseButton;
        [SerializeField] private Button _increaseButton;

        private QuestionType _questionType;
        private Action<QuestionType, int> _onCountChanged;
        private int _currentCount = 0;

        /// <summary>
        /// Setup item với question type
        /// </summary>
        public void Setup(QuestionType questionType, Action<QuestionType, int> onCountChanged)
        {
            _questionType = questionType;
            _onCountChanged = onCountChanged;

            UpdateDisplay();
            SetupInteractions();
        }

        /// <summary>
        /// Update hiển thị thông tin
        /// </summary>
        private void UpdateDisplay()
        {
            // Basic info
            if (_typeNameText) _typeNameText.text = _questionType.GetVietnameseName();
            if (_typeDescriptionText) _typeDescriptionText.text = _questionType.GetDescription();
            if (_typeIconText) _typeIconText.text = _questionType.GetIcon();

            // Background color
            if (_backgroundImage) 
            {
                var color = _questionType.GetColor();
                color.a = 0.1f; // Light background
                _backgroundImage.color = color;
            }

            UpdateCountDisplay();
        }

        /// <summary>
        /// Setup interactions
        /// </summary>
        private void SetupInteractions()
        {
            // Slider
            if (_countSlider)
            {
                _countSlider.minValue = 0;
                _countSlider.maxValue = 10;
                _countSlider.wholeNumbers = true;
                _countSlider.value = _currentCount;
                _countSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            // Buttons
            if (_decreaseButton) _decreaseButton.onClick.AddListener(DecreaseCount);
            if (_increaseButton) _increaseButton.onClick.AddListener(IncreaseCount);
        }

        /// <summary>
        /// Xử lý khi slider thay đổi
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            SetCount((int)value);
        }

        /// <summary>
        /// Giảm số lượng
        /// </summary>
        private void DecreaseCount()
        {
            SetCount(Mathf.Max(0, _currentCount - 1));
        }

        /// <summary>
        /// Tăng số lượng
        /// </summary>
        private void IncreaseCount()
        {
            SetCount(Mathf.Min(10, _currentCount + 1));
        }

        /// <summary>
        /// Set số lượng cụ thể
        /// </summary>
        private void SetCount(int count)
        {
            _currentCount = Mathf.Clamp(count, 0, 10);
            
            // Update UI
            if (_countSlider) _countSlider.value = _currentCount;
            UpdateCountDisplay();

            // Trigger callback
            _onCountChanged?.Invoke(_questionType, _currentCount);
        }

        /// <summary>
        /// Update hiển thị số lượng
        /// </summary>
        private void UpdateCountDisplay()
        {
            if (_countText)
            {
                _countText.text = _currentCount.ToString();
                _countText.color = _currentCount > 0 ? Color.black : Color.gray;
            }

            // Update button states
            if (_decreaseButton) _decreaseButton.interactable = _currentCount > 0;
            if (_increaseButton) _increaseButton.interactable = _currentCount < 10;
        }
    }
}
