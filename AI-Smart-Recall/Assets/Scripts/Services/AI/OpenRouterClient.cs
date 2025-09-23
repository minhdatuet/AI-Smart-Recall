using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using AISmartRecall.Data.Models;
using AISmartRecall.Data.Models.Questions;
using Newtonsoft.Json;

namespace AISmartRecall.Services.AI
{
    /// <summary>
    /// Client để gọi OpenRouter API trực tiếp từ Unity
    /// </summary>
    public class OpenRouterClient : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string _apiUrl = "https://openrouter.ai/api/v1/chat/completions";
        [SerializeField] private string _defaultModel = "qwen/qwen-2.5-72b-instruct:free";
        [SerializeField] private int _timeout = 30;
        [SerializeField] private int _maxTokens = 2000;

        private string _apiKey;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static OpenRouterClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Set API key cho OpenRouter
        /// </summary>
        public void SetAPIKey(string apiKey)
        {
            _apiKey = apiKey;
            Debug.Log("[OpenRouter] API key updated");
        }

        /// <summary>
        /// Generate questions cho content với loại và số lượng cụ thể
        /// </summary>
        /// <param name="content">Nội dung gốc</param>
        /// <param name="questionRequests">Danh sách yêu cầu generate từng loại câu hỏi</param>
        /// <returns>Dictionary mapping QuestionType -> List questions</returns>
        public async UniTask<Dictionary<QuestionType, List<BaseQuestion>>> GenerateQuestionsAsync(
            ContentData content, 
            List<QuestionGenerationRequest> questionRequests)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("API Key chưa được set");
            }

            var results = new Dictionary<QuestionType, List<BaseQuestion>>();

            // Generate từng loại câu hỏi riêng biệt
            foreach (var request in questionRequests)
            {
                try
                {
                    Debug.Log($"[OpenRouter] Generating {request.Count} questions of type: {request.QuestionType.GetVietnameseName()}");

                    var questions = await GenerateQuestionsByType(content, request.QuestionType, request.Count);
                    results[request.QuestionType] = questions;

                    // Delay nhẹ giữa các requests để tránh rate limit
                    await UniTask.Delay(1000);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OpenRouter] Error generating {request.QuestionType}: {ex.Message}");
                    results[request.QuestionType] = new List<BaseQuestion>();
                }
            }

            return results;
        }

        /// <summary>
        /// Generate questions cho một loại cụ thể
        /// </summary>
        public async UniTask<List<BaseQuestion>> GenerateQuestionsByType(
            ContentData content, 
            QuestionType questionType, 
            int count)
        {
            string prompt = CreatePromptForQuestionType(content, questionType, count);
            string aiResponse = await CallOpenRouterAPI(prompt);
            
            return ParseAIResponseToQuestions(aiResponse, questionType, content.ContentType);
        }

        /// <summary>
        /// Đánh giá câu trả lời tự luận bằng AI
        /// </summary>
        /// <param name="question">Câu hỏi gốc</param>
        /// <param name="userAnswer">Câu trả lời của user</param>
        /// <param name="originalContent">Nội dung gốc để tham khảo</param>
        /// <returns>Kết quả đánh giá từ AI</returns>
        public async UniTask<AIGradingResult> GradeAnswerAsync(
            BaseQuestion question, 
            string userAnswer, 
            string originalContent)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new AIGradingResult(); // Failed result
            }

            try
            {
                string prompt = question.CreateGradingPrompt(userAnswer, originalContent);
                string aiResponse = await CallOpenRouterAPI(prompt);
                
                return ParseGradingResponse(aiResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenRouter] Error grading answer: {ex.Message}");
                return new AIGradingResult(); // Failed result
            }
        }

        /// <summary>
        /// Gửi request đơn giản đến AI (dùng cho AnswerValidationSystem)
        /// </summary>
        /// <param name="prompt">Prompt để gửi</param>
        /// <returns>Response text từ AI</returns>
        public async UniTask<string> SendRequestAsync(string prompt)
        {
            return await CallOpenRouterAPI(prompt);
        }

        /// <summary>
        /// Gọi OpenRouter API
        /// </summary>
        private async UniTask<string> CallOpenRouterAPI(string prompt)
        {
            var requestData = new OpenRouterRequest
            {
                model = _defaultModel,
                messages = new[]
                {
                    new OpenRouterMessage
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = _maxTokens,
                temperature = 0.7f
            };

            string jsonPayload = JsonConvert.SerializeObject(requestData);

            using (var request = new UnityWebRequest(_apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("HTTP-Referer", "https://ai-smart-recall.com");
                request.SetRequestHeader("X-Title", "AI Smart Recall");

                request.timeout = _timeout;

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<OpenRouterResponse>(responseText);

                    if (response?.choices != null && response.choices.Length > 0)
                    {
                        return response.choices[0].message.content;
                    }
                    else
                    {
                        throw new Exception("Invalid response from OpenRouter API");
                    }
                }
                else
                {
                    string error = $"HTTP {request.responseCode}: {request.error}";
                    Debug.LogError($"[OpenRouter] API call failed: {error}");
                    throw new Exception($"API call failed: {error}");
                }
            }
        }

        /// <summary>
        /// Tạo prompt cho từng loại câu hỏi
        /// </summary>
        private string CreatePromptForQuestionType(ContentData content, QuestionType questionType, int count)
        {
            string questionTypeInfo = GetQuestionTypeInstructions(questionType);
            
            return $@"
Bạn là một giáo viên chuyên nghiệp tạo câu hỏi cho học sinh.

THÔNG TIN BÀI HỌC:
- Chế độ học: {content.ContentType.GetDisplayName()}
- Tiêu đề: {content.Title}
- Nội dung: {content.Content}

YÊU CẦU TẠO CÂU HỎI:
- Loại câu hỏi: {questionType.GetVietnameseName()}
- Số lượng: {count} câu
- Mô tả: {questionType.GetDescription()}

{questionTypeInfo}

ĐỊNH DẠNG TRẢ LỜI (JSON):
{{
  ""questions"": [
    {{
      ""question"": ""[Nội dung câu hỏi]"",
      ""correctAnswer"": ""[Đáp án đúng]"",
      ""explanation"": ""[Giải thích tại sao đáp án này đúng]"",
      ""options"": [""Option1"", ""Option2"", ""Option3"", ""Option4""] // Chỉ dành cho multiple choice
    }}
  ]
}}

Chỉ trả lời bằng JSON hợp lệ, không thêm text khác.
";
        }

        /// <summary>
        /// Lấy instructions cụ thể cho từng loại câu hỏi
        /// </summary>
        private string GetQuestionTypeInstructions(QuestionType questionType)
        {
            return questionType switch
            {
                QuestionType.FillInTheBlank => 
                    "Tạo câu có chỗ trống, thay thế từ/cụm từ quan trọng bằng ___. correctAnswer chứa từ cần điền.",

                QuestionType.MissingWordChoice => 
                    "Tạo câu thiếu từ với 4 lựa chọn. 1 đáp án đúng, 3 đáp án sai hợp lý. options chứa 4 lựa chọn.",

                QuestionType.Flashcard => 
                    "Tạo thẻ ghi nhớ: question là mặt trước (khái niệm/thuật ngữ), correctAnswer là mặt sau (định nghĩa/giải thích).",

                QuestionType.ExactTyping => 
                    "Tạo câu yêu cầu gõ chính xác từ/cụm từ quan trọng. question mô tả yêu cầu, correctAnswer là text cần gõ chính xác.",

                QuestionType.ContentMultipleChoice => 
                    "Tạo câu trắc nghiệm 4 lựa chọn dựa trên nội dung. 1 đáp án đúng, 3 đáp án sai hợp lý. options chứa 4 lựa chọn.",

                QuestionType.UnderstandingMultipleChoice => 
                    "Tạo câu trắc nghiệm kiểm tra hiểu biết sâu. 4 lựa chọn, tập trung vào ý nghĩa, nguyên nhân, hệ quả. options chứa 4 lựa chọn.",

                QuestionType.TrueFalse => 
                    "Tạo câu đúng/sai về nội dung. correctAnswer là 'true' hoặc 'false'. options chứa ['Đúng', 'Sai'].",

                QuestionType.MatchConcepts => 
                    "Tạo câu ghép khái niệm. question mô tả yêu cầu ghép, options chứa các cặp concept-definition cần ghép, correctAnswer mô tả cách ghép đúng.",

                QuestionType.ShortAnswer => 
                    "Tạo câu hỏi tự luận ngắn (2-3 câu). Yêu cầu giải thích, phân tích hoặc tóm tắt. correctAnswer là câu trả lời mẫu.",

                QuestionType.ScenarioQuestion => 
                    "Tạo câu hỏi tình huống thực tế áp dụng kiến thức. question mô tả tình huống, yêu cầu phân tích/giải quyết. correctAnswer là cách xử lý đúng.",

                _ => "Tạo câu hỏi phù hợp với loại đã chọn."
            };
        }

        /// <summary>
        /// Parse response từ AI thành danh sách questions
        /// </summary>
        private List<BaseQuestion> ParseAIResponseToQuestions(string aiResponse, QuestionType questionType, ContentType contentType)
        {
            try
            {
                // Clean response trước khi parse JSON
                string cleanedResponse = CleanAIResponse(aiResponse);
                Debug.Log($"[OpenRouter] Original response length: {aiResponse.Length}");
                Debug.Log($"[OpenRouter] Cleaned response length: {cleanedResponse.Length}");
                Debug.Log($"[OpenRouter] Cleaned response: {cleanedResponse}");
                
                var jsonResponse = JsonConvert.DeserializeObject<AIQuestionResponse>(cleanedResponse);
                var questions = new List<BaseQuestion>();

                foreach (var questionData in jsonResponse.questions)
                {
                    var question = QuestionFactory.CreateQuestion(questionType, contentType, questionData);
                    if (question != null)
                    {
                        questions.Add(question);
                    }
                }

                return questions;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenRouter] Failed to parse questions: {ex.Message}\nResponse: {aiResponse}");
                return new List<BaseQuestion>();
            }
        }

        /// <summary>
        /// Parse response grading từ AI
        /// </summary>
        private AIGradingResult ParseGradingResponse(string aiResponse)
        {
            try
            {
                // Clean response trước khi parse JSON
                string cleanedResponse = CleanAIResponse(aiResponse);
                
                var gradingData = JsonConvert.DeserializeObject<AIGradingResponse>(cleanedResponse);
                
                return new AIGradingResult(
                    gradingData.percentage,
                    gradingData.explanation,
                    gradingData.suggestions
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenRouter] Failed to parse grading: {ex.Message}\nResponse: {aiResponse}");
                return new AIGradingResult();
            }
        }
        
        /// <summary>
        /// Clean AI response bằng cách loại bỏ markdown code blocks và các text không cần thiết
        /// </summary>
        private string CleanAIResponse(string aiResponse)
        {
            if (string.IsNullOrEmpty(aiResponse))
                return aiResponse;
            
            string cleaned = aiResponse.Trim();
            
            // Loại bỏ markdown code blocks
            if (cleaned.StartsWith("```json"))
            {
                // Tìm và loại bỏ ```json ở đầu
                int startIndex = cleaned.IndexOf("```json") + 7;
                
                // Tìm và loại bỏ ``` ở cuối
                int endIndex = cleaned.LastIndexOf("```");
                
                if (startIndex < endIndex)
                {
                    cleaned = cleaned.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            else if (cleaned.StartsWith("```"))
            {
                // Loại bỏ các code block khác
                int firstNewline = cleaned.IndexOf('\n');
                if (firstNewline > 0)
                {
                    int startIndex = firstNewline + 1;
                    int endIndex = cleaned.LastIndexOf("```");
                    
                    if (startIndex < endIndex)
                    {
                        cleaned = cleaned.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }
            }
            
            // Loại bỏ các dòng comment hoặc giải thích trước/sau JSON
            var lines = cleaned.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var jsonLines = new List<string>();
            bool insideJson = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Bắt đầu JSON khi gặp dấu {
                if (!insideJson && trimmedLine.StartsWith("{"))
                {
                    insideJson = true;
                }
                
                if (insideJson)
                {
                    jsonLines.Add(line);
                    
                    // Kết thúc JSON khi gặp dấu } ở cuối
                    if (trimmedLine.EndsWith("}") && GetJsonBraceBalance(string.Join("\n", jsonLines)) == 0)
                    {
                        break;
                    }
                }
            }
            
            if (jsonLines.Count > 0)
            {
                cleaned = string.Join("\n", jsonLines);
            }
            
            return cleaned.Trim();
        }
        
        /// <summary>
        /// Kiểm tra balance của dấu ngoặc nhọn JSON
        /// </summary>
        private int GetJsonBraceBalance(string json)
        {
            int balance = 0;
            bool inString = false;
            bool escaped = false;
            
            foreach (char c in json)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                
                if (!inString)
                {
                    if (c == '{') balance++;
                    else if (c == '}') balance--;
                }
            }
            
            return balance;
        }
    }

    /// <summary>
    /// Request model cho OpenRouter API
    /// </summary>
    [Serializable]
    public class OpenRouterRequest
    {
        public string model;
        public OpenRouterMessage[] messages;
        public int max_tokens;
        public float temperature;
    }

    [Serializable]
    public class OpenRouterMessage
    {
        public string role;
        public string content;
    }

    /// <summary>
    /// Response model từ OpenRouter API
    /// </summary>
    [Serializable]
    public class OpenRouterResponse
    {
        public OpenRouterChoice[] choices;
    }

    [Serializable]
    public class OpenRouterChoice
    {
        public OpenRouterMessage message;
    }

    /// <summary>
    /// Response model cho questions từ AI
    /// </summary>
    [Serializable]
    public class AIQuestionResponse
    {
        public QuestionData[] questions;
    }

    [Serializable]
    public class QuestionData
    {
        public string question;
        public string correctAnswer;
        public string explanation;
        public string[] options;
    }

    /// <summary>
    /// Response model cho grading từ AI
    /// </summary>
    [Serializable]
    public class AIGradingResponse
    {
        public float percentage;
        public string explanation;
        public string suggestions;
    }

    /// <summary>
    /// Request model cho việc generate questions
    /// </summary>
    [Serializable]
    public class QuestionGenerationRequest
    {
        public QuestionType QuestionType { get; set; }
        public int Count { get; set; }

        public QuestionGenerationRequest(QuestionType questionType, int count)
        {
            QuestionType = questionType;
            Count = Mathf.Clamp(count, 1, 10); // Max 10 questions per type
        }
    }

    /// <summary>
    /// Factory để tạo các loại questions từ AI response
    /// </summary>
    public static class QuestionFactory
    {
        public static BaseQuestion CreateQuestion(QuestionType questionType, ContentType contentType, QuestionData data)
        {
            if (data == null || string.IsNullOrEmpty(data.question) || string.IsNullOrEmpty(data.correctAnswer))
            {
                Debug.LogWarning($"[QuestionFactory] Invalid question data for type: {questionType}");
                return null;
            }
            
            try
            {
                // Tạo SimpleQuestion cho tất cả các loại (temporary implementation)
                return new SimpleQuestion(questionType, contentType, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestionFactory] Error creating question: {ex.Message}");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Simple implementation của BaseQuestion cho testing
    /// </summary>
    [Serializable]
    public class SimpleQuestion : BaseQuestion
    {
        [SerializeField] private string _correctAnswer;
        [SerializeField] private List<string> _options;
        
        public SimpleQuestion(QuestionType questionType, ContentType contentType, QuestionData data) 
            : base(questionType, contentType)
        {
            _question = data.question;
            _explanation = data.explanation;
            _correctAnswer = data.correctAnswer;
            _options = data.options?.Length > 0 ? new List<string>(data.options) : new List<string>();
            _aiProvider = "OpenRouter";
        }
        
        public override bool ValidateAnswer(string userAnswer)
        {
            if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(_correctAnswer))
                return false;
            
            // Basic validation - case insensitive comparison
            string cleanUserAnswer = userAnswer.Trim().ToLowerInvariant();
            string cleanCorrectAnswer = _correctAnswer.Trim().ToLowerInvariant();
            
            // For multiple choice, check if answer matches any option
            if (_options != null && _options.Count > 0)
            {
                // Find the correct option index
                for (int i = 0; i < _options.Count; i++)
                {
                    if (_options[i].Trim().ToLowerInvariant() == cleanCorrectAnswer)
                    {
                        // Check if user answer matches the option or its index
                        return cleanUserAnswer == cleanCorrectAnswer || 
                               cleanUserAnswer == i.ToString() ||
                               cleanUserAnswer == (i + 1).ToString(); // 1-based index
                    }
                }
            }
            
            // Direct comparison for other question types
            return cleanUserAnswer == cleanCorrectAnswer;
        }
        
        public override string GetCorrectAnswer()
        {
            return _correctAnswer;
        }
        
        public override List<string> GetOptions()
        {
            return _options;
        }
        
        public override string GetInputPlaceholder()
        {
            return QuestionType switch
            {
                QuestionType.FillInTheBlank => "Nhập từ cần điền...",
                QuestionType.ExactTyping => "Gõ chính xác...",
                QuestionType.ShortAnswer => "Nhập câu trả lời ngắn...",
                QuestionType.ScenarioQuestion => "Nhập câu trả lời...",
                _ => "Chọn đáp án hoặc nhập câu trả lời..."
            };
        }
    }
}
