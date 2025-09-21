# AI Smart Recall - Unity API System

## Tổng Quan

Hệ thống API Unity này cung cấp kết nối với AI Smart Recall backend server, sử dụng **UniTask** cho async operations và **SharedModels** DTOs để chia sẻ data structures với server.

## Cấu Trúc Thư Mục

```
Assets/Scripts/API/
├── APIConfig.cs              # Cấu hình API (endpoints, URLs, settings)
├── Utils/
│   └── HTTPClient.cs        # HTTP client utility với UniTask
├── Services/
│   └── AuthenticationService.cs  # Service xử lý authentication
└── Models/                   # (Không cần - dùng SharedModels DLL)
```

## Tính Năng

### ✅ Đã Hoàn Thành

1. **HTTP Client (HTTPClient.cs)**
   - GET, POST, PUT, DELETE requests với UniTask
   - Automatic JSON serialization/deserialization  
   - Error handling và timeout
   - Authorization header support

2. **Authentication Service (AuthenticationService.cs)**
   - User registration & login
   - JWT token management
   - Auto token validation & refresh
   - Profile management
   - API keys management
   - Persistent storage với PlayerPrefs
   - Events cho UI components

3. **API Configuration (APIConfig.cs)**
   - Centralized endpoint management
   - Base URL configuration
   - Debug logging
   - Headers & content types

## Cách Sử Dụng

### 1. Setup Authentication Service

```csharp
// Tạo Authentication Service (thường trong scene chính)
GameObject authGO = new GameObject("AuthenticationService");
var authService = authGO.AddComponent<AuthenticationService>();

// Subscribe events
AuthenticationService.OnLoginSuccess += (response) => {
    Debug.Log($"Welcome, {response.User.DisplayName}!");
};

AuthenticationService.OnLoginFailed += (message) => {
    Debug.LogError($"Login failed: {message}");
};
```

### 2. User Registration

```csharp
try
{
    var response = await authService.RegisterAsync(username, email, password);
    if (response.Success)
    {
        Debug.Log("Registration successful!");
    }
}
catch (Exception ex)
{
    Debug.LogError($"Registration error: {ex.Message}");
}
```

### 3. User Login

```csharp
try
{
    var response = await authService.LoginAsync(email, password);
    if (response.Success)
    {
        Debug.Log($"Welcome, {response.User.DisplayName}!");
        // Token được tự động lưu
    }
}
catch (Exception ex)
{
    Debug.LogError($"Login error: {ex.Message}");
}
```

### 4. Kiểm tra Login Status

```csharp
if (AuthenticationService.IsLoggedIn)
{
    var currentUser = AuthenticationService.CurrentUser;
    Debug.Log($"Current user: {currentUser.Username}");
}
```

### 5. Get AI Providers

```csharp
try
{
    var providers = await authService.GetAIProvidersAsync();
    foreach (var provider in providers)
    {
        Debug.Log($"{provider.DisplayName}: {provider.Description}");
    }
}
catch (Exception ex)
{
    Debug.LogError($"Error: {ex.Message}");
}
```

## API Endpoints

### Authentication Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/auth/register` | Đăng ký tài khoản mới |
| POST | `/api/auth/login` | Đăng nhập |
| POST | `/api/auth/logout` | Đăng xuất |
| GET | `/api/auth/profile` | Lấy thông tin profile |
| PUT | `/api/auth/profile` | Cập nhật profile |
| PUT | `/api/auth/api-keys` | Cập nhật API keys |
| GET | `/api/auth/validate` | Validate token |
| POST | `/api/auth/refresh` | Refresh token |
| GET | `/api/auth/ai-providers` | Lấy danh sách AI providers |

## Testing

### Demo Script

Sử dụng `APITestDemo.cs` để test các API calls:

1. Add script vào một GameObject trong scene
2. Assign UI references (buttons, input fields, text)
3. Play scene và test các functions

### Context Menu Testing

AuthenticationService có sẵn context menu để test trực tiếp trong Editor:
- Right-click component → "Test Register"
- Right-click component → "Test Login" 
- Right-click component → "Test Get AI Providers"

## Cấu Hình

### API Base URL

Trong `APIConfig.cs`, thay đổi `BASE_URL`:

```csharp
public static readonly string BASE_URL = "http://localhost:5011"; // Development
// public static readonly string BASE_URL = "https://your-production-api.com"; // Production
```

### Debug Mode

```csharp
public static bool DEBUG_MODE = true; // Bật/tắt debug logs
```

### Token Validation

```csharp
[SerializeField] private bool _autoValidateToken = true;
[SerializeField] private float _tokenValidationInterval = 300f; // 5 phút
```

## Dependencies

1. **UniTask** - Async operations
   - Thêm vào Package Manager: `com.cysharp.unitask`
   - GitHub: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

2. **AISmartRecall.SharedModels.dll** - DTOs
   - Đã include trong `Assets/DLL/`
   - Contains: RegisterRequestDTO, LoginResponseDTO, UserProfileDTO, etc.

## Error Handling

- Tất cả API calls đều có try-catch
- Network errors được log và throw exception
- HTTP status codes >= 400 được xử lý
- Token expiry được tự động refresh

## Security

- JWT tokens được lưu an toàn trong PlayerPrefs
- Auto token validation mỗi 5 phút
- API keys được encrypt trước khi gửi lên server
- HTTPS được khuyến khích cho production

## Performance

- Sử dụng UniTask thay vì Coroutines - hiệu suất cao hơn
- HTTP connections được tự động dispose
- Minimal allocation với async/await pattern
- Token validation chạy background không block UI

## Roadmap

### Sắp Tới
- [ ] Content Management APIs
- [ ] Question Generation APIs  
- [ ] Learning Session APIs
- [ ] Real-time features với SignalR

### Tương Lai
- [ ] Offline mode support
- [ ] Request caching
- [ ] Retry logic
- [ ] Upload progress tracking