using System;
using System.Collections.Generic;
using UnityEngine;

namespace AISmartRecall.Data.Models.Questions
{
    /// <summary>
    /// Abstract base class cho tất cả loại câu hỏi
    /// </summary>
    [Serializable]
    public abstract class BaseQuestion
    {
        [Header("Basic Info")]
        [SerializeField] protected string _id;
        [SerializeField] protected string _question;
        [SerializeField] protected string _explanation;
        [SerializeField] protected QuestionType _questionType;
        [SerializeField] protected ContentType _contentType;

        [Header("Metadata")]
        [SerializeField] protected DateTime _createdAt;
        [SerializeField] protected string _aiProvider;
        [SerializeField] protected int _timeLimit; // seconds, 0 = no limit

        // Properties
        public string Id => _id;
        public string Question => _question;
        public string Explanation => _explanation;
        public QuestionType QuestionType => _questionType;
        public ContentType ContentType => _contentType;
        public DateTime CreatedAt => _createdAt;
        public string AIProvider => _aiProvider;
        public int TimeLimit => _timeLimit;

        /// <summary>
        /// Constructor cơ bản
        /// </summary>
        protected BaseQuestion(QuestionType questionType, ContentType contentType)
        {
            _id = Guid.NewGuid().ToString();
            _questionType = questionType;
            _contentType = contentType;
            _createdAt = DateTime.Now;
            _timeLimit = 30; // Default 30 seconds
        }

        /// <summary>
        /// Kiểm tra xem câu hỏi này có cần AI chấm điểm không
        /// </summary>
        /// <returns>True nếu cần AI chấm điểm, False nếu chấm điểm tự động</returns>
        public bool RequiresAIGrading()
        {
            return _questionType switch
            {
                QuestionType.FillInTheBlank => true,
                QuestionType.ExactTyping => true,
                QuestionType.ShortAnswer => true,
                QuestionType.ScenarioQuestion => true,
                _ => false // Các loại khác chấm điểm tự động
            };
        }

        /// <summary>
        /// Validate câu trả lời của user cho các câu tự động chấm
        /// </summary>
        /// <param name="userAnswer">Câu trả lời của user</param>
        /// <returns>True nếu đúng, false nếu sai</returns>
        public abstract bool ValidateAnswer(string userAnswer);

        /// <summary>
        /// Lấy đáp án đúng - abstract method
        /// </summary>
        /// <returns>Đáp án đúng dưới dạng string</returns>
        public abstract string GetCorrectAnswer();

        /// <summary>
        /// Lấy tất cả options có thể (cho multiple choice) - virtual method
        /// </summary>
        /// <returns>List các options, null nếu không áp dụng</returns>
        public virtual List<string> GetOptions()
        {
            return null;
        }

        /// <summary>
        /// Lấy placeholder text cho input field - virtual method
        /// </summary>
        /// <returns>Placeholder text</returns>
        public virtual string GetInputPlaceholder()
        {
            return "Nhập câu trả lời...";
        }

        /// <summary>
        /// Mỗi câu hỏi đều có điểm tối đa là 10
        /// </summary>
        /// <returns>10 điểm</returns>
        public int GetMaxScore()
        {
            return 10;
        }

        /// <summary>
        /// Tạo prompt để gửi AI đánh giá câu trả lời tự luận
        /// </summary>
        /// <param name="userAnswer">Câu trả lời của user</param>
        /// <param name="originalContent">Nội dung gốc để AI tham khảo</param>
        /// <returns>Prompt cho AI</returns>
        public virtual string CreateGradingPrompt(string userAnswer, string originalContent)
        {
            return $@"
Bạn là một giáo viên chuyên nghiệp đánh giá câu trả lời của học sinh.

THÔNG TIN BÀI HỌC:
Nội dung gốc: {originalContent}

THÔNG TIN CÂU HỎI:
Loại câu hỏi: {_questionType.GetVietnameseName()}
Câu hỏi: {_question}
Đáp án mẫu: {GetCorrectAnswer()}

CÂU TRẢ LỜI CỦA HỌC SINH:
{userAnswer}

YÊU CẦU ĐÁNH GIÁ:
1. So sánh câu trả lời với đáp án mẫu và nội dung gốc
2. Xem xét độ chính xác về mặt nội dung và ý nghĩa
3. Đối với câu điền chỗ trống và gõ chính xác: yêu cầu độ chính xác cao
4. Đối với tự luận: chấp nhận các cách diễn đạt khác nhau nhưng đúng ý nghĩa

ĐỊNH DẠNG TRẢ LỜI (JSON):
{{
  ""percentage"": [số từ 0-100, là % độ chính xác],
  ""explanation"": ""[Giải thích ngắn gọn tại sao được điểm này]"",
  ""suggestions"": ""[Gợi ý cải thiện nếu có]""
}}

Chỉ trả lời bằng JSON, không thêm text khác.";
        }

        /// <summary>
        /// Convert to dictionary cho serialization
        /// </summary>
        public virtual Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["id"] = _id,
                ["question"] = _question,
                ["explanation"] = _explanation,
                ["questionType"] = _questionType.ToString(),
                ["contentType"] = _contentType.ToString(),
                ["createdAt"] = _createdAt,
                ["aiProvider"] = _aiProvider,
                ["timeLimit"] = _timeLimit,
                ["correctAnswer"] = GetCorrectAnswer(),
                ["options"] = GetOptions(),
                ["requiresAIGrading"] = RequiresAIGrading()
            };
        }
    }

    /// <summary>
    /// Kết quả chấm điểm từ AI
    /// </summary>
    [Serializable]
    public class AIGradingResult
    {
        [SerializeField] private float _percentage; // 0-100
        [SerializeField] private string _explanation;
        [SerializeField] private string _suggestions;
        [SerializeField] private bool _isValid;

        public float Percentage 
        { 
            get => _percentage; 
            set => _percentage = Mathf.Clamp(value, 0f, 100f); 
        }
        
        public string Explanation 
        { 
            get => _explanation; 
            set => _explanation = value ?? ""; 
        }
        
        public string Suggestions 
        { 
            get => _suggestions; 
            set => _suggestions = value ?? ""; 
        }
        
        public bool IsValid 
        { 
            get => _isValid; 
            set => _isValid = value; 
        }

        /// <summary>
        /// Tính điểm cuối cùng (0-10 điểm)
        /// </summary>
        public float GetFinalScore()
        {
            return (_percentage / 100f) * 10f;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AIGradingResult(float percentage, string explanation, string suggestions)
        {
            Percentage = percentage;
            Explanation = explanation;
            Suggestions = suggestions;
            IsValid = true;
        }

        /// <summary>
        /// Constructor for failed grading
        /// </summary>
        public AIGradingResult()
        {
            Percentage = 0f;
            Explanation = "Không thể chấm điểm tự động";
            Suggestions = "Vui lòng thử lại";
            IsValid = false;
        }
    }

    /// <summary>
    /// Enum định nghĩa các loại câu hỏi
    /// </summary>
    public enum QuestionType
    {
        // MEMORIZATION MODE QUESTIONS
        [QuestionTypeInfo("Fill in the Blank", "Điền chỗ trống", ContentType.Memorization, true)]
        FillInTheBlank,

        [QuestionTypeInfo("Missing Word Multiple Choice", "Trắc nghiệm từ thiếu", ContentType.Memorization, false)]
        MissingWordChoice,

        [QuestionTypeInfo("Flashcard", "Thẻ ghi nhớ", ContentType.Memorization, false)]
        Flashcard,

        [QuestionTypeInfo("Exact Typing", "Gõ lại chính xác", ContentType.Memorization, true)]
        ExactTyping,

        [QuestionTypeInfo("Content Multiple Choice", "Trắc nghiệm nội dung", ContentType.Memorization, false)]
        ContentMultipleChoice,

        // UNDERSTANDING MODE QUESTIONS
        [QuestionTypeInfo("Understanding Multiple Choice", "Trắc nghiệm hiểu biết", ContentType.Understanding, false)]
        UnderstandingMultipleChoice,

        [QuestionTypeInfo("True/False", "Đúng/Sai", ContentType.Understanding, false)]
        TrueFalse,

        [QuestionTypeInfo("Match Concepts", "Ghép khái niệm", ContentType.Understanding, false)]
        MatchConcepts,

        [QuestionTypeInfo("Short Answer", "Tự luận ngắn", ContentType.Understanding, true)]
        ShortAnswer,

        [QuestionTypeInfo("Scenario Questions", "Câu hỏi tình huống", ContentType.Understanding, true)]
        ScenarioQuestion,
    }

    /// <summary>
    /// Attribute để định nghĩa thông tin cho QuestionType
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QuestionTypeInfoAttribute : Attribute
    {
        public string EnglishName { get; }
        public string VietnameseName { get; }
        public ContentType ContentType { get; }
        public bool RequiresAIGrading { get; }

        public QuestionTypeInfoAttribute(string englishName, string vietnameseName, ContentType contentType, bool requiresAIGrading)
        {
            EnglishName = englishName;
            VietnameseName = vietnameseName;
            ContentType = contentType;
            RequiresAIGrading = requiresAIGrading;
        }
    }

    /// <summary>
    /// Extension methods cho QuestionType
    /// </summary>
    public static class QuestionTypeExtensions
    {
        private static readonly Dictionary<QuestionType, QuestionTypeInfoAttribute> _questionTypeInfo;

        static QuestionTypeExtensions()
        {
            _questionTypeInfo = new Dictionary<QuestionType, QuestionTypeInfoAttribute>();
            
            foreach (QuestionType questionType in Enum.GetValues(typeof(QuestionType)))
            {
                var field = typeof(QuestionType).GetField(questionType.ToString());
                var attribute = (QuestionTypeInfoAttribute)Attribute.GetCustomAttribute(field, typeof(QuestionTypeInfoAttribute));
                if (attribute != null)
                {
                    _questionTypeInfo[questionType] = attribute;
                }
            }
        }

        public static string GetVietnameseName(this QuestionType questionType)
        {
            return _questionTypeInfo.TryGetValue(questionType, out var info) ? info.VietnameseName : questionType.ToString();
        }

        public static string GetEnglishName(this QuestionType questionType)
        {
            return _questionTypeInfo.TryGetValue(questionType, out var info) ? info.EnglishName : questionType.ToString();
        }

        public static ContentType GetContentType(this QuestionType questionType)
        {
            return _questionTypeInfo.TryGetValue(questionType, out var info) ? info.ContentType : ContentType.Understanding;
        }

        public static bool RequiresAIGrading(this QuestionType questionType)
        {
            return _questionTypeInfo.TryGetValue(questionType, out var info) && info.RequiresAIGrading;
        }

        public static List<QuestionType> GetQuestionTypesForContentType(ContentType contentType)
        {
            var result = new List<QuestionType>();
            
            foreach (var kvp in _questionTypeInfo)
            {
                if (kvp.Value.ContentType == contentType)
                {
                    result.Add(kvp.Key);
                }
            }
            
            return result;
        }

        public static Color GetColor(this QuestionType questionType)
        {
            var color = questionType.GetContentType() switch
            {
                ContentType.Memorization => new Color(1f, 0.7f, 0.3f), // Orange
                ContentType.Understanding => new Color(0.3f, 0.7f, 1f), // Blue
                _ => Color.gray
            };

            // Làm đậm màu cho câu hỏi AI grading
            if (questionType.RequiresAIGrading())
            {
                color = Color.Lerp(color, Color.white, -0.2f); // Đậm hơn một chút
            }

            return color;
        }

        public static string GetDescription(this QuestionType questionType)
        {
            var baseDescription = questionType switch
            {
                QuestionType.FillInTheBlank => "Câu hỏi có chỗ trống cần điền vào",
                QuestionType.MissingWordChoice => "Trắc nghiệm với từ thiếu trong câu",
                QuestionType.Flashcard => "Thẻ ghi nhớ một mặt hỏi, một mặt trả lời",
                QuestionType.ExactTyping => "Gõ lại chính xác từ/cụm từ",
                QuestionType.ContentMultipleChoice => "Trắc nghiệm dựa trên nội dung",
                QuestionType.UnderstandingMultipleChoice => "Trắc nghiệm kiểm tra hiểu biết",
                QuestionType.TrueFalse => "Câu hỏi đúng hoặc sai",
                QuestionType.MatchConcepts => "Ghép các khái niệm với nhau",
                QuestionType.ShortAnswer => "Trả lời ngắn tự do",
                QuestionType.ScenarioQuestion => "Câu hỏi tình huống thực tế",
                _ => ""
            };

            // Thêm thông tin về AI grading
            if (questionType.RequiresAIGrading())
            {
                baseDescription += " (AI chấm điểm)";
            }

            return baseDescription;
        }

        public static string GetIcon(this QuestionType questionType)
        {
            return questionType switch
            {
                QuestionType.FillInTheBlank => "[FB]",
                QuestionType.MissingWordChoice => "[MW]",
                QuestionType.Flashcard => "[FC]",
                QuestionType.ExactTyping => "[ET]",
                QuestionType.ContentMultipleChoice => "[CM]",
                QuestionType.UnderstandingMultipleChoice => "[UM]",
                QuestionType.TrueFalse => "[TF]",
                QuestionType.MatchConcepts => "[MC]",
                QuestionType.ShortAnswer => "[SA]",
                QuestionType.ScenarioQuestion => "[SQ]",
                _ => "[?]"
            };
        }
    }
}
