using UnityEngine;
using Cysharp.Threading.Tasks;
using AISmartRecall.API.Services;
using AISmartRecall.API;

/// <summary>
/// Simple API tester để check basic functionality
/// </summary>
public class SimpleAPITester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool _runTestsOnStart = true;
    [SerializeField] private string _testEmail = "test@example.com";
    [SerializeField] private string _testPassword = "Test123456";
    [SerializeField] private string _testUsername = "testuser";
    
    private AuthenticationService _authService;
    
    async void Start()
    {
        // Setup auth service
        var authGO = new GameObject("AuthenticationService");
        _authService = authGO.AddComponent<AuthenticationService>();
        
        if (_runTestsOnStart)
        {
            await UniTask.Delay(1000); // Wait for initialization
            await RunBasicTests();
        }
    }
    
    async UniTask RunBasicTests()
    {
        Debug.Log("=== Starting API Tests ===");
        
        try
        {
            // Test 1: Get AI Providers (no auth needed)
            Debug.Log("Test 1: Getting AI Providers...");
            var providers = await _authService.GetAIProvidersAsync();
            Debug.Log($"Found {providers?.Length ?? 0} AI providers");
            
            // Test 2: Register (might fail if user exists)
            Debug.Log("Test 2: Register user...");
            try
            {
                var registerResult = await _authService.RegisterAsync(_testUsername, _testEmail, _testPassword);
                Debug.Log($"Register result: {registerResult?.Success ?? false}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Register failed (expected if user exists): {ex.Message}");
            }
            
            // Test 3: Login
            Debug.Log("Test 3: Login user...");
            var loginResult = await _authService.LoginAsync(_testEmail, _testPassword);
            Debug.Log($"Login result: {loginResult?.Success ?? false}");
            if (loginResult != null && !string.IsNullOrEmpty(loginResult.Token))
            {
                Debug.Log($"Token length: {loginResult.Token.Length}");
            }
            
            if (loginResult?.Success == true)
            {
                // Test 4: Get Profile
                Debug.Log("Test 4: Get user profile...");
                var profile = await _authService.GetProfileAsync();
                Debug.Log($"Got profile for: {profile?.Username ?? "Unknown"}");
                
                // Test 5: Validate Token
                Debug.Log("Test 5: Validate token...");
                var isValid = await _authService.ValidateTokenAsync();
                Debug.Log($"Token valid: {isValid}");
            }
            
            Debug.Log("=== API Tests Completed ===");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Test failed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }
    
    [ContextMenu("Run Tests")]
    public void RunTests()
    {
        if (Application.isPlaying)
        {
            RunBasicTests().Forget();
        }
        else
        {
            Debug.LogWarning("Tests can only run in Play mode");
        }
    }
}