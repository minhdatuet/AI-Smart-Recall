using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Utils;
using AISmartRecall.SharedModels.DTOs;
using Newtonsoft.Json;

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
                (var response, long statusCode) = await UniTaskWebRequest.PostAsync<RegisterResponseDTO>(url, null, null, request);
                
                if (response == null)
                {
                    throw new Exception($"Registration failed with status code: {statusCode}");
                }
                
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
                (var response, long statusCode) = await UniTaskWebRequest.PostAsync<LoginResponseDTO>(url, null, null, request);
                
                if (response == null)
                {
                    string message = $"Login failed with status code: {statusCode}";
                    APIConfig.LogError(message);
                    OnLoginFailed?.Invoke(message);
                    throw new Exception(message);
                }
                
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
                
                await UniTaskWebRequest.PostAsync<object>(url, "Bearer", CurrentToken);
                
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
                
                // Sử dụng RetryWithTokenRefresh để tự động retry khi gặp lỗi 401
                var (profile, statusCode) = await RetryWithTokenRefresh(async () => 
                {
                    return await UniTaskWebRequest.GetAsync<UserProfileDTO>(url, "Bearer", CurrentToken);
                });
                
                if (profile == null)
                {
                    throw new Exception($"Failed to get profile with status code: {statusCode}");
                }
                
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
                
                // Sử dụng RetryWithTokenRefresh để tự động retry khi gặp lỗi 401
                var (updatedProfile, statusCode) = await RetryWithTokenRefresh(async () => 
                {
                    return await UniTaskWebRequest.PutAsync<UserProfileDTO>(url, "Bearer", CurrentToken, request);
                });
                
                if (updatedProfile == null)
                {
                    throw new Exception($"Failed to update profile with status code: {statusCode}");
                }
                
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
        /// Cập nhật API key cho OpenRouter
        /// </summary>
        public async UniTask UpdateAPIKeysAsync(string openRouterKey, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsLoggedIn)
                    throw new Exception("User not logged in");
                
                var request = new UpdateAPIKeysRequestDTO
                {
                    OpenRouterKey = openRouterKey
                };
                
                string url = APIConfig.GetFullURL(APIConfig.Endpoints.API_KEYS);
                
                // Sử dụng RetryWithTokenRefresh để tự động retry khi gặp lỗi 401
                var (response, statusCode) = await RetryWithTokenRefresh(async () => 
                {
                    return await UniTaskWebRequest.PutAsync<object>(url, "Bearer", CurrentToken, request);
                });
                
                if (statusCode != 200)
                {
                    throw new Exception($"Failed to update API keys with status code: {statusCode}");
                }
                
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
                (var response, long statusCode) = await UniTaskWebRequest.PostAsync<RefreshTokenResponseDTO>(url, null, null, request);
                
                if (response == null || !response.Success)
                {
                    APIConfig.LogDebug($"Token refresh failed with status code: {statusCode}");
                    return false;
                }
                
                CurrentToken = response.Token;
                CurrentRefreshToken = response.RefreshToken;
                SaveTokens();
                return true;
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
                (var providers, long statusCode) = await UniTaskWebRequest.GetAsync<AIProviderDTO[]>(url, null, null);
                
                if (providers == null)
                {
                    throw new Exception($"Failed to get AI providers with status code: {statusCode}");
                }
                
                return providers;
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"Get AI providers error: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Kiểm tra xem status code có phải là Unauthorized không
        /// </summary>
        private bool IsUnauthorized(long statusCode)
        {
            return statusCode == 401;
        }
        
        /// <summary>
        /// Retry API call với auto-refresh token nếu gặp lỗi 401
        /// </summary>
        private async UniTask<(T response, long statusCode)> RetryWithTokenRefresh<T>(Func<UniTask<(T, long)>> apiCall)
        {
            // Thử lần đầu
            var (response, statusCode) = await apiCall();
            
            // Nếu gặp lỗi 401, thử refresh token và gọi lại
            if (response == null && IsUnauthorized(statusCode) && IsLoggedIn)
            {
                APIConfig.LogDebug("Received 401, attempting token refresh...");
                
                bool refreshed = await RefreshTokenAsync();
                if (refreshed)
                {
                    // Thử lại với token mới
                    (response, statusCode) = await apiCall();
                }
                else
                {
                    // Token refresh failed, logout
                    APIConfig.LogError("Token refresh failed, logging out user");
                    ClearLocalData();
                    OnTokenExpired?.Invoke("Token expired and refresh failed");
                }
            }
            
            return (response, statusCode);
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