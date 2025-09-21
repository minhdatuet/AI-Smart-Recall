using UnityEngine;

namespace AISmartRecall.API
{
    /// <summary>
    /// Cấu hình API cho AI Smart Recall
    /// </summary>
    public static class APIConfig
    {
        // Base URL của API server
        public static readonly string BASE_URL = "http://localhost:5011";
        
        // API Endpoints
        public static class Endpoints
        {
            // Authentication endpoints
            public static readonly string REGISTER = "/api/auth/register";
            public static readonly string LOGIN = "/api/auth/login";
            public static readonly string LOGOUT = "/api/auth/logout";
            public static readonly string PROFILE = "/api/auth/profile";
            public static readonly string VALIDATE = "/api/auth/validate";
            public static readonly string REFRESH_TOKEN = "/api/auth/refresh";
            public static readonly string API_KEYS = "/api/auth/api-keys";
            public static readonly string AI_PROVIDERS = "/api/auth/ai-providers";
            
            // Content endpoints (sẽ implement sau)
            public static readonly string CONTENTS = "/api/content";
            public static readonly string CONTENT_BY_ID = "/api/content/{0}";
            
            // Question endpoints (sẽ implement sau) 
            public static readonly string GENERATE_QUESTIONS = "/api/questions/generate";
            public static readonly string QUESTIONS_BY_CONTENT = "/api/questions/by-content/{0}";
            
            // Learning endpoints (sẽ implement sau)
            public static readonly string LEARNING_SESSIONS = "/api/learning/sessions";
            public static readonly string LEARNING_PROGRESS = "/api/learning/progress";
        }
        
        // HTTP Headers
        public static class Headers
        {
            public static readonly string AUTHORIZATION = "Authorization";
            public static readonly string CONTENT_TYPE = "Content-Type";
            public static readonly string ACCEPT = "Accept";
        }
        
        // Content Types
        public static class ContentTypes
        {
            public static readonly string JSON = "application/json";
        }
        
        // Timeout settings
        public static readonly int REQUEST_TIMEOUT = 30; // seconds
        
        // Debug mode
        public static bool DEBUG_MODE = true;
        
        /// <summary>
        /// Tạo full URL từ endpoint
        /// </summary>
        public static string GetFullURL(string endpoint)
        {
            return BASE_URL + endpoint;
        }
        
        /// <summary>
        /// Log debug message nếu debug mode bật
        /// </summary>
        public static void LogDebug(string message)
        {
            if (DEBUG_MODE)
            {
                Debug.Log($"[API] {message}");
            }
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"[API ERROR] {message}");
        }
    }
}