# 📋 Hướng Dẫn Setup UI cho Learning System

## 📋 **Tổng Quan**
Hướng dẫn này sẽ giúp bạn setup UI hoàn chỉnh cho Learning System với tất cả components đã được tạo.

---

## 🎯 **1. LearningSessionUI Setup**

### **Tạo Main Learning Panel**
1. **Tạo GameObject** `LearningSessionPanel`
2. **Add Canvas Group** (để fade in/out)
3. **Cấu trúc hierarchy:**

```
LearningSessionPanel
├── Header
│   ├── SessionTitle (TextMeshPro)
│   ├── ContentInfo (TextMeshPro)
│   └── ExitButton (Button)
├── MainContent
│   ├── ProgressArea
│   │   └── ProgressTrackerUI (sẽ setup sau)
│   ├── QuestionArea
│   │   └── QuestionDisplayUI (sẽ setup sau)
│   └── NavigationArea
│       ├── PreviousButton (Button)
│       ├── NextButton (Button)
│       ├── SubmitButton (Button)
│       └── PauseButton (Button)
├── FeedbackPanel (initially inactive)
│   ├── Background (Image)
│   ├── FeedbackIcon (Image)
│   ├── FeedbackText (TextMeshPro)
│   └── ContinueButton (Button)
└── LoadingPanel (initially inactive)
    ├── LoadingSpinner (Image - rotating)
    └── LoadingText (TextMeshPro)
```

### **Component Settings:**
- **LearningSessionUI script** gán vào `LearningSessionPanel`
- **Assign references** trong Inspector:
  - Session Panel: `LearningSessionPanel`
  - Question Display: `QuestionDisplayUI`
  - Progress Tracker: `ProgressTrackerUI`
  - Buttons và UI elements tương ứng

---

## 📊 **2. ProgressTrackerUI Setup**

### **Tạo Progress Panel**
```
ProgressTrackerPanel
├── ProgressBar
│   ├── Background (Slider Background)
│   ├── FillArea
│   │   └── Fill (Slider Fill - gradient color)
│   └── MilestonesParent
│       └── Milestone_Prefab (sẽ tạo prefab)
├── QuestionInfo
│   ├── CurrentQuestion (TextMeshPro)
│   ├── TotalQuestions (TextMeshPro)
│   └── AnsweredQuestions (TextMeshPro)
├── TimeInfo
│   ├── ElapsedTime (TextMeshPro)
│   ├── EstimatedTime (TextMeshPro)
│   └── TimerIcon (Image)
└── ProgressText (TextMeshPro)
```

### **Milestone Prefab:**
```
MilestonePrefab
├── Background (Image - small circle)
├── Text (TextMeshPro - percentage)
└── HighlightEffect (Image - glow effect)
```

### **Slider Settings:**
- **Min Value:** 0
- **Max Value:** 1
- **Whole Numbers:** Off
- **Fill Color:** Gradient từ xanh lá đến đỏ

---

## ❓ **3. QuestionDisplayUI Setup**

### **Tạo Question Panel**
```
QuestionPanel
├── QuestionContent
│   ├── QuestionTypeLabel (TextMeshPro)
│   ├── QuestionText (TextMeshPro) - Hiển thị question.Question
│   └── InstructionText (TextMeshPro)
├── AnswerAreas
│   ├── MultipleChoiceArea (initially inactive)
│   │   ├── ChoicesParent (Vertical Layout Group)
│   │   └── ChoiceToggle_Prefab
│   ├── FillBlankArea (initially inactive)
│   │   ├── FillBlankText (TextMeshPro)
│   │   └── BlanksParent (Vertical Layout Group)
│   ├── TextInputArea (initially inactive)
│   │   ├── TextInputField (TMP_InputField)
│   │   └── CharacterCount (TextMeshPro)
│   ├── TrueFalseArea (initially inactive)
│   │   ├── TrueToggle (Toggle)
│   │   └── FalseToggle (Toggle)
│   └── MatchingArea (initially inactive)
│       ├── LeftMatchingParent
│       └── RightMatchingParent
└── QuestionBackground (Image)
```

### **Prefabs cần tạo:**

#### **ChoiceToggle_Prefab:**
```
ChoiceToggle
├── Background (Toggle Background)
├── Checkmark (Toggle Checkmark)
└── ChoiceText (TextMeshPro)
```

#### **BlankInput_Prefab:**
```
BlankInput
├── InputField (TMP_InputField)
└── Label (TextMeshPro - "Câu trả lời 1")
```

#### **MatchingItem_Prefab:**
```
MatchingItem
├── Background (Button Background)
├── Text (TextMeshPro)
└── SelectionHighlight (Image - initially inactive)
```

---

## 🏆 **4. ResultsUI Setup**

### **Tạo Results Panel**
```
ResultsPanel
├── Header
│   ├── ResultsTitle (TextMeshPro)
│   └── CloseButton (Button)
├── MainContent (Scroll View)
│   ├── OverallResults
│   │   ├── ScoreSection
│   │   │   ├── ScoreText (TextMeshPro)
│   │   │   ├── GradeText (TextMeshPro)
│   │   │   ├── GradeIcon (Image)
│   │   │   └── ScoreProgressBar (Slider)
│   │   └── StatsSection
│   │       ├── TotalQuestions (TextMeshPro)
│   │       ├── CorrectAnswers (TextMeshPro)
│   │       ├── IncorrectAnswers (TextMeshPro)
│   │       ├── AccuracyPercentage (TextMeshPro)
│   │       ├── TotalTime (TextMeshPro)
│   │       └── AverageTime (TextMeshPro)
│   ├── QuestionTypeBreakdown
│   │   ├── BreakdownTitle (TextMeshPro)
│   │   └── QuestionTypesParent (Vertical Layout Group)
│   ├── PerformanceChart
│   │   ├── ChartTitle (TextMeshPro)
│   │   ├── ChartArea (RectTransform)
│   │   └── ChartParent (RectTransform)
│   ├── DetailedResults
│   │   ├── DetailedTitle (TextMeshPro)
│   │   └── DetailedScrollView
│   │       └── DetailedResultsParent (Vertical Layout Group)
│   ├── Recommendations
│   │   ├── RecommendationsTitle (TextMeshPro)
│   │   ├── RecommendationsText (TextMeshPro)
│   │   └── ImprovementAreasParent (Vertical Layout Group)
│   └── Achievements
│       ├── AchievementsTitle (TextMeshPro)
│       └── AchievementsParent (Horizontal Layout Group)
└── Footer
    ├── RetryButton (Button)
    ├── ContinueButton (Button)
    └── CloseButton (Button)
```

### **Prefabs cho Results:**

#### **QuestionTypeItem_Prefab:**
```
QuestionTypeItem
├── Background (Image)
├── TypeText (TextMeshPro)
├── StatsText (TextMeshPro)
└── AccuracyText (TextMeshPro)
```

#### **QuestionResultItem_Prefab:**
```
QuestionResultItem
├── Background (Image)
├── QuestionNumber (TextMeshPro)
├── QuestionText (TextMeshPro)
├── UserAnswer (TextMeshPro)
├── ResultIcon (Image)
└── Feedback (TextMeshPro)
```

#### **ChartBar_Prefab:**
```
ChartBar
├── BarImage (Image - height thay đổi theo accuracy)
└── LabelText (TextMeshPro)
```

#### **ImprovementArea_Prefab:**
```
ImprovementArea
├── Background (Image)
├── Icon (Image)
└── Text (TextMeshPro)
```

#### **Achievement_Prefab:**
```
Achievement
├── Background (Image)
├── Icon (Image)
└── Text (TextMeshPro)
```

---

## 🔧 **5. BaseQuestion & AnswerValidationSystem**

### **BaseQuestion Data Model:**
Hệ thống sử dụng BaseQuestion làm lớp cơ sở cho tất cả loại câu hỏi:

#### **Thuộc tính quan trọng:**
- `Question`: Nội dung câu hỏi (thay vì QuestionText)
- `QuestionType`: Enum xác định loại câu hỏi
- `Options`: List các lựa chọn (cho multiple choice)
- `CorrectAnswer`: Đáp án đúng
- `Explanation`: Giải thích đáp án

#### **Methods hỗ trợ:**
```csharp
// Lấy options cho multiple choice
List<string> options = question.GetOptions();

// Lấy đáp án đúng
string correctAnswer = question.GetCorrectAnswer();

// Kiểm tra xem có cần AI grading không
bool needsAI = question.RequiresAIGrading();
```

### **AnswerValidationSystem Features:**

#### **Validation Methods:**
1. **Automatic Validation:**
   - Multiple Choice: Exact match
   - True/False: Boolean comparison
   - Fill in Blank: Partial matching với 80% threshold
   - Matching: Dictionary-based pair comparison
   - Text Input: Keyword-based scoring

2. **AI Validation:**
   - Sử dụng OpenRouter API
   - Timeout protection (30 seconds)
   - Fallback to automatic nếu AI fail
   - Structured JSON response parsing

#### **Configuration Settings:**
```csharp
// Case sensitivity
public bool CaseSensitive = false;

// Ignore whitespace
public bool IgnoreWhitespace = true;

// Partial credit threshold (0.0 - 1.0)
public float PartialCreditThreshold = 0.5f;

// AI grading timeout
public float AIGradingTimeout = 30f;
```

#### **Events System:**
```csharp
// Subscribe to validation events
AnswerValidationSystem.OnAnswerValidated += HandleAnswerValidated;
AnswerValidationSystem.OnValidationError += HandleValidationError;
AnswerValidationSystem.OnAIGradingProgress += HandleAIProgress;
```

---

## 🔧 **6. LearningSceneManager Integration**

### **Tạo Main Learning Scene:**
```
LearningScene
├── Canvas (Screen Space - Overlay)
│   ├── ContentInputUI (existing)
│   ├── QuestionSelectionUI (existing)
│   ├── LearningSessionPanel (vừa tạo)
│   └── ResultsPanel (vừa tạo)
├── LearningSceneManager (Empty GameObject)
│   └── Components:
│       ├── LearningSceneManager script
│       ├── OpenRouterClient (nếu chưa có)
│       └── AnswerValidationSystem
└── EventSystem
```

### **Manager Component Setup:**
- **Assign references** trong LearningSceneManager:
  - Content Input UI
  - Question Selection UI
  - Learning Session UI
  - Results UI
  - Progress Tracker UI
  - Question Display UI
  - Answer Validation System

---

## 🎨 **6. Styling & Visual Guidelines**

### **Color Scheme:**
```css
Primary: #2196F3 (Blue)
Success: #4CAF50 (Green)
Warning: #FF9800 (Orange)
Error: #F44336 (Red)
Background: #FAFAFA (Light Gray)
Text Primary: #212121 (Dark Gray)
Text Secondary: #757575 (Medium Gray)
```

### **Typography:**
- **Headers:** Bold, 18-24pt
- **Body Text:** Regular, 14-16pt
- **Labels:** Medium, 12-14pt
- **Buttons:** Medium, 14-16pt

### **Spacing:**
- **Padding:** 16px standard, 8px tight
- **Margins:** 16px between sections
- **Component spacing:** 8px between related elements

---

## ⚙️ **7. Script Assignment Checklist**

### **LearningSessionUI:**
- [x] Session Panel reference
- [x] Question Display reference
- [x] Progress Tracker reference
- [x] All button references
- [x] Feedback panel references
- [x] Loading panel references

### **QuestionDisplayUI:**
- [x] Question content references
- [x] All answer area references
- [x] All prefab references
- [x] Background image reference

### **ProgressTrackerUI:**
- [x] Progress slider reference
- [x] All text references
- [x] Timer icon reference
- [x] Milestones parent reference
- [x] Milestone prefab reference

### **ResultsUI:**
- [x] Results panel reference
- [x] All section references
- [x] All prefab references
- [x] Scroll view references
- [x] Button references

### **LearningSceneManager:**
- [x] All UI component references
- [x] OpenRouter client reference
- [x] Answer validation system reference
- [x] Panel GameObject references

---

## 🚀 **8. Testing & Debugging**

### **Test Scenarios:**
1. **Question Navigation:** Previous/Next buttons
2. **Different Question Types:** Multiple choice, fill blank, etc.
3. **Answer Validation:** Correct/incorrect feedback
4. **Progress Tracking:** Real-time updates
5. **Results Display:** Complete session flow
6. **Error Handling:** Network failures, invalid data

### **Debug Tips:**
- Enable **Console logs** trong các scripts
- Test với **different screen resolutions**
- Verify **button interactions**
- Check **text overflow** handling
- Test **performance** với nhiều câu hỏi

---

## 📱 **9. Responsive Design**

### **Canvas Scaler Settings:**
- **UI Scale Mode:** Scale With Screen Size
- **Reference Resolution:** 1920x1080
- **Screen Match Mode:** Match Width Or Height
- **Match:** 0.5

### **Layout Components:**
- Sử dụng **Content Size Fitter** cho dynamic content
- **Layout Groups** cho consistent spacing
- **Anchor presets** cho responsive positioning

---

## ✅ **10. Final Checklist**

- [ ] Tất cả prefabs đã được tạo
- [ ] Scripts đã được assign đúng GameObjects
- [ ] UI references đã được assign trong Inspector
- [ ] Colors và fonts đã được setup
- [ ] Layout groups và anchors đã được cấu hình
- [ ] Canvas settings đã được tối ưu
- [ ] Test basic functionality
- [ ] Performance optimization
- [ ] Documentation cập nhật

---

## 🎯 **Quick Start Command**

Để nhanh chóng tạo basic structure, bạn có thể:

1. **Import TextMeshPro** (nếu chưa có)
2. **Create Canvas** với Camera overlay
3. **Follow hierarchy structure** như đã mô tả
4. **Assign scripts** theo checklist
5. **Test từng component** một cách riêng biệt

---

## 🛠️ **11. Troubleshooting & Common Issues**

### **Compile Errors:**

#### **1. "QuestionText" not found:**
```csharp
// Lỗi: CS1061 - 'BaseQuestion' does not contain 'QuestionText'

// SAI:
string questionText = question.QuestionText;

// ĐÚNG:
string questionText = question.Question;
```

#### **2. Missing concrete question classes:**
```csharp
// Lỗi: MultipleChoiceQuestion, FillInBlankQuestion không tồn tại

// SAI:
MultipleChoiceQuestion mcq = question as MultipleChoiceQuestion;

// ĐÚNG:
BaseQuestion baseQuestion = question;
var options = baseQuestion.GetOptions();
```

#### **3. UniTask.WhenAny usage:**
```csharp
// Lỗi: Cannot apply '==' to tuple and int

// SAI:
var result = await UniTask.WhenAny(task1, task2);
if (result == 0) { ... }

// ĐÚNG:
var (hasResultLeft, result) = await UniTask.WhenAny(task1, task2);
if (hasResultLeft) { ... }
```

### **Runtime Issues:**

#### **1. NullReferenceException trong UI:**
- Kiểm tra tất cả UI references trong Inspector
- Đảm bảo prefabs đã được assign
- Verify GameObject hierarchy đúng cấu trúc

#### **2. AI Validation timeout:**
```csharp
// Tăng timeout nếu cần
answerValidationSystem.SetAIGradingSettings(
    enabled: true,
    timeout: 60f, // Tăng lên 60 giây
    fallback: true
);
```

#### **3. Canvas scaling issues:**
- Check Canvas Scaler component
- Verify Reference Resolution (1920x1080)
- Test trên nhiều resolution khác nhau

### **Performance Issues:**

#### **1. Quá nhiều instantiate/destroy:**
```csharp
// Sử dụng object pooling cho prefabs
// Cache UI elements thay vì tạo mới
```

#### **2. Memory leaks:**
```csharp
// Unsubscribe events trong OnDestroy
Private void OnDestroy()
{
    AnswerValidationSystem.OnAnswerValidated -= HandleAnswerValidated;
    AnswerValidationSystem.OnValidationError -= HandleValidationError;
}
```

### **Integration Issues:**

#### **1. Scene transition problems:**
- Đảm bảo tất cả coroutines được dừng trước khi chuyển scene
- Clean up event subscriptions
- Save/restore UI state nếu cần

#### **2. Data persistence:**
- Sử dụng ScriptableObject cho settings
- Serialize data trước khi destroy

---

## 📞 **Support**

Nếu gặp vấn đề khi setup:

### **Debug Steps:**
1. **Kiểm tra Console errors** - Ư tiên sử lỗi compile
2. **Verify script references** - Đảm bảo tất cả component được assign
3. **Check prefab assignments** - Kiểm tra prefab paths và structure
4. **Test individual components** - Test từng phần riêng biệt
5. **Enable detailed logging** - Bật Debug.Log trong scripts

### **Common Solutions:**
- **Clean & Rebuild** - Xóa Library/Temp folders, rebuild
- **Reimport Assets** - Right-click → Reimport
- **Check Unity version** - Đảm bảo compatibility
- **Update packages** - UniTask, TextMeshPro, etc.

### **Performance Monitoring:**
```csharp
// Sử dụng Unity Profiler
// Monitor memory usage
// Check frame rate drops
// Profile UI rendering
```

**Happy coding! 🚀**
