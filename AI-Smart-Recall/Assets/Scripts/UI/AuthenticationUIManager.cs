using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Services;
using AISmartRecall.SharedModels.DTOs;
using AISmartRecall.Managers;
using TMPro;
using System.Text;

namespace AISmartRecall.UI
{
    /// <summary>
    /// UI Manager cho Feature scene - quản lý authentication UI
    /// </summary>
    public class AuthenticationUIManager : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField _usernameInput;
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private TMP_InputField _displayNameInput;
        [SerializeField] private TMP_InputField _openRouterKeyInput;
        [SerializeField] private Dropdown _aiProviderDropdown;
        [SerializeField] private Dropdown _learningModeDropdown;
        
        [Header("Action Buttons")]
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _getProfileButton;
        [SerializeField] private Button _updateProfileButton;
        [SerializeField] private Button _getAIProvidersButton;
        [SerializeField] private Button _updateAPIKeyButton;
        [SerializeField] private Button _startButton;
        
        [Header("Display Components")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _userInfoText;
        [SerializeField] private ScrollRect _aiProvidersScrollRect;
        [SerializeField] private TMP_Text _aiProvidersText;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private GameObject _userInfoPanel;
        
        private AuthenticationService _authService;
        private AIProviderDTO[] _availableProviders;
        private bool _isAPIKeyValid = false;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Tìm hoặc tạo AuthenticationService
            InitializeAuthService();
            
            // Subscribe events
            SubscribeEvents();
            
            // Setup UI
            SetupUI();
            UpdateUI();
            
            // Load initial data
            LoadInitialData().Forget();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe events
            UnsubscribeEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeAuthService()
        {
            _authService = FindObjectOfType<AuthenticationService>();
            if (_authService == null)
            {
                GameObject authGO = new GameObject("AuthenticationService");
                _authService = authGO.AddComponent<AuthenticationService>();
            }
        }
        
        private void SubscribeEvents()
        {
            AuthenticationService.OnLoginSuccess += OnLoginSuccess;
            AuthenticationService.OnLoginFailed += OnLoginFailed;
            AuthenticationService.OnLogoutSuccess += OnLogoutSuccess;
            AuthenticationService.OnTokenExpired += OnTokenExpired;
            
            // Subscribe APIKeyManager events
            APIKeyManager.OnAPIKeyValidated += OnAPIKeyValidated;
            APIKeyManager.OnAPIKeyInvalid += OnAPIKeyInvalidated;
        }
        
        private void UnsubscribeEvents()
        {
            AuthenticationService.OnLoginSuccess -= OnLoginSuccess;
            AuthenticationService.OnLoginFailed -= OnLoginFailed;
            AuthenticationService.OnLogoutSuccess -= OnLogoutSuccess;
            AuthenticationService.OnTokenExpired -= OnTokenExpired;
            
            // Unsubscribe APIKeyManager events
            APIKeyManager.OnAPIKeyValidated -= OnAPIKeyValidated;
            APIKeyManager.OnAPIKeyInvalid -= OnAPIKeyInvalidated;
        }
        
        private void SetupUI()
        {
            // Setup button listeners
            if (_registerButton) _registerButton.onClick.AddListener(() => RegisterAsync().Forget());
            if (_loginButton) _loginButton.onClick.AddListener(() => LoginAsync().Forget());
            if (_logoutButton) _logoutButton.onClick.AddListener(() => LogoutAsync().Forget());
            if (_getProfileButton) _getProfileButton.onClick.AddListener(() => GetProfileAsync().Forget());
            if (_updateProfileButton) _updateProfileButton.onClick.AddListener(() => UpdateProfileAsync().Forget());
            if (_getAIProvidersButton) _getAIProvidersButton.onClick.AddListener(() => GetAIProvidersAsync().Forget());
            if (_updateAPIKeyButton) _updateAPIKeyButton.onClick.AddListener(() => UpdateAPIKeyAsync().Forget());
            if (_startButton) _startButton.onClick.AddListener(() => StartLearningAsync().Forget());
            
            // Setup dropdowns
            SetupLearningModeDropdown();
            
            // Set default values cho testing
            // SetDefaultTestValues();
        }
        
        private void SetDefaultTestValues()
        {
            if (_usernameInput) _usernameInput.text = "testuser" + UnityEngine.Random.Range(100, 999);
            if (_emailInput) _emailInput.text = "test" + UnityEngine.Random.Range(100, 999) + "@example.com";
            if (_passwordInput) _passwordInput.text = "Test123456";
            if (_displayNameInput) _displayNameInput.text = "Test User " + UnityEngine.Random.Range(100, 999);
        }
        
        private void SetupLearningModeDropdown()
        {
            if (_learningModeDropdown != null)
            {
                _learningModeDropdown.options.Clear();
                _learningModeDropdown.options.Add(new Dropdown.OptionData("Memorization"));
                _learningModeDropdown.options.Add(new Dropdown.OptionData("Understanding"));
                _learningModeDropdown.value = 1; // Default to Understanding
                _learningModeDropdown.RefreshShownValue();
            }
        }
        
        private async UniTaskVoid LoadInitialData()
        {
            // Load AI providers khi khởi động
            // await GetAIProvidersAsync();
            if (AuthenticationService.IsLoggedIn)
            {
                GetProfileAsync().Forget();
                CheckAPIKeyOnStartup().Forget();
            }
        }
        
        /// <summary>
        /// Kiểm tra API key khi app khởi động
        /// </summary>
        private async UniTaskVoid CheckAPIKeyOnStartup()
        {
            // Delay một chút để UI load xong
            await UniTask.Delay(500);
            
            bool hasValidKey = await APIKeyManager.AutoValidateOnStartAsync();
            
            if (hasValidKey)
            {
                _isAPIKeyValid = true;
                UpdateStatus("API Key hợp lệ - sẵn sàng sử dụng!");
            }
            else
            {
                _isAPIKeyValid = false;
                UpdateStatus("Chưa có API Key hợp lệ. Vui lòng nhập và test API Key.");
            }
            
            UpdateUI();
        }
        
        #endregion
        
        #region UI Management
        
        private void UpdateUI()
        {
            bool isLoggedIn = AuthenticationService.IsLoggedIn;
            
            // Enable/disable buttons based on login status
            if (_registerButton) _registerButton.interactable = !isLoggedIn;
            if (_loginButton) _loginButton.interactable = !isLoggedIn;
            if (_logoutButton) _logoutButton.interactable = isLoggedIn;
            if (_getProfileButton) _getProfileButton.interactable = isLoggedIn;
            if (_updateProfileButton) _updateProfileButton.interactable = isLoggedIn;
            
            // Update API Key button - cần login để update
            if (_updateAPIKeyButton) _updateAPIKeyButton.interactable = isLoggedIn;
            
            // Start button - chỉ hiển thị sau khi API key đã được validate và update thành công
            if (_startButton) _startButton.gameObject.SetActive(_isAPIKeyValid && isLoggedIn);
            
            // Show/hide panels
            if (_loginPanel) _loginPanel.SetActive(!isLoggedIn);
            if (_userInfoPanel) _userInfoPanel.SetActive(isLoggedIn);
            
            // Update user info display
            UpdateUserInfo();
        }
        
        private void UpdateUserInfo()
        {
            if (_userInfoText == null) return;
            
            if (AuthenticationService.IsLoggedIn && AuthenticationService.CurrentUser != null)
            {
                var user = AuthenticationService.CurrentUser;
                _userInfoText.text = $"<b>Thông tin người dùng:</b>\n" +
                                   $"Username: {user.Username}\n" +
                                   $"Email: {user.Email}\n" +
                                   $"Display Name: {user.DisplayName}\n" +
                                   $"Level: {user.Level}\n" +
                                   $"Experience: {user.Experience}\n" +
                                   $"Streak Days: {user.StreakDays}";
            }
            else
            {
                _userInfoText.text = "<b>Chưa đăng nhập</b>\n\nHãy đăng nhập hoặc đăng ký tài khoản mới để sử dụng các tính năng.";
            }
        }
        
        private void UpdateStatus(string message, bool isError = false)
        {
            if (_statusText == null) return;
            
            _statusText.text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _statusText.color = isError ? Color.red : Color.green;
            
            Debug.Log($"Status: {message}");
        }
        
        #endregion
        
        #region API Methods
        
        private async UniTaskVoid RegisterAsync()
        {
            try
            {
                UpdateStatus("Đang đăng ký tài khoản...");
                
                string username = _usernameInput?.text ?? "testuser";
                string email = _emailInput?.text ?? "test@example.com";
                string password = _passwordInput?.text ?? "Test123456";
                
                var response = await _authService.RegisterAsync(username, email, password);
                
                if (response.Success)
                {
                    UpdateStatus("Đăng ký thành công!");
                }
                else
                {
                    UpdateStatus($"Đăng ký thất bại: {response.Message}", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi đăng ký: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid LoginAsync()
        {
            try
            {
                UpdateStatus("Đang đăng nhập...");
                
                string email = _emailInput?.text ?? "test@example.com";
                string password = _passwordInput?.text ?? "Test123456";
                
                var response = await _authService.LoginAsync(email, password);
                
                if (response.Success)
                {
                    UpdateStatus("Đăng nhập thành công!");
                    UpdateUI();
                }
                else
                {
                    UpdateStatus($"Đăng nhập thất bại: {response.Message}", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi đăng nhập: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid LogoutAsync()
        {
            try
            {
                UpdateStatus("Đang đăng xuất...");
                await _authService.LogoutAsync();
                UpdateStatus("Đăng xuất thành công!");
                UpdateUI();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi đăng xuất: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid GetProfileAsync()
        {
            try
            {
                UpdateStatus("Đang tải thông tin profile...");
                var profile = await _authService.GetProfileAsync();
                UpdateStatus($"Đã tải profile: {profile.DisplayName}");
                UpdateUserInfo();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi lấy profile: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid UpdateProfileAsync()
        {
            try
            {
                UpdateStatus("Đang cập nhật profile...");
                
                string displayName = _displayNameInput?.text ?? "Updated User";
                string selectedProvider = GetSelectedAIProvider();
                string selectedMode = GetSelectedLearningMode();
                
                var profile = await _authService.UpdateProfileAsync(displayName, selectedProvider, selectedMode);
                UpdateStatus($"Cập nhật profile thành công: {profile.DisplayName}");
                UpdateUserInfo();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi cập nhật profile: {ex.Message}", true);
            }
        }
        
        private async UniTask GetAIProvidersAsync()
        {
            try
            {
                UpdateStatus("Đang tải danh sách AI providers...");
                var providers = await _authService.GetAIProvidersAsync();
                
                _availableProviders = providers;
                UpdateAIProvidersDisplay(providers);
                UpdateAIProviderDropdown(providers);
                
                UpdateStatus($"Đã tải {providers.Length} AI providers");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi lấy AI providers: {ex.Message}", true);
            }
        }
        
        /// <summary>
        /// Test và cập nhật OpenRouter API key bằng APIKeyManager
        /// Logic: APIKeyManager sẽ tự động test và lưu cả local + server
        /// </summary>
        private async UniTaskVoid UpdateAPIKeyAsync()
        {
            try
            {
                if (!AuthenticationService.IsLoggedIn)
                {
                    UpdateStatus("Vui lòng đăng nhập trước khi cập nhật API Key!", true);
                    return;
                }
                
                string apiKey = _openRouterKeyInput?.text;
                if (string.IsNullOrEmpty(apiKey))
                {
                    UpdateStatus("Vui lòng nhập OpenRouter API Key!", true);
                    return;
                }
                
                UpdateStatus("Đang kiểm tra và lưu API Key...");
                
                // Sử dụng APIKeyManager để test và lưu API key
                bool isValid = await APIKeyManager.TestAndSaveAPIKeyAsync(apiKey);
                
                if (isValid)
                {
                    _isAPIKeyValid = true;
                    UpdateStatus("API Key hợp lệ và đã được lưu! Button Start sẵn sàng.");
                }
                else
                {
                    _isAPIKeyValid = false;
                    UpdateStatus("API Key không hợp lệ, không thể lưu!", true);
                }
                
                UpdateUI();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi cập nhật API Key: {ex.Message}", true);
                _isAPIKeyValid = false;
                UpdateUI();
            }
        }
        
        /// <summary>
        /// Test API key internal - trả về true/false thay vì hiển thị message
        /// </summary>
        private async UniTask<bool> TestAPIKeyInternalAsync(string apiKey)
        {
            try
            {
                string jsonPayload = @"{
                    ""model"": ""qwen/qwen-2.5-72b-instruct:free"",
                    ""messages"": [
                        {
                            ""role"": ""user"",
                            ""content"": ""Test message - respond with 'OK' in Vietnamese.""
                        }
                    ],
                    ""max_tokens"": 50
                }";
                
                using (UnityWebRequest request = new UnityWebRequest("https://openrouter.ai/api/v1/chat/completions", "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("HTTP-Referer", "https://ai-smart-recall.com");
                    request.SetRequestHeader("X-Title", "AI Smart Recall");
                    
                    request.timeout = 30;
                    
                    await request.SendWebRequest();
                    
                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test API Key error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Bắt đầu learning session - chuyển sang Learning scene
        /// </summary>
        private async UniTaskVoid StartLearningAsync()
        {
            try
            {
                // Kiểm tra API key thông qua APIKeyManager
                if (!APIKeyManager.HasValidAPIKey())
                {
                    UpdateStatus("Không có API Key hợp lệ! Vui lòng nhập và test API Key trước.", true);
                    return;
                }
                
                if (!AuthenticationService.IsLoggedIn)
                {
                    UpdateStatus("Vui lòng đăng nhập trước khi bắt đầu học!", true);
                    return;
                }
                
                UpdateStatus("Đang chuẩn bị learning session...");
                
                // Lấy API key để truyền sang Learning scene
                string validAPIKey = APIKeyManager.GetValidatedAPIKey();
                Debug.Log($"[AuthenticationUI] Starting Learning session with API key: {validAPIKey?.Substring(0, Math.Min(10, validAPIKey.Length))}...");
                
                // Lưu thông tin cần thiết vào PlayerPrefs hoặc static class để truyền sang scene khác
                PrepareDataForLearningScene();
                
                // Fade out effect (optional)
                await UniTask.Delay(500);
                
                UpdateStatus("Đang chuyển đến màn hình học tập...");
                
                // Chấm delay để hiện thị message
                await UniTask.Delay(1000);
                
                // Chuyển sang Learning scene
                Debug.Log("[AuthenticationUI] Loading Learning scene...");
                SceneManager.LoadScene("Learning");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthenticationUI] Error starting learning session: {ex.Message}");
                UpdateStatus($"Lỗi khởi tạo learning session: {ex.Message}", true);
            }
        }
        
        /// <summary>
        /// Chuẩn bị dữ liệu cần truyền sang Learning scene
        /// </summary>
        private void PrepareDataForLearningScene()
        {
            try
            {
                // Tạo SessionData từ thông tin hiện tại
                string apiKey = APIKeyManager.GetValidatedAPIKey();
                
                SessionData sessionData;
                if (AuthenticationService.IsLoggedIn && AuthenticationService.CurrentUser != null)
                {
                    // Tạo từ user profile
                    sessionData = SessionData.FromUserProfile(
                        AuthenticationService.CurrentUser,
                        apiKey,
                        SceneManager.GetActiveScene().name,
                        GetSelectedAIProvider(),
                        GetSelectedLearningMode()
                    );
                }
                else
                {
                    // Tạo session data mặc định cho guest
                    sessionData = new SessionData
                    {
                        APIKey = apiKey,
                        Username = "Guest",
                        DisplayName = "Guest User",
                        UserLevel = 1,
                        UserExperience = 0,
                        SessionStartTime = DateTime.Now,
                        SourceScene = SceneManager.GetActiveScene().name,
                        SelectedAIProvider = GetSelectedAIProvider(),
                        SelectedLearningMode = GetSelectedLearningMode()
                    };
                }
                
                // Lưu session data
                SessionDataManager.SaveSessionData(sessionData);
                
                Debug.Log($"[AuthenticationUI] Session data prepared: {SessionDataManager.GetSessionDataInfo()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthenticationUI] Error preparing learning data: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private void UpdateAIProvidersDisplay(AIProviderDTO[] providers)
        {
            if (_aiProvidersText == null) return;
            
            string providerList = "<b>Danh sách AI Providers:</b>\n\n";
            foreach (var provider in providers)
            {
                providerList += $"<b>{provider.DisplayName}</b>\n";
                providerList += $"Name: {provider.Name}\n";
                providerList += $"Description: {provider.Description}\n";
                providerList += $"Available: {(provider.IsAvailable ? "Có" : "Không")}\n";
                
                if (provider.SupportedLanguages != null && provider.SupportedLanguages.Count > 0)
                {
                    providerList += $"Languages: {string.Join(", ", provider.SupportedLanguages)}\n";
                }
                
                providerList += "\n";
            }
            
            _aiProvidersText.text = providerList;
        }
        
        private void UpdateAIProviderDropdown(AIProviderDTO[] providers)
        {
            if (_aiProviderDropdown == null) return;
            
            _aiProviderDropdown.options.Clear();
            foreach (var provider in providers)
            {
                _aiProviderDropdown.options.Add(new Dropdown.OptionData(provider.DisplayName));
            }
            
            if (providers.Length > 0)
            {
                _aiProviderDropdown.value = 0;
                _aiProviderDropdown.RefreshShownValue();
            }
        }
        
        private string GetSelectedAIProvider()
        {
            if (_aiProviderDropdown == null || _availableProviders == null) return "gemini";
            
            int selectedIndex = _aiProviderDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < _availableProviders.Length)
            {
                return _availableProviders[selectedIndex].Name;
            }
            
            return "gemini";
        }
        
        private string GetSelectedLearningMode()
        {
            if (_learningModeDropdown == null) return "understanding";
            
            return _learningModeDropdown.value == 0 ? "memorization" : "understanding";
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLoginSuccess(LoginResponseDTO response)
        {
            UpdateStatus($"Chào mừng, {response.User.DisplayName}!");
            UpdateUI();
        }
        
        private void OnLoginFailed(string message)
        {
            UpdateStatus($"Đăng nhập thất bại: {message}", true);
        }
        
        private void OnLogoutSuccess()
        {
            UpdateStatus("Đã đăng xuất thành công");
            UpdateUI();
        }
        
        private void OnTokenExpired(string message)
        {
            UpdateStatus($"Phiên đăng nhập hết hạn: {message}", true);
            UpdateUI();
        }
        
        /// <summary>
        /// Xử lý khi APIKeyManager xác nhận API key hợp lệ
        /// </summary>
        private void OnAPIKeyValidated(string apiKey)
        {
            _isAPIKeyValid = true;
            UpdateStatus($"API Key đã được xác thực: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
            UpdateUI();
            
            // Hiển thị API key trong input field (nếu cần)
            if (_openRouterKeyInput != null && string.IsNullOrEmpty(_openRouterKeyInput.text))
            {
                _openRouterKeyInput.text = apiKey;
            }
        }
        
        /// <summary>
        /// Xử lý khi API key không hợp lệ
        /// </summary>
        private void OnAPIKeyInvalidated()
        {
            _isAPIKeyValid = false;
            UpdateStatus("API Key không còn hợp lệ. Vui lòng nhập lại.", true);
            UpdateUI();
        }
        
        #endregion
        
        #region OpenRouter Minimal Models
        
        [Serializable]
        private class OpenRouterResponse
        {
            public OpenRouterChoice[] choices;
        }
        
        [Serializable]
        private class OpenRouterChoice
        {
            public OpenRouterMessage message;
        }
        
        [Serializable]
        private class OpenRouterMessage
        {
            public string role;
            public string content;
        }
        
        #endregion
    }
}
