using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Services;

namespace AISmartRecall.Managers
{
    /// <summary>
    /// Manager quản lý API keys đã được validate và lưu trữ
    /// </summary>
    public class APIKeyManager : MonoBehaviour
    {
        private const string API_KEY_PREF = "OpenRouter_API_Key";
        private const string API_KEY_VALID_PREF = "OpenRouter_API_Key_Valid";
        private const string API_KEY_LAST_TESTED_PREF = "OpenRouter_API_Key_LastTested";
        
        // Cache
        private static string _cachedAPIKey;
        private static bool? _cachedIsValid;
        
        // Singleton
        public static APIKeyManager Instance { get; private set; }
        
        // Events
        public static event Action<string> OnAPIKeyValidated;
        public static event Action OnAPIKeyInvalid;
        
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
                return;
            }
        }
        
        /// <summary>
        /// Lưu API key đã được validate thành công (cả local và server)
        /// </summary>
        /// <param name="apiKey">API key đã test thành công</param>
        public static async UniTask SaveValidatedAPIKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return;
            
            try
            {
                // Bước 1: Lưu lên server trước (nếu đã login)
                if (AuthenticationService.IsLoggedIn)
                {
                    var authService = FindObjectOfType<AuthenticationService>();
                    if (authService != null)
                    {
                        await authService.UpdateAPIKeysAsync(apiKey);
                        Debug.Log("[APIKeyManager] API key updated on server");
                    }
                }
                
                // Bước 2: Lưu local cache
                string encryptedKey = EncryptAPIKey(apiKey);
                PlayerPrefs.SetString(API_KEY_PREF, encryptedKey);
                PlayerPrefs.SetInt(API_KEY_VALID_PREF, 1);
                PlayerPrefs.SetString(API_KEY_LAST_TESTED_PREF, DateTime.Now.ToBinary().ToString());
                PlayerPrefs.Save();
                
                // Update cache
                _cachedAPIKey = apiKey;
                _cachedIsValid = true;
                
                Debug.Log("[APIKeyManager] API key saved locally and on server");
                
                // Trigger event
                OnAPIKeyValidated?.Invoke(apiKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIKeyManager] Error saving API key: {ex.Message}");
                // Vẫn lưu local nếu server fail
                SaveValidatedAPIKeyLocal(apiKey);
            }
        }
        
        /// <summary>
        /// Lưu API key local only (fallback method)
        /// </summary>
        private static void SaveValidatedAPIKeyLocal(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return;
            
            string encryptedKey = EncryptAPIKey(apiKey);
            PlayerPrefs.SetString(API_KEY_PREF, encryptedKey);
            PlayerPrefs.SetInt(API_KEY_VALID_PREF, 1);
            PlayerPrefs.SetString(API_KEY_LAST_TESTED_PREF, DateTime.Now.ToBinary().ToString());
            PlayerPrefs.Save();
            
            _cachedAPIKey = apiKey;
            _cachedIsValid = true;
            
            Debug.Log("[APIKeyManager] API key saved locally only");
            OnAPIKeyValidated?.Invoke(apiKey);
        }
        
        /// <summary>
        /// Lấy API key đã lưu (ưu tiên từ cache, sau đó local, cuối cùng từ server)
        /// </summary>
        /// <returns>API key hoặc null nếu không có/không hợp lệ</returns>
        public static string GetValidatedAPIKey()
        {
            // Return from cache if available
            if (!string.IsNullOrEmpty(_cachedAPIKey) && _cachedIsValid == true)
            {
                return _cachedAPIKey;
            }
            
            // Check PlayerPrefs first
            if (PlayerPrefs.HasKey(API_KEY_PREF) && PlayerPrefs.GetInt(API_KEY_VALID_PREF, 0) == 1)
            {
                try
                {
                    string encryptedKey = PlayerPrefs.GetString(API_KEY_PREF);
                    string decryptedKey = DecryptAPIKey(encryptedKey);
                    
                    // Update cache
                    _cachedAPIKey = decryptedKey;
                    _cachedIsValid = true;
                    
                    return decryptedKey;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIKeyManager] Error decrypting local API key: {ex.Message}");
                    PlayerPrefs.DeleteKey(API_KEY_PREF);
                }
            }
            
            _cachedIsValid = false;
            return null;
        }
        
        /// <summary>
        /// Lấy API key từ server (async version)
        /// </summary>
        /// <returns>API key từ server hoặc null nếu không có</returns>
        public static async UniTask<string> GetValidatedAPIKeyFromServerAsync()
        {
            try
            {
                if (!AuthenticationService.IsLoggedIn)
                {
                    Debug.Log("[APIKeyManager] User not logged in, cannot get server API key");
                    return null;
                }
                
                // Lấy user profile có chứa API key info
                var authService = FindObjectOfType<AuthenticationService>();
                if (authService != null)
                {
                    var profile = await authService.GetProfileAsync();
                    // TODO: Server cần trả về API key info trong profile
                    // Hiện tại chưa có field này trong UserProfileDTO
                    Debug.Log("[APIKeyManager] Got profile from server - API key retrieval not yet implemented");
                }
                
                return null; // Chưa implement server-side API key retrieval
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIKeyManager] Error getting API key from server: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Kiểm tra xem có API key hợp lệ không
        /// </summary>
        /// <returns>True nếu có API key đã validate</returns>
        public static bool HasValidAPIKey()
        {
            return !string.IsNullOrEmpty(GetValidatedAPIKey());
        }
        
        /// <summary>
        /// Lấy thời gian test cuối cùng
        /// </summary>
        /// <returns>DateTime của lần test cuối, hoặc DateTime.MinValue nếu chưa có</returns>
        public static DateTime GetLastTestedTime()
        {
            if (!PlayerPrefs.HasKey(API_KEY_LAST_TESTED_PREF))
                return DateTime.MinValue;
                
            try
            {
                string binaryString = PlayerPrefs.GetString(API_KEY_LAST_TESTED_PREF);
                long binary = Convert.ToInt64(binaryString);
                return DateTime.FromBinary(binary);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        
        /// <summary>
        /// Kiểm tra xem API key có cần test lại không (sau 7 ngày)
        /// </summary>
        /// <returns>True nếu cần test lại</returns>
        public static bool ShouldRetestAPIKey()
        {
            if (!HasValidAPIKey()) return false;
            
            DateTime lastTested = GetLastTestedTime();
            if (lastTested == DateTime.MinValue) return true;
            
            TimeSpan timeSinceTest = DateTime.Now - lastTested;
            return timeSinceTest.TotalDays > 7; // Test lại sau 7 ngày
        }
        
        /// <summary>
        /// Đánh dấu API key không hợp lệ
        /// </summary>
        public static void InvalidateAPIKey()
        {
            PlayerPrefs.SetInt(API_KEY_VALID_PREF, 0);
            PlayerPrefs.Save();
            
            _cachedIsValid = false;
            
            Debug.Log("[APIKeyManager] API key invalidated");
            OnAPIKeyInvalid?.Invoke();
        }
        
        /// <summary>
        /// Xóa toàn bộ API key data
        /// </summary>
        public static void ClearAPIKey()
        {
            PlayerPrefs.DeleteKey(API_KEY_PREF);
            PlayerPrefs.DeleteKey(API_KEY_VALID_PREF);
            PlayerPrefs.DeleteKey(API_KEY_LAST_TESTED_PREF);
            PlayerPrefs.Save();
            
            _cachedAPIKey = null;
            _cachedIsValid = false;
            
            Debug.Log("[APIKeyManager] API key cleared");
        }
        
        /// <summary>
        /// Test API key và lưu nếu hợp lệ (cả local và server)
        /// </summary>
        /// <param name="apiKey">API key cần test</param>
        /// <returns>True nếu hợp lệ</returns>
        public static async UniTask<bool> TestAndSaveAPIKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return false;
            
            try
            {
                Debug.Log("[APIKeyManager] Testing API key...");
                bool isValid = await TestAPIKeyAsync(apiKey);
                
                if (isValid)
                {
                    await SaveValidatedAPIKeyAsync(apiKey);
                    Debug.Log("[APIKeyManager] API key tested and saved successfully");
                }
                else
                {
                    Debug.Log("[APIKeyManager] API key test failed");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIKeyManager] Error testing API key: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Auto-validate API key khi app start (ưu tiên local, fallback server)
        /// </summary>
        public static async UniTask<bool> AutoValidateOnStartAsync()
        {
            Debug.Log("[APIKeyManager] Starting auto-validation...");
            
            // Bước 1: Kiểm tra local cache trước
            string existingKey = GetValidatedAPIKey();
            
            if (!string.IsNullOrEmpty(existingKey))
            {
                // Nếu key còn mới (dưới 7 ngày) thì không cần test lại
                if (!ShouldRetestAPIKey())
                {
                    Debug.Log("[APIKeyManager] Using cached valid API key");
                    OnAPIKeyValidated?.Invoke(existingKey);
                    return true;
                }
                
                // Test lại key local
                Debug.Log("[APIKeyManager] Retesting existing local API key...");
                try
                {
                    bool isValid = await TestAPIKeyAsync(existingKey);
                    
                    if (isValid)
                    {
                        // Update last tested time
                        PlayerPrefs.SetString(API_KEY_LAST_TESTED_PREF, DateTime.Now.ToBinary().ToString());
                        PlayerPrefs.Save();
                        
                        Debug.Log("[APIKeyManager] Existing API key still valid");
                        OnAPIKeyValidated?.Invoke(existingKey);
                        return true;
                    }
                    else
                    {
                        Debug.Log("[APIKeyManager] Local API key no longer valid");
                        InvalidateAPIKey();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIKeyManager] Error retesting local API key: {ex.Message}");
                }
            }
            
            // Bước 2: Nếu local không có hoặc không hợp lệ, thử lấy từ server
            if (AuthenticationService.IsLoggedIn)
            {
                Debug.Log("[APIKeyManager] Trying to get API key from server...");
                string serverKey = await GetValidatedAPIKeyFromServerAsync();
                
                if (!string.IsNullOrEmpty(serverKey))
                {
                    // Test server key
                    bool isServerKeyValid = await TestAPIKeyAsync(serverKey);
                    
                    if (isServerKeyValid)
                    {
                        SaveValidatedAPIKeyLocal(serverKey);
                        Debug.Log("[APIKeyManager] Server API key validated and cached");
                        OnAPIKeyValidated?.Invoke(serverKey);
                        return true;
                    }
                    else
                    {
                        Debug.Log("[APIKeyManager] Server API key is invalid");
                    }
                }
            }
            
            Debug.Log("[APIKeyManager] No valid API key found");
            return false;
        }
        
        /// <summary>
        /// Test API key với OpenRouter
        /// </summary>
        private static async UniTask<bool> TestAPIKeyAsync(string apiKey)
        {
            try
            {
                string jsonPayload = @"{
                    ""model"": ""qwen/qwen-2.5-72b-instruct:free"",
                    ""messages"": [
                        {
                            ""role"": ""user"",
                            ""content"": ""Test connection - respond with just 'OK'.""
                        }
                    ],
                    ""max_tokens"": 10
                }";
                
                using (var request = new UnityEngine.Networking.UnityWebRequest("https://openrouter.ai/api/v1/chat/completions", "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("HTTP-Referer", "https://ai-smart-recall.com");
                    request.SetRequestHeader("X-Title", "AI Smart Recall");
                    
                    request.timeout = 30;
                    
                    await request.SendWebRequest();
                    
                    return request.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIKeyManager] API test error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Simple encryption cho API key (Base64 + XOR)
        /// </summary>
        private static string EncryptAPIKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return "";
            
            try
            {
                // Simple XOR encryption với fixed key
                string encryptionKey = "AISmartRecall2024Key";
                char[] encrypted = new char[apiKey.Length];
                
                for (int i = 0; i < apiKey.Length; i++)
                {
                    encrypted[i] = (char)(apiKey[i] ^ encryptionKey[i % encryptionKey.Length]);
                }
                
                return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(encrypted));
            }
            catch
            {
                return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey));
            }
        }
        
        /// <summary>
        /// Simple decryption cho API key
        /// </summary>
        private static string DecryptAPIKey(string encryptedKey)
        {
            if (string.IsNullOrEmpty(encryptedKey)) return "";
            
            try
            {
                byte[] encryptedBytes = System.Convert.FromBase64String(encryptedKey);
                char[] encrypted = System.Text.Encoding.UTF8.GetChars(encryptedBytes);
                
                string encryptionKey = "AISmartRecall2024Key";
                char[] decrypted = new char[encrypted.Length];
                
                for (int i = 0; i < encrypted.Length; i++)
                {
                    decrypted[i] = (char)(encrypted[i] ^ encryptionKey[i % encryptionKey.Length]);
                }
                
                return new string(decrypted);
            }
            catch
            {
                // Fallback: assume it's not encrypted
                try
                {
                    return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encryptedKey));
                }
                catch
                {
                    return "";
                }
            }
        }
        
        /// <summary>
        /// Get API key info cho debugging
        /// </summary>
        public static string GetAPIKeyInfo()
        {
            if (!HasValidAPIKey())
            {
                return "Chưa có API key hợp lệ";
            }
            
            DateTime lastTested = GetLastTestedTime();
            string timeInfo = lastTested == DateTime.MinValue ? 
                "chưa rõ" : 
                lastTested.ToString("dd/MM/yyyy HH:mm");
            
            bool needsRetest = ShouldRetestAPIKey();
            string status = needsRetest ? "Cần test lại" : "Hợp lệ";
            
            return $"{status} • Tested: {timeInfo}";
        }
    }
}
