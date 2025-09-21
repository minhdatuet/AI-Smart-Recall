using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Utils;
using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecall.API.Services
{
    /// <summary>
    /// Service xử lý các tác vụ authentication với server
    /// </summary>
    public class AuthenticationService : MonoBehaviour
    {
        [Header("Authentication Settings")]
        [SerializeField] private bool _autoValidateToken = true;
        [SerializeField] private float _tokenValidationInterval = 300f; // 5 phút
        
        // Events
        public static event Action<LoginResponseDTO> OnLoginSuccess;
        public static event Action<string> OnLoginFailed;
        public static event Action OnLogoutSuccess;
        public static event Action<string> OnTokenExpired;
        
        // Properties
        public static string CurrentToken { get; private set; }
        public static string CurrentRefreshToken { get; private set; }
        public static UserProfileDTO CurrentUser { get; private set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentToken);
        
        private CancellationTokenSource _cancellationTokenSource;
        
        private void Awake()
        {
            // Singleton pattern
            if (FindObjectOfType<AuthenticationService>() != this)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Auto validate token nếu có
            if (_autoValidateToken)
            {
                LoadSavedTokens();
                _ = StartTokenValidationLoop();
            }
        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        
        #region Public API Methods
        
        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        public async UniTask<RegisterResponseDTO> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                APIConfig.LogDebug($"Registering user: {email}");
                
                var request = new RegisterRequestDTO
                {
                    Username = username,
                    Email = email,
                    Password = password
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.REGISTER);
                var response = await HTTPClient.PostAsync<RegisterResponseDTO>(url, request, null, cancellationToken);
                
                APIConfig.LogDebug($"Registration result: {response.Success}");
                return response;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Registration failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Đăng nhập
        /// </summary>
        public async UniTask<LoginResponseDTO> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                APIConfig.LogDebug($"Logging in user: {email}");
                
                var request = new LoginRequestDTO
                {
                    Email = email,
                    Password = password
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.LOGIN);
                var response = await HTTPClient.PostAsync<LoginResponseDTO>(url, request, null, cancellationToken);
                
                if (response.Success)
                {
                    // Lưu token và user info
                    CurrentToken = response.Token;
                    CurrentRefreshToken = response.RefreshToken;
                    CurrentUser = response.User;
                    
                    // Lưu vào PlayerPrefs
                    SaveTokens();
                    
                    APIConfig.LogDebug("Login successful");
                    OnLoginSuccess?.Invoke(response);
                }
                else
                {
                    APIConfig.LogError($"Login failed: {response.Message}");
                    OnLoginFailed?.Invoke(response.Message);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Login error: {ex.Message}");
                OnLoginFailed?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async UniTask LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsLoggedIn)
                {
                    APIConfig.LogDebug("User not logged in");
                    return;
                }
                
                // Gọi logout endpoint
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.LOGOUT);
                var headers = HTTPClient.CreateAuthHeaders(CurrentToken);
                
                await HTTPClient.PostAsync<object>(url, null, headers, cancellationToken);
                
                // Clear local data
                ClearLocalData();
                
                APIConfig.LogDebug("Logout successful");
                OnLogoutSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Logout error: {ex.Message}");
                // Vẫn clear local data ngay cả khi server call fail
                ClearLocalData();
                OnLogoutSuccess?.Invoke();
            }
        }
        
        /// <summary>
        /// Lấy thông tin profile người dùng
        /// </summary>
        public async UniTask<UserProfileDTO> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsLoggedIn)
                    throw new Exception("User not logged in");
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.PROFILE);
                var headers = HTTPClient.CreateAuthHeaders(CurrentToken);
                
                var profile = await HTTPClient.GetAsync<UserProfileDTO>(url, headers, cancellationToken);
                CurrentUser = profile;
                
                return profile;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Get profile error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Cập nhật profile người dùng
        /// </summary>
        public async UniTask<UserProfileDTO> UpdateProfileAsync(string displayName, string preferredAI, string defaultLearningMode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsLoggedIn)
                    throw new Exception("User not logged in");
                
                var request = new UpdateProfileRequestDTO
                {
                    DisplayName = displayName,
                    PreferredAIProvider = preferredAI,
                    DefaultLearningMode = defaultLearningMode
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.PROFILE);
                var headers = HTTPClient.CreateAuthHeaders(CurrentToken);
                
                var updatedProfile = await HTTPClient.PutAsync<UserProfileDTO>(url, request, headers, cancellationToken);
                CurrentUser = updatedProfile;
                
                return updatedProfile;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Update profile error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Cập nhật API keys
        /// </summary>
        public async UniTask UpdateAPIKeysAsync(Dictionary<string, string> apiKeys, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsLoggedIn)
                    throw new Exception("User not logged in");
                
                var request = new UpdateAPIKeysRequestDTO
                {
                    OpenAIKey = apiKeys.GetValueOrDefault("openai"),
                    GeminiKey = apiKeys.GetValueOrDefault("gemini"),
                    QwenKey = apiKeys.GetValueOrDefault("qwen")
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.API_KEYS);
                var headers = HTTPClient.CreateAuthHeaders(CurrentToken);
                
                await HTTPClient.PutAsync<object>(url, request, headers, cancellationToken);
                
                APIConfig.LogDebug("API keys updated successfully");
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Update API keys error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Validate token hiện tại
        /// </summary>
        public async UniTask<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO: Implement proper token validation when ValidateTokenResponseDTO is available
                // For now, just return true if user is logged in
                return IsLoggedIn;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Token validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Refresh token
        /// </summary>
        public async UniTask<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentRefreshToken))
                    return false;
                
                var request = new RefreshTokenRequestDTO
                {
                    RefreshToken = CurrentRefreshToken
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.REFRESH_TOKEN);
                var response = await HTTPClient.PostAsync<RefreshTokenResponseDTO>(url, request, null, cancellationToken);
                
                if (response.Success)
                {
                    CurrentToken = response.Token;
                    CurrentRefreshToken = response.RefreshToken;
                    SaveTokens();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Token refresh error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Lấy danh sách AI providers
        /// </summary>
        public async UniTask<AIProviderDTO[]> GetAIProvidersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.AI_PROVIDERS);
                var providers = await HTTPClient.GetAsync<AIProviderDTO[]>(url, null, cancellationToken);
                
                return providers;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Get AI providers error: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Load saved tokens từ PlayerPrefs
        /// </summary>
        private void LoadSavedTokens()
        {
            CurrentToken = PlayerPrefs.GetString("AuthToken", "");
            CurrentRefreshToken = PlayerPrefs.GetString("RefreshToken", "");
            
            // Load user profile
            string userJson = PlayerPrefs.GetString("UserProfile", "");
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    CurrentUser = JsonUtility.FromJson<UserProfileDTO>(userJson);
                }
                catch (Exception ex)
                {
                    APIConfig.LogError($"Load user profile error: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Lưu tokens vào PlayerPrefs
        /// </summary>
        private void SaveTokens()
        {
            PlayerPrefs.SetString("AuthToken", CurrentToken ?? "");
            PlayerPrefs.SetString("RefreshToken", CurrentRefreshToken ?? "");
            
            if (CurrentUser != null)
            {
                try
                {
                    string userJson = JsonUtility.ToJson(CurrentUser);
                    PlayerPrefs.SetString("UserProfile", userJson);
                }
                catch (Exception ex)
                {
                    APIConfig.LogError($"Save user profile error: {ex.Message}");
                }
            }
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Xóa tất cả dữ liệu local
        /// </summary>
        private void ClearLocalData()
        {
            CurrentToken = null;
            CurrentRefreshToken = null;
            CurrentUser = null;
            
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.DeleteKey("RefreshToken");
            PlayerPrefs.DeleteKey("UserProfile");
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loop kiểm tra token validation định kỳ
        /// </summary>
        private async UniTaskVoid StartTokenValidationLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_tokenValidationInterval), cancellationToken: _cancellationTokenSource.Token);
                    
                    if (IsLoggedIn)
                    {
                        bool isValid = await ValidateTokenAsync(_cancellationTokenSource.Token);
                        if (!isValid)
                        {
                            APIConfig.LogDebug("Token expired, attempting refresh...");
                            bool refreshed = await RefreshTokenAsync(_cancellationTokenSource.Token);
                            
                            if (!refreshed)
                            {
                                APIConfig.LogError("Token refresh failed, logging out...");
                                ClearLocalData();
                                OnTokenExpired?.Invoke("Token expired and refresh failed");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Loop bị cancel, thoát
                    break;
                }
                catch (Exception ex)
                {
                    APIConfig.LogError($"Token validation loop error: {ex.Message}");
                }
            }
        }
        
        #endregion
    }
}