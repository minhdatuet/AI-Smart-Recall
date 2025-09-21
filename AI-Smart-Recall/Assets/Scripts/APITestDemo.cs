using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Services;
using AISmartRecall.API;
using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecall
{
    /// <summary>
    /// Demo script để test các API calls
    /// </summary>
    public class APITestDemo : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField _usernameInput;
        [SerializeField] private InputField _emailInput;
        [SerializeField] private InputField _passwordInput;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _getProfileButton;
        [SerializeField] private Button _updateProfileButton;
        [SerializeField] private Button _getAIProvidersButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _userInfoText;
        
        private AuthenticationService _authService;
        
        private void Start()
        {
            // Tìm hoặc tạo AuthenticationService
            _authService = FindObjectOfType<AuthenticationService>();
            if (_authService == null)
            {
                GameObject authGO = new GameObject("AuthenticationService");
                _authService = authGO.AddComponent<AuthenticationService>();
            }
            
            // Subscribe events
            AuthenticationService.OnLoginSuccess += OnLoginSuccess;
            AuthenticationService.OnLoginFailed += OnLoginFailed;
            AuthenticationService.OnLogoutSuccess += OnLogoutSuccess;
            AuthenticationService.OnTokenExpired += OnTokenExpired;
            
            // Setup UI
            SetupUI();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe events
            AuthenticationService.OnLoginSuccess -= OnLoginSuccess;
            AuthenticationService.OnLoginFailed -= OnLoginFailed;
            AuthenticationService.OnLogoutSuccess -= OnLogoutSuccess;
            AuthenticationService.OnTokenExpired -= OnTokenExpired;
        }
        
        private void SetupUI()
        {
            if (_registerButton) _registerButton.onClick.AddListener(() => RegisterAsync().Forget());
            if (_loginButton) _loginButton.onClick.AddListener(() => LoginAsync().Forget());
            if (_logoutButton) _logoutButton.onClick.AddListener(() => LogoutAsync().Forget());
            if (_getProfileButton) _getProfileButton.onClick.AddListener(() => GetProfileAsync().Forget());
            if (_updateProfileButton) _updateProfileButton.onClick.AddListener(() => UpdateProfileAsync().Forget());
            if (_getAIProvidersButton) _getAIProvidersButton.onClick.AddListener(() => GetAIProvidersAsync().Forget());
            
            // Set default values cho testing
            if (_usernameInput) _usernameInput.text = "testuser" + UnityEngine.Random.Range(100, 999);
            if (_emailInput) _emailInput.text = "test" + UnityEngine.Random.Range(100, 999) + "@example.com";
            if (_passwordInput) _passwordInput.text = "Test123456";
        }
        
        private void UpdateUI()
        {
            bool isLoggedIn = AuthenticationService.IsLoggedIn;
            
            if (_registerButton) _registerButton.interactable = !isLoggedIn;
            if (_loginButton) _loginButton.interactable = !isLoggedIn;
            if (_logoutButton) _logoutButton.interactable = isLoggedIn;
            if (_getProfileButton) _getProfileButton.interactable = isLoggedIn;
            if (_updateProfileButton) _updateProfileButton.interactable = isLoggedIn;
            
            UpdateUserInfo();
        }
        
        private void UpdateUserInfo()
        {
            if (_userInfoText == null) return;
            
            if (AuthenticationService.IsLoggedIn && AuthenticationService.CurrentUser != null)
            {
                var user = AuthenticationService.CurrentUser;
                _userInfoText.text = $"User: {user.Username}\nEmail: {user.Email}\nDisplay Name: {user.DisplayName}\nLevel: {user.Level}";
            }
            else
            {
                _userInfoText.text = "Not logged in";
            }
        }
        
        private void UpdateStatus(string message, bool isError = false)
        {
            if (_statusText == null) return;
            
            _statusText.text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _statusText.color = isError ? Color.red : Color.green;
            
            APIConfig.LogDebug($"Status: {message}");
        }
        
        #region API Methods
        
        private async UniTaskVoid RegisterAsync()
        {
            try
            {
                UpdateStatus("Registering...");
                
                string username = _usernameInput?.text ?? "testuser";
                string email = _emailInput?.text ?? "test@example.com";
                string password = _passwordInput?.text ?? "Test123456";
                
                var response = await _authService.RegisterAsync(username, email, password);
                
                if (response.Success)
                {
                    UpdateStatus("Registration successful!");
                }
                else
                {
                    UpdateStatus($"Registration failed: {response.Message}", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Registration error: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid LoginAsync()
        {
            try
            {
                UpdateStatus("Logging in...");
                
                string email = _emailInput?.text ?? "test@example.com";
                string password = _passwordInput?.text ?? "Test123456";
                
                var response = await _authService.LoginAsync(email, password);
                
                if (response.Success)
                {
                    UpdateStatus("Login successful!");
                    UpdateUI();
                }
                else
                {
                    UpdateStatus($"Login failed: {response.Message}", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Login error: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid LogoutAsync()
        {
            try
            {
                UpdateStatus("Logging out...");
                await _authService.LogoutAsync();
                UpdateStatus("Logout successful!");
                UpdateUI();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Logout error: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid GetProfileAsync()
        {
            try
            {
                UpdateStatus("Getting profile...");
                var profile = await _authService.GetProfileAsync();
                UpdateStatus($"Profile loaded: {profile.DisplayName}");
                UpdateUserInfo();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Get profile error: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid UpdateProfileAsync()
        {
            try
            {
                UpdateStatus("Updating profile...");
                
                string newDisplayName = "Updated User " + UnityEngine.Random.Range(100, 999);
                var profile = await _authService.UpdateProfileAsync(newDisplayName, "gemini", "understanding");
                
                UpdateStatus($"Profile updated: {profile.DisplayName}");
                UpdateUserInfo();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Update profile error: {ex.Message}", true);
            }
        }
        
        private async UniTaskVoid GetAIProvidersAsync()
        {
            try
            {
                UpdateStatus("Getting AI providers...");
                var providers = await _authService.GetAIProvidersAsync();
                
                string providerList = "AI Providers:\n";
                foreach (var provider in providers)
                {
                    providerList += $"- {provider.DisplayName}: {provider.Description}\n";
                }
                
                UpdateStatus($"Found {providers.Length} AI providers");
                Debug.Log(providerList);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Get AI providers error: {ex.Message}", true);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLoginSuccess(LoginResponseDTO response)
        {
            UpdateStatus($"Welcome, {response.User.DisplayName}!");
            UpdateUI();
        }
        
        private void OnLoginFailed(string message)
        {
            UpdateStatus($"Login failed: {message}", true);
        }
        
        private void OnLogoutSuccess()
        {
            UpdateStatus("Logged out successfully");
            UpdateUI();
        }
        
        private void OnTokenExpired(string message)
        {
            UpdateStatus($"Session expired: {message}", true);
            UpdateUI();
        }
        
        #endregion
        
        #region Editor Testing
        
        [ContextMenu("Test Register")]
        private void EditorTestRegister()
        {
            if (Application.isPlaying)
            {
                RegisterAsync().Forget();
            }
        }
        
        [ContextMenu("Test Login")]
        private void EditorTestLogin()
        {
            if (Application.isPlaying)
            {
                LoginAsync().Forget();
            }
        }
        
        [ContextMenu("Test Get AI Providers")]
        private void EditorTestGetAIProviders()
        {
            if (Application.isPlaying)
            {
                GetAIProvidersAsync().Forget();
            }
        }
        
        #endregion
    }
}
