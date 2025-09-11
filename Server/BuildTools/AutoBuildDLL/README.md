# AI Smart Recall - MemoryPack Build & Deploy Tools

> **🚀 Công cụ tự động hóa build và deploy MemoryPack DLLs cho Unity client**

[![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-blue.svg)](https://docs.microsoft.com/en-us/powershell/)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![MemoryPack](https://img.shields.io/badge/MemoryPack-1.21.1-green.svg)](https://github.com/Cysharp/MemoryPack)

## 📋 Tổng Quan

Tool này tự động hóa toàn bộ quy trình build và deploy cho AI Smart Recall project:

1. **🔨 Build Solution**: Tự động build SharedModels và MemoryPackSerializer
2. **⚙️ Generate Serializers**: Chạy MemoryPackSerializer để test và validate DTOs
3. **📦 Deploy to Unity**: Copy các DLL cần thiết vào Unity Assets/DLL/

## ✨ Tính Năng Chính

- **🧠 Phát Hiện Đường Dẫn Thông Minh**: Tự động tìm solution và project paths
- **⚡ Chế Độ Deploy Nhanh**: Bỏ qua build khi chỉ cần copy DLL
- **🔍 Hash Checking**: So sánh file changes để chỉ copy khi cần thiết  
- **🎯 Setup Đơn Giản**: Chỉ cần config đường dẫn Unity project
- **🛡️ Comprehensive Testing**: Test serialization của tất cả DTOs

## 🚀 Cách Sử Dụng Nhanh

### 🎯 Cách 1: Double-Click Batch Files
```
🔧 BuildDeploy.bat     → Build đầy đủ + deploy
⚡ QuickDeploy.bat     → Deploy nhanh (bỏ qua build)  
🔍 ShowConfig.bat      → Xem thông tin cấu hình
```

### 💻 Cách 2: Lệnh PowerShell
```powershell
# Build đầy đủ và deploy
.\AutoBuildDeploy.ps1

# Deploy nhanh (bỏ qua build)
.\AutoBuildDeploy.ps1 -SkipBuild

# Xem thông tin cấu hình
.\AutoBuildDeploy.ps1 -ShowConfig

# Chế độ verbose (hiển thị chi tiết)
.\AutoBuildDeploy.ps1 -Verbose
```

## ⚙️ Cấu Hình

### 🔧 Cần Cấu Hình (Config.ps1)
Chỉ cần sửa **một đường dẫn** trong `Config.ps1`:

```powershell
# Đường dẫn đến Assets/DLL của Unity project
$Global:UnityDLLPath = "D:\AI-Smart-Recall\Unity\AISmartRecallClient\Assets\DLL"
```

### 🤖 Tự Động Phát Hiện
Tool tự động tìm:
- ✅ Solution file (.sln) 
- ✅ Đường dẫn bin của SharedModels và MemoryPackSerializer
- ✅ Tất cả dependencies cần thiết

## 📦 Files Được Deploy

| File | Mô Tả | Nguồn |
|------|-------|--------|
| `AISmartRecall.SharedModels.dll` | Shared DTOs với MemoryPack | SharedModels project |
| `MemoryPack.dll` | MemoryPack runtime | NuGet package |
| `MemoryPackSerializer.dll` | Custom serializer tool | MemoryPackSerializer project |

## 🧪 MemoryPack DTOs

### 📝 Authentication DTOs
- `LoginRequestDTO` / `LoginResponseDTO`
- `RegisterRequestDTO` / `RegisterResponseDTO` 
- `UserProfileDTO`
- `UpdateProfileRequestDTO`
- `RefreshTokenRequestDTO` / `RefreshTokenResponseDTO`

### 📚 Content & Question DTOs  
- `CreateContentRequestDTO` / `ContentDTO`
- `GenerateQuestionsRequestDTO` / `QuestionDTO`
- `GetQuestionsRequestDTO` / `GetQuestionsResponseDTO`

### 🎮 Learning Session DTOs
- `StartLearningSessionRequestDTO` / `LearningSessionDTO`
- `SubmitAnswerRequestDTO` / `SubmitAnswerResponseDTO`
- `UserProgressDTO` / `LearningStatisticsDTO`

### 👥 Room Learning DTOs
- `CreateRoomRequestDTO` / `LearningRoomDTO`
- `JoinRoomRequestDTO` / `RoomParticipantDTO`
- `RoomEventDTO` (cho SignalR real-time)

## 📊 Kết Quả Mong Đợi

### 🎉 Khi Chạy Thành Công
```
=== AI SMART RECALL AUTO BUILD AND DEPLOY ===

=> Buoc 1: Build solution...
[OK] Build solution thanh cong!

=> Buoc 2: Chay MemoryPackSerializer.exe...
✅ Found 25 MemoryPackable types
🧪 Testing MemoryPack serialization...
✅ All serialization tests passed!
[OK] Chay MemoryPackSerializer thanh cong!

=> Buoc 3: Copy cac file DLL sang Unity...
[NEW] AISmartRecall.SharedModels.dll
[SKIP] MemoryPack.dll (khong thay doi)
[UPDATE] MemoryPackSerializer.dll

=== KET QUA ===
File moi: 1, Cap nhat: 1, Bo qua: 1
[OK] Hoan thanh quy trinh build va deploy!
```

## 🛠️ Khi Nào Sử Dụng

| Tình Huống | Tool Khuyến Cáo | Mô Tả |
|------------|-----------------|-------|
| 🆕 **Lần Đầu Setup** | `BuildDeploy.bat` | Build và deploy hoàn chỉnh |
| 🔄 **Thay Đổi DTOs** | `BuildDeploy.bat` | Build lại đảm bảo consistency |
| ⚡ **Chỉ Copy DLL** | `QuickDeploy.bat` | Nhanh chóng khi DLL đã có |
| 🔍 **Debug Issues** | `ShowConfig.bat` | Kiểm tra paths và settings |

## 🚨 Xử Lý Lỗi

### ❌ Lỗi "Execution Policy"
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### ❌ Lỗi "Solution file không tồn tại"  
- Đảm bảo chạy từ đúng thư mục có solution file
- Kiểm tra Config.ps1 có đúng paths không

### ❌ Lỗi "MemoryPack serialization failed"
- Kiểm tra DTOs có `[MemoryPackable]` attribute
- Đảm bảo các properties có getter/setter public

## 🔮 Unity Integration

Sau khi deploy thành công, trong Unity:

```csharp
using AISmartRecall.SharedModels.DTOs;
using MemoryPack;

// Sử dụng DTOs trong networking code
var loginRequest = new LoginRequestDTO 
{
    Email = "user@example.com",
    Password = "password123"
};

// Serialize để gửi qua network
byte[] data = MemoryPackSerializer.Serialize(loginRequest);

// Deserialize khi nhận response
var response = MemoryPackSerializer.Deserialize<LoginResponseDTO>(responseData);
```

## 📈 So Sánh vs Protobuf

| Feature | MemoryPack | Protobuf |
|---------|------------|----------|
| **Performance** | ⚡ Nhanh hơn 2-10x | 🐢 Chậm hơn |
| **Size** | 📦 Nhỏ gọn | 📦 Tương đương |  
| **C# Native** | ✅ Native support | ❌ Generated code |
| **Unity Support** | ✅ Excellent | ⚠️ Cần setup |

---

**🎯 Ready to build! Chỉ cần double-click BuildDeploy.bat để bắt đầu!** 🚀
