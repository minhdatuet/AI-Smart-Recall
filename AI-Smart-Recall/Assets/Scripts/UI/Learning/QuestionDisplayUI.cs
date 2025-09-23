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
    /// Component hiển thị câu hỏi theo từng loại (multiple choice, fill blank, text input, etc.)
    /// </summary>
    public class QuestionDisplayUI : MonoBehaviour
    {
        [Header("Question Content")]
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private TextMeshProUGUI _questionTypeLabel;
        [SerializeField] private TextMeshProUGUI _instructionText;
        
        [Header("Answer Input Areas")]
        [SerializeField] private GameObject _multipleChoiceArea;
        [SerializeField] private GameObject _fillBlankArea;
        [SerializeField] private GameObject _textInputArea;
        [SerializeField] private GameObject _trueFalseArea;
        [SerializeField] private GameObject _matchingArea;

        [Header("Multiple Choice")]
        [SerializeField] private ToggleGroup _choicesToggleGroup;
        [SerializeField] private Transform _choicesParent;
        [SerializeField] private GameObject _choiceTogglePrefab;

        [Header("Fill in Blank")]
        [SerializeField] private TextMeshProUGUI _fillBlankText;
        [SerializeField] private Transform _blanksParent;
        [SerializeField] private GameObject _blankInputPrefab;

        [Header("Text Input")]
        [SerializeField] private TMP_InputField _textInputField;
        [SerializeField] private TextMeshProUGUI _characterCountText;

        [Header("True/False")]
        [SerializeField] private Toggle _trueToggle;
        [SerializeField] private Toggle _falseToggle;

        [Header("Matching")]
        [SerializeField] private Transform _leftMatchingParent;
        [SerializeField] private Transform _rightMatchingParent;
        [SerializeField] private GameObject _matchingItemPrefab;

        [Header("Visual")]
        [SerializeField] private Image _questionBackground;
        [SerializeField] private Color _defaultBackgroundColor = Color.white;
        [SerializeField] private Color _answeredBackgroundColor = new Color(0.9f, 1f, 0.9f, 1f);

        // Events
        public event Action<string> OnAnswerChanged;

        // Properties
        public BaseQuestion CurrentQuestion { get; private set; }
        
        // Private fields
        private List<Toggle> _currentChoiceToggles = new List<Toggle>();
        private List<TMP_InputField> _currentBlankInputs = new List<TMP_InputField>();
        private Dictionary<string, string> _currentMatchingPairs = new Dictionary<string, string>();
        private string _lastAnswer = "";

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
            // Setup text input listener
            if (_textInputField)
            {
                _textInputField.onValueChanged.AddListener(OnTextInputChanged);
            }

            // Setup True/False toggles
            if (_trueToggle)
            {
                _trueToggle.onValueChanged.AddListener((value) => 
                {
                    if (value) OnAnswerChanged?.Invoke("True");
                });
            }
            
            if (_falseToggle)
            {
                _falseToggle.onValueChanged.AddListener((value) => 
                {
                    if (value) OnAnswerChanged?.Invoke("False");
                });
            }

            // Initially hide all answer areas
            HideAllAnswerAreas();
        }

        /// <summary>
        /// Hiển thị câu hỏi
        /// </summary>
        public void DisplayQuestion(BaseQuestion question)
        {
            if (question == null) return;

            CurrentQuestion = question;
            
            // Reset state
            ClearCurrentAnswerComponents();
            HideAllAnswerAreas();
            
            // Display question content
            DisplayQuestionContent(question);
            
            // Setup answer area based on question type
            SetupAnswerAreaForQuestionType(question);
            
            // Update visual state
            UpdateQuestionVisuals(false);
            
            Debug.Log($"[QuestionDisplayUI] Displayed question: {question.QuestionType}");
        }

        /// <summary>
        /// Hiển thị nội dung câu hỏi
        /// </summary>
        private void DisplayQuestionContent(BaseQuestion question)
        {
            // Question text
            if (_questionText)
                _questionText.text = question.Question;

            // Question type label
            if (_questionTypeLabel)
                _questionTypeLabel.text = GetQuestionTypeDisplayName(question.QuestionType);

            // Instruction text
            if (_instructionText)
                _instructionText.text = GetInstructionText(question.QuestionType);
        }

        /// <summary>
        /// Setup answer area theo loại câu hỏi
        /// </summary>
        private void SetupAnswerAreaForQuestionType(BaseQuestion question)
        {
            switch (question.QuestionType)
            {
                case QuestionType.ContentMultipleChoice:
                case QuestionType.UnderstandingMultipleChoice:
                case QuestionType.MissingWordChoice:
                    SetupMultipleChoiceQuestion(question);
                    break;
                
                case QuestionType.FillInTheBlank:
                    SetupFillInBlankQuestion(question);
                    break;
                case QuestionType.ShortAnswer:
                case QuestionType.ScenarioQuestion:
                case QuestionType.ExactTyping:
                    SetupTextInputQuestion(question);
                    break;
                    
                case QuestionType.TrueFalse:
                    SetupTrueFalseQuestion(question);
                    break;
                    
                case QuestionType.MatchConcepts:
                    SetupMatchingQuestion(question);
                    break;
                    
                case QuestionType.Flashcard:
                    SetupFlashcardQuestion(question);
                    break;
                    
                default:
                    Debug.LogWarning($"[QuestionDisplayUI] Unsupported question type: {question.QuestionType}");
                    break;
            }
        }

        /// <summary>
        /// Setup Multiple Choice question
        /// </summary>
        private void SetupMultipleChoiceQuestion(BaseQuestion question)
        {
            var options = question.GetOptions();
            if (options == null || options.Count == 0 || _multipleChoiceArea == null) return;

            _multipleChoiceArea.SetActive(true);
            
            // Clear existing choices
            foreach (var toggle in _currentChoiceToggles)
            {
                if (toggle) DestroyImmediate(toggle.gameObject);
            }
            _currentChoiceToggles.Clear();

            // Create choice toggles
            for (int i = 0; i < options.Count; i++)
            {
                var choiceObj = Instantiate(_choiceTogglePrefab, _choicesParent);
                var toggle = choiceObj.GetComponent<Toggle>();
                var text = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (toggle && text)
                {
                    toggle.group = _choicesToggleGroup;
                    text.text = $"{(char)('A' + i)}. {options[i]}";
                    
                    int index = i; // Capture for closure
                    toggle.onValueChanged.AddListener((value) =>
                    {
                        if (value) OnAnswerChanged?.Invoke(options[index]);
                    });
                    
                    _currentChoiceToggles.Add(toggle);
                }
            }
        }

        /// <summary>
        /// Setup Fill in Blank question
        /// </summary>
        private void SetupFillInBlankQuestion(BaseQuestion question)
        {
            if (_fillBlankArea == null) return;

            _fillBlankArea.SetActive(true);
            
            // Clear existing blanks
            foreach (var input in _currentBlankInputs)
            {
                if (input) DestroyImmediate(input.gameObject);
            }
            _currentBlankInputs.Clear();

            // Display text with blanks - extract from question text
            if (_fillBlankText)
            {
                _fillBlankText.text = ProcessTextWithBlanks(question.Question);
            }

            // Estimate number of blanks from question text (count ___ occurrences)
            string questionText = question.Question ?? "";
            int blankCount = CountBlanks(questionText);
            
            if (blankCount == 0) blankCount = 1; // At least one blank

            // Create input fields for each blank
            for (int i = 0; i < blankCount; i++)
            {
                var blankObj = Instantiate(_blankInputPrefab, _blanksParent);
                var inputField = blankObj.GetComponent<TMP_InputField>();
                
                if (inputField)
                {
                    inputField.placeholder.GetComponent<TextMeshProUGUI>().text = $"Câu trả lời {i + 1}";
                    inputField.onValueChanged.AddListener((_) => OnFillBlankChanged());
                    _currentBlankInputs.Add(inputField);
                }
            }
        }

        /// <summary>
        /// Setup Text Input question
        /// </summary>
        private void SetupTextInputQuestion(BaseQuestion question)
        {
            if (_textInputArea == null) return;

            _textInputArea.SetActive(true);
            
            if (_textInputField)
            {
                _textInputField.text = "";
                _textInputField.characterLimit = 1000; // Default limit
                _textInputField.placeholder.GetComponent<TextMeshProUGUI>().text = question.GetInputPlaceholder();
                UpdateCharacterCount();
            }
        }

        /// <summary>
        /// Setup True/False question
        /// </summary>
        private void SetupTrueFalseQuestion(BaseQuestion question)
        {
            if (_trueFalseArea == null) return;

            _trueFalseArea.SetActive(true);
            
            if (_trueToggle) _trueToggle.isOn = false;
            if (_falseToggle) _falseToggle.isOn = false;
        }

        /// <summary>
        /// Setup Matching question
        /// </summary>
        private void SetupMatchingQuestion(BaseQuestion question)
        {
            if (_matchingArea == null) return;

            _matchingArea.SetActive(true);
            _currentMatchingPairs.Clear();

            // For now, create simple matching from options if available
            var options = question.GetOptions();
            if (options != null && options.Count >= 2)
            {
                var leftItems = new List<string>();
                var rightItems = new List<string>();
                
                // Split options into left and right (simple implementation)
                for (int i = 0; i < options.Count; i += 2)
                {
                    leftItems.Add(options[i]);
                    if (i + 1 < options.Count)
                        rightItems.Add(options[i + 1]);
                }
                
                CreateMatchingItems(_leftMatchingParent, leftItems, true);
                CreateMatchingItems(_rightMatchingParent, rightItems, false);
            }
        }

        /// <summary>
        /// Setup Flashcard question
        /// </summary>
        private void SetupFlashcardQuestion(BaseQuestion question)
        {
            // For flashcards, use text input area
            SetupTextInputQuestion(question);
        }

        /// <summary>
        /// Tạo matching items
        /// </summary>
        private void CreateMatchingItems(Transform parent, List<string> items, bool isLeftSide)
        {
            foreach (string item in items)
            {
                var itemObj = Instantiate(_matchingItemPrefab, parent);
                var text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                var button = itemObj.GetComponent<Button>();
                
                if (text) text.text = item;
                if (button)
                {
                    button.onClick.AddListener(() => OnMatchingItemClicked(item, isLeftSide));
                }
            }
        }

        /// <summary>
        /// Xử lý click matching item
        /// </summary>
        private void OnMatchingItemClicked(string item, bool isLeftSide)
        {
            // TODO: Implement matching logic
            Debug.Log($"[QuestionDisplayUI] Matching item clicked: {item} (Left: {isLeftSide})");
        }

        /// <summary>
        /// Xử lý thay đổi fill in blank
        /// </summary>
        private void OnFillBlankChanged()
        {
            var answers = new List<string>();
            foreach (var input in _currentBlankInputs)
            {
                answers.Add(input.text);
            }
            OnAnswerChanged?.Invoke(string.Join("|", answers));
        }

        /// <summary>
        /// Xử lý thay đổi text input
        /// </summary>
        private void OnTextInputChanged(string value)
        {
            UpdateCharacterCount();
            OnAnswerChanged?.Invoke(value);
        }

        /// <summary>
        /// Cập nhật số ký tự
        /// </summary>
        private void UpdateCharacterCount()
        {
            if (_characterCountText && _textInputField)
            {
                int current = _textInputField.text.Length;
                int max = _textInputField.characterLimit;
                _characterCountText.text = $"{current}/{max}";
                _characterCountText.color = current > max * 0.9f ? Color.red : Color.gray;
            }
        }

        /// <summary>
        /// Process text with blanks
        /// </summary>
        private string ProcessTextWithBlanks(string textWithBlanks)
        {
            return textWithBlanks?.Replace("___", "_____") ?? "";
        }

        /// <summary>
        /// Count number of blanks in text
        /// </summary>
        private int CountBlanks(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            int count = 0;
            int index = text.IndexOf("___");
            while (index != -1)
            {
                count++;
                index = text.IndexOf("___", index + 3);
            }
            return count;
        }

        /// <summary>
        /// Get display name cho question type
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
                _ => "Câu hỏi"
            };
        }

        /// <summary>
        /// Get instruction text
        /// </summary>
        private string GetInstructionText(QuestionType type)
        {
            return type switch
            {
                QuestionType.ContentMultipleChoice => "Chọn một đáp án đúng:",
                QuestionType.UnderstandingMultipleChoice => "Chọn một đáp án đúng:",
                QuestionType.MissingWordChoice => "Chọn từ phù hợp:",
                QuestionType.FillInTheBlank => "Điền vào chỗ trống:",
                QuestionType.ShortAnswer => "Nhập câu trả lời ngắn:",
                QuestionType.ScenarioQuestion => "Nhập câu trả lời:",
                QuestionType.ExactTyping => "Gõ chính xác:",
                QuestionType.TrueFalse => "Chọn Đúng hoặc Sai:",
                QuestionType.MatchConcepts => "Nối các khái niệm:",
                QuestionType.Flashcard => "Nhập câu trả lời:",
                _ => ""
            };
        }

        /// <summary>
        /// Ẩn tất cả answer areas
        /// </summary>
        private void HideAllAnswerAreas()
        {
            if (_multipleChoiceArea) _multipleChoiceArea.SetActive(false);
            if (_fillBlankArea) _fillBlankArea.SetActive(false);
            if (_textInputArea) _textInputArea.SetActive(false);
            if (_trueFalseArea) _trueFalseArea.SetActive(false);
            if (_matchingArea) _matchingArea.SetActive(false);
        }

        /// <summary>
        /// Clear current answer components
        /// </summary>
        private void ClearCurrentAnswerComponents()
        {
            // Clear multiple choice toggles
            foreach (var toggle in _currentChoiceToggles)
            {
                if (toggle) DestroyImmediate(toggle.gameObject);
            }
            _currentChoiceToggles.Clear();

            // Clear blank inputs
            foreach (var input in _currentBlankInputs)
            {
                if (input) DestroyImmediate(input.gameObject);
            }
            _currentBlankInputs.Clear();

            // Clear matching pairs
            _currentMatchingPairs.Clear();
        }

        /// <summary>
        /// Update visual state
        /// </summary>
        private void UpdateQuestionVisuals(bool isAnswered)
        {
            if (_questionBackground)
            {
                _questionBackground.color = isAnswered ? _answeredBackgroundColor : _defaultBackgroundColor;
            }
        }

        /// <summary>
        /// Kiểm tra có answer không
        /// </summary>
        public bool HasAnswer()
        {
            if (CurrentQuestion == null) return false;

            return CurrentQuestion.QuestionType switch
            {
                QuestionType.ContentMultipleChoice => _currentChoiceToggles.Any(t => t.isOn),
                QuestionType.UnderstandingMultipleChoice => _currentChoiceToggles.Any(t => t.isOn),
                QuestionType.MissingWordChoice => _currentChoiceToggles.Any(t => t.isOn),
                QuestionType.FillInTheBlank => _currentBlankInputs.Any(i => !string.IsNullOrWhiteSpace(i.text)),
                QuestionType.ShortAnswer => !string.IsNullOrWhiteSpace(_textInputField?.text),
                QuestionType.ScenarioQuestion => !string.IsNullOrWhiteSpace(_textInputField?.text),
                QuestionType.ExactTyping => !string.IsNullOrWhiteSpace(_textInputField?.text),
                QuestionType.TrueFalse => (_trueToggle?.isOn ?? false) || (_falseToggle?.isOn ?? false),
                QuestionType.MatchConcepts => _currentMatchingPairs.Count > 0,
                QuestionType.Flashcard => !string.IsNullOrWhiteSpace(_textInputField?.text),
                _ => false
            };
        }

        /// <summary>
        /// Lấy answer hiện tại
        /// </summary>
        public string GetAnswer()
        {
            if (CurrentQuestion == null) return "";

            return CurrentQuestion.QuestionType switch
            {
                QuestionType.ContentMultipleChoice => GetMultipleChoiceAnswer(),
                QuestionType.UnderstandingMultipleChoice => GetMultipleChoiceAnswer(),
                QuestionType.MissingWordChoice => GetMultipleChoiceAnswer(),
                QuestionType.FillInTheBlank => GetFillInBlankAnswer(),
                QuestionType.ShortAnswer => _textInputField?.text ?? "",
                QuestionType.ScenarioQuestion => _textInputField?.text ?? "",
                QuestionType.ExactTyping => _textInputField?.text ?? "",
                QuestionType.TrueFalse => GetTrueFalseAnswer(),
                QuestionType.MatchConcepts => GetMatchingAnswer(),
                QuestionType.Flashcard => _textInputField?.text ?? "",
                _ => ""
            };
        }

        /// <summary>
        /// Set answer
        /// </summary>
        public void SetAnswer(string answer)
        {
            if (CurrentQuestion == null || string.IsNullOrEmpty(answer)) return;

            switch (CurrentQuestion.QuestionType)
            {
                case QuestionType.ContentMultipleChoice:
                case QuestionType.UnderstandingMultipleChoice:
                case QuestionType.MissingWordChoice:
                    SetMultipleChoiceAnswer(answer);
                    break;
                case QuestionType.FillInTheBlank:
                    SetFillInBlankAnswer(answer);
                    break;
                case QuestionType.ShortAnswer:
                case QuestionType.ScenarioQuestion:
                case QuestionType.ExactTyping:
                case QuestionType.Flashcard:
                    if (_textInputField) _textInputField.text = answer;
                    break;
                case QuestionType.TrueFalse:
                    SetTrueFalseAnswer(answer);
                    break;
                case QuestionType.MatchConcepts:
                    SetMatchingAnswer(answer);
                    break;
            }

            UpdateQuestionVisuals(true);
        }

        /// <summary>
        /// Clear answer
        /// </summary>
        public void ClearAnswer()
        {
            switch (CurrentQuestion?.QuestionType)
            {
                case QuestionType.ContentMultipleChoice:
                case QuestionType.UnderstandingMultipleChoice:
                case QuestionType.MissingWordChoice:
                    foreach (var toggle in _currentChoiceToggles)
                        if (toggle) toggle.isOn = false;
                    break;
                case QuestionType.FillInTheBlank:
                    foreach (var input in _currentBlankInputs)
                        if (input) input.text = "";
                    break;
                case QuestionType.ShortAnswer:
                case QuestionType.ScenarioQuestion:
                case QuestionType.ExactTyping:
                case QuestionType.Flashcard:
                    if (_textInputField) _textInputField.text = "";
                    break;
                case QuestionType.TrueFalse:
                    if (_trueToggle) _trueToggle.isOn = false;
                    if (_falseToggle) _falseToggle.isOn = false;
                    break;
                case QuestionType.MatchConcepts:
                    _currentMatchingPairs.Clear();
                    break;
            }

            UpdateQuestionVisuals(false);
        }

        #region Answer Getters
        private string GetMultipleChoiceAnswer()
        {
            var selectedToggle = _currentChoiceToggles.FirstOrDefault(t => t.isOn);
            if (selectedToggle != null)
            {
                var text = selectedToggle.GetComponentInChildren<TextMeshProUGUI>();
                return text?.text?.Substring(3) ?? ""; // Remove "A. " prefix
            }
            return "";
        }

        private string GetFillInBlankAnswer()
        {
            var answers = new List<string>();
            foreach (var input in _currentBlankInputs)
            {
                answers.Add(input.text);
            }
            return string.Join("|", answers);
        }

        private string GetTrueFalseAnswer()
        {
            if (_trueToggle?.isOn == true) return "True";
            if (_falseToggle?.isOn == true) return "False";
            return "";
        }

        private string GetMatchingAnswer()
        {
            // Convert matching pairs to string format
            var pairs = new List<string>();
            foreach (var kvp in _currentMatchingPairs)
            {
                pairs.Add($"{kvp.Key}:{kvp.Value}");
            }
            return string.Join("|", pairs);
        }
        #endregion

        #region Answer Setters
        private void SetMultipleChoiceAnswer(string answer)
        {
            foreach (var toggle in _currentChoiceToggles)
            {
                var text = toggle.GetComponentInChildren<TextMeshProUGUI>();
                if (text?.text?.Contains(answer) == true)
                {
                    toggle.isOn = true;
                    break;
                }
            }
        }

        private void SetFillInBlankAnswer(string answer)
        {
            var answers = answer.Split('|');
            for (int i = 0; i < Math.Min(answers.Length, _currentBlankInputs.Count); i++)
            {
                _currentBlankInputs[i].text = answers[i];
            }
        }

        private void SetTrueFalseAnswer(string answer)
        {
            if (answer == "True" && _trueToggle) _trueToggle.isOn = true;
            else if (answer == "False" && _falseToggle) _falseToggle.isOn = true;
        }

        private void SetMatchingAnswer(string answer)
        {
            _currentMatchingPairs.Clear();
            var pairs = answer.Split('|');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    _currentMatchingPairs[parts[0]] = parts[1];
                }
            }
        }
        #endregion

        private void UnsubscribeEvents()
        {
            if (_textInputField)
                _textInputField.onValueChanged.RemoveAllListeners();
        }
    }
}
