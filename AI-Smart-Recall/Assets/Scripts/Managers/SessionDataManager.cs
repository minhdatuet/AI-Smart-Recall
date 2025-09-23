using System;
using UnityEngine;
using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecall.Managers
{
    /// <summary>
    /// Manager để quản lý dữ liệu session giữa các scene
    /// Sử dụng PlayerPrefs và static variables để lưu trữ tạm thời
    /// </summary>
    public static class SessionDataManager
    {
        private const string LEARNING_API_KEY = "Learning_APIKey";
        private const string LEARNING_USERNAME = "Learning_Username";
        private const string LEARNING_DISPLAY_NAME = "Learning_DisplayName";
        private const string LEARNING_USER_LEVEL = "Learning_UserLevel";
        private const string LEARNING_USER_EXPERIENCE = "Learning_UserExperience";
        private const string LEARNING_SESSION_START = "Learning_SessionStartTime";
        private const string LEARNING_SOURCE_SCENE = "Learning_SourceScene";
        private const string LEARNING_AI_PROVIDER = "Learning_SelectedAIProvider";
        private const string LEARNING_LEARNING_MODE = "Learning_SelectedLearningMode";

        // Static cache để tránh đọc PlayerPrefs nhiều lần
        private static SessionData _cachedSessionData;
        private static bool _isDataCached = false;

        /// <summary>
        /// Lưu dữ liệu session để truyền sang Learning scene
        /// </summary>
        public static void SaveSessionData(SessionData sessionData)
        {
            try
            {
                // Lưu vào PlayerPrefs
                PlayerPrefs.SetString(LEARNING_API_KEY, sessionData.APIKey ?? "");
                PlayerPrefs.SetString(LEARNING_USERNAME, sessionData.Username ?? "");
                PlayerPrefs.SetString(LEARNING_DISPLAY_NAME, sessionData.DisplayName ?? "");
                PlayerPrefs.SetInt(LEARNING_USER_LEVEL, sessionData.UserLevel);
                PlayerPrefs.SetInt(LEARNING_USER_EXPERIENCE, sessionData.UserExperience);
                PlayerPrefs.SetString(LEARNING_SESSION_START, sessionData.SessionStartTime.ToBinary().ToString());
                PlayerPrefs.SetString(LEARNING_SOURCE_SCENE, sessionData.SourceScene ?? "");
                PlayerPrefs.SetString(LEARNING_AI_PROVIDER, sessionData.SelectedAIProvider ?? "");
                PlayerPrefs.SetString(LEARNING_LEARNING_MODE, sessionData.SelectedLearningMode ?? "");
                
                PlayerPrefs.Save();

                // Update cache
                _cachedSessionData = sessionData;
                _isDataCached = true;

                Debug.Log("[SessionDataManager] Session data saved successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDataManager] Error saving session data: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy dữ liệu session đã lưu
        /// </summary>
        public static SessionData GetSessionData()
        {
            // Return cached data nếu có
            if (_isDataCached && _cachedSessionData != null)
            {
                return _cachedSessionData;
            }

            try
            {
                var sessionData = new SessionData
                {
                    APIKey = PlayerPrefs.GetString(LEARNING_API_KEY, ""),
                    Username = PlayerPrefs.GetString(LEARNING_USERNAME, ""),
                    DisplayName = PlayerPrefs.GetString(LEARNING_DISPLAY_NAME, ""),
                    UserLevel = PlayerPrefs.GetInt(LEARNING_USER_LEVEL, 1),
                    UserExperience = PlayerPrefs.GetInt(LEARNING_USER_EXPERIENCE, 0),
                    SourceScene = PlayerPrefs.GetString(LEARNING_SOURCE_SCENE, ""),
                    SelectedAIProvider = PlayerPrefs.GetString(LEARNING_AI_PROVIDER, "gemini"),
                    SelectedLearningMode = PlayerPrefs.GetString(LEARNING_LEARNING_MODE, "understanding")
                };

                // Parse session start time
                string sessionStartString = PlayerPrefs.GetString(LEARNING_SESSION_START, "");
                if (!string.IsNullOrEmpty(sessionStartString) && long.TryParse(sessionStartString, out long binary))
                {
                    sessionData.SessionStartTime = DateTime.FromBinary(binary);
                }
                else
                {
                    sessionData.SessionStartTime = DateTime.Now;
                }

                // Update cache
                _cachedSessionData = sessionData;
                _isDataCached = true;

                Debug.Log("[SessionDataManager] Session data loaded successfully");
                return sessionData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDataManager] Error loading session data: {ex.Message}");
                return GetDefaultSessionData();
            }
        }

        /// <summary>
        /// Kiểm tra xem có session data hợp lệ không
        /// </summary>
        public static bool HasValidSessionData()
        {
            var sessionData = GetSessionData();
            return !string.IsNullOrEmpty(sessionData.APIKey) && !string.IsNullOrEmpty(sessionData.Username);
        }

        /// <summary>
        /// Xóa session data
        /// </summary>
        public static void ClearSessionData()
        {
            try
            {
                PlayerPrefs.DeleteKey(LEARNING_API_KEY);
                PlayerPrefs.DeleteKey(LEARNING_USERNAME);
                PlayerPrefs.DeleteKey(LEARNING_DISPLAY_NAME);
                PlayerPrefs.DeleteKey(LEARNING_USER_LEVEL);
                PlayerPrefs.DeleteKey(LEARNING_USER_EXPERIENCE);
                PlayerPrefs.DeleteKey(LEARNING_SESSION_START);
                PlayerPrefs.DeleteKey(LEARNING_SOURCE_SCENE);
                PlayerPrefs.DeleteKey(LEARNING_AI_PROVIDER);
                PlayerPrefs.DeleteKey(LEARNING_LEARNING_MODE);
                
                PlayerPrefs.Save();

                // Clear cache
                _cachedSessionData = null;
                _isDataCached = false;

                Debug.Log("[SessionDataManager] Session data cleared");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionDataManager] Error clearing session data: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy default session data
        /// </summary>
        private static SessionData GetDefaultSessionData()
        {
            return new SessionData
            {
                APIKey = "",
                Username = "Guest",
                DisplayName = "Guest User",
                UserLevel = 1,
                UserExperience = 0,
                SessionStartTime = DateTime.Now,
                SourceScene = "Unknown",
                SelectedAIProvider = "gemini",
                SelectedLearningMode = "understanding"
            };
        }

        /// <summary>
        /// Lấy thông tin debug về session data
        /// </summary>
        public static string GetSessionDataInfo()
        {
            var sessionData = GetSessionData();
            return $"User: {sessionData.DisplayName} | " +
                   $"Level: {sessionData.UserLevel} | " +
                   $"Mode: {sessionData.SelectedLearningMode} | " +
                   $"Provider: {sessionData.SelectedAIProvider} | " +
                   $"Source: {sessionData.SourceScene} | " +
                   $"API Key: {(string.IsNullOrEmpty(sessionData.APIKey) ? "None" : "Set")}";
        }
    }

    /// <summary>
    /// Data class chứa thông tin session
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public string APIKey { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public int UserLevel { get; set; }
        public int UserExperience { get; set; }
        public DateTime SessionStartTime { get; set; }
        public string SourceScene { get; set; }
        public string SelectedAIProvider { get; set; }
        public string SelectedLearningMode { get; set; }

        /// <summary>
        /// Tạo SessionData từ UserProfileDTO
        /// </summary>
        public static SessionData FromUserProfile(UserProfileDTO userProfile, string apiKey, string sourceScene, string aiProvider, string learningMode)
        {
            return new SessionData
            {
                APIKey = apiKey,
                Username = userProfile.Username,
                DisplayName = userProfile.DisplayName,
                UserLevel = userProfile.Level,
                UserExperience = userProfile.Experience,
                SessionStartTime = DateTime.Now,
                SourceScene = sourceScene,
                SelectedAIProvider = aiProvider,
                SelectedLearningMode = learningMode
            };
        }
    }
}
