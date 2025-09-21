using System;
using UnityEngine;
using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecall.API.Utils
{
    /// <summary>
    /// Manual JSON parser cho SharedModels DTOs vì Unity JsonUtility không support properties
    /// </summary>
    public static class ManualJsonParser
    {
        /// <summary>
        /// Parse login response từ JSON string
        /// </summary>
        public static LoginResponseDTO ParseLoginResponse(string json)
        {
            try
            {
                // Parse JSON manually vì LoginResponseDTO có properties
                var response = new LoginResponseDTO();
                
                // Extract basic fields từ JSON
                if (json.Contains("\"success\":true"))
                    response.Success = true;
                else if (json.Contains("\"success\":false"))
                    response.Success = false;

                // Extract token
                response.Token = ExtractJsonValue(json, "token");
                response.RefreshToken = ExtractJsonValue(json, "refreshToken");
                response.Message = ExtractJsonValue(json, "message");

                // Extract user object
                string userJson = ExtractJsonObject(json, "user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    response.User = ParseUserProfile(userJson);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse login response: {ex.Message}");
                return new LoginResponseDTO { Success = false, Message = "Parse error" };
            }
        }

        /// <summary>
        /// Parse register response từ JSON string
        /// </summary>
        public static RegisterResponseDTO ParseRegisterResponse(string json)
        {
            try
            {
                var response = new RegisterResponseDTO();
                
                if (json.Contains("\"success\":true"))
                    response.Success = true;
                else if (json.Contains("\"success\":false"))
                    response.Success = false;

                response.Message = ExtractJsonValue(json, "message");

                // Extract user object
                string userJson = ExtractJsonObject(json, "user");
                if (!string.IsNullOrEmpty(userJson))
                {
                    response.User = ParseUserProfile(userJson);
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse register response: {ex.Message}");
                return new RegisterResponseDTO { Success = false, Message = "Parse error" };
            }
        }

        /// <summary>
        /// Parse user profile từ JSON string
        /// </summary>
        public static UserProfileDTO ParseUserProfile(string json)
        {
            try
            {
                var profile = new UserProfileDTO();
                
                profile.Id = ExtractJsonValue(json, "id");
                profile.Username = ExtractJsonValue(json, "username");
                profile.Email = ExtractJsonValue(json, "email");
                profile.DisplayName = ExtractJsonValue(json, "displayName");
                
                // Parse numbers
                if (int.TryParse(ExtractJsonValue(json, "level"), out int level))
                    profile.Level = level;
                
                if (int.TryParse(ExtractJsonValue(json, "experience"), out int experience))
                    profile.Experience = experience;

                if (int.TryParse(ExtractJsonValue(json, "totalContentsCreated"), out int totalContents))
                    profile.TotalContentsCreated = totalContents;

                if (int.TryParse(ExtractJsonValue(json, "totalQuestionsAnswered"), out int totalQuestions))
                    profile.TotalQuestionsAnswered = totalQuestions;

                if (int.TryParse(ExtractJsonValue(json, "totalCorrectAnswers"), out int totalCorrect))
                    profile.TotalCorrectAnswers = totalCorrect;

                if (int.TryParse(ExtractJsonValue(json, "streakDays"), out int streak))
                    profile.StreakDays = streak;

                // Parse dates
                if (DateTime.TryParse(ExtractJsonValue(json, "createdAt"), out DateTime createdAt))
                    profile.CreatedAt = createdAt;

                if (DateTime.TryParse(ExtractJsonValue(json, "lastLogin"), out DateTime lastLogin))
                    profile.LastLogin = lastLogin;

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse user profile: {ex.Message}");
                return new UserProfileDTO();
            }
        }

        /// <summary>
        /// Parse AI providers array từ JSON
        /// </summary>
        public static AIProviderDTO[] ParseAIProviders(string json)
        {
            try
            {
                // Server trả về array trực tiếp: [{"name":"chatgpt",...}]
                // Cần parse từng item
                json = json.Trim();
                if (!json.StartsWith("[") || !json.EndsWith("]"))
                    return new AIProviderDTO[0];

                // Remove brackets
                json = json.Substring(1, json.Length - 2);
                
                // Split by },{ pattern
                string[] items = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                
                var providers = new AIProviderDTO[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    string item = items[i];
                    if (i == 0 && !item.StartsWith("{")) item = "{" + item;
                    if (i == items.Length - 1 && !item.EndsWith("}")) item = item + "}";
                    if (i > 0 && i < items.Length - 1) item = "{" + item + "}";

                    providers[i] = new AIProviderDTO
                    {
                        Name = ExtractJsonValue(item, "name"),
                        DisplayName = ExtractJsonValue(item, "displayName"),
                        Description = ExtractJsonValue(item, "description")
                    };
                }

                return providers;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse AI providers: {ex.Message}");
                return new AIProviderDTO[0];
            }
        }

        /// <summary>
        /// Extract JSON value by key
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                string pattern = $"\"{key}\":\"";
                int startIndex = json.IndexOf(pattern);
                if (startIndex == -1)
                {
                    // Try without quotes (for numbers/booleans)
                    pattern = $"\"{key}\":";
                    startIndex = json.IndexOf(pattern);
                    if (startIndex == -1) return "";
                    
                    startIndex += pattern.Length;
                    int endIndex = json.IndexOfAny(new char[] { ',', '}' }, startIndex);
                    if (endIndex == -1) return "";
                    
                    return json.Substring(startIndex, endIndex - startIndex).Trim();
                }
                
                startIndex += pattern.Length;
                int endQuote = json.IndexOf("\"", startIndex);
                if (endQuote == -1) return "";
                
                return json.Substring(startIndex, endQuote - startIndex);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Extract JSON object by key
        /// </summary>
        private static string ExtractJsonObject(string json, string key)
        {
            try
            {
                string pattern = $"\"{key}\":{{";
                int startIndex = json.IndexOf(pattern);
                if (startIndex == -1) return "";
                
                startIndex += pattern.Length - 1; // Keep the opening brace
                
                int braceCount = 0;
                int endIndex = startIndex;
                
                for (int i = startIndex; i < json.Length; i++)
                {
                    if (json[i] == '{') braceCount++;
                    else if (json[i] == '}') braceCount--;
                    
                    if (braceCount == 0)
                    {
                        endIndex = i + 1;
                        break;
                    }
                }
                
                return json.Substring(startIndex, endIndex - startIndex);
            }
            catch
            {
                return "";
            }
        }
    }
}