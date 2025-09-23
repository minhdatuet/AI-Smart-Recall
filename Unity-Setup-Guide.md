# AI Smart Recall - Unity UI Setup Guide

## 🎨 SETUP LEARNING SCENE UI

### BƯỚC 1: TẠO CANVAS VÀ CẤU TRÚC CƠ BẢN

#### 1.1 Canvas Setup
1. **Right-click Hierarchy** → UI → Canvas
2. **Canvas Settings:**
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: Match Width Or Height (0.5)

#### 1.2 Tạo Main Panels
**Tạo 4 panels chính:**

1. **ContentInputPanel**
```
Right-click Canvas → UI → Panel
- Name: ContentInputPanel
- Anchor: Full Stretch (Alt+Shift+Click anchor preset)
- Background Color: #F5F5F5 (Light Gray)
```

2. **QuestionSelectionPanel** 
```
Right-click Canvas → UI → Panel  
- Name: QuestionSelectionPanel
- Anchor: Full Stretch
- Background Color: #FFFFFF (White)
- Active: False (sẽ được LearningSceneManager control)
```

3. **LearningSessionPanel**
```
Right-click Canvas → UI → Panel
- Name: LearningSessionPanel  
- Anchor: Full Stretch
- Background Color: #F8F9FA (Very Light Gray)
- Active: False
```

4. **ResultsPanel**
```
Right-click Canvas → UI → Panel
- Name: ResultsPanel
- Anchor: Full Stretch  
- Background Color: #E8F5E8 (Light Green)
- Active: False
```

### BƯỚC 2: CONTENT INPUT PANEL UI

#### Layout Structure:
```
ContentInputPanel
├─ Header
│  ├─ Title Text ("Nhập nội dung học tập")
│  └─ Back Button
├─ Content Form (Vertical Layout Group)
│  ├─ Title Section
│  │  ├─ Label ("Tiêu đề:")
│  │  └─ Title InputField
│  ├─ Content Type Section  
│  │  ├─ Label ("Chế độ học:")
│  │  └─ Content Type Toggle Group
│  │     ├─ Memorization Toggle ("🧠 Học thuộc")
│  │     └─ Understanding Toggle ("💡 Học hiểu")
│  ├─ Content Section
│  │  ├─ Label ("Nội dung:")
│  │  └─ Content InputField (Multi-line)
│  └─ Stats Text ("0 từ • ~0 phút đọc")
└─ Footer
   ├─ Clear Button
   └─ Next Button ("Tiếp tục")
```

#### Chi tiết setup từng component:

**2.1 Header:**
```
- Right-click ContentInputPanel → UI → Panel (name: Header)
- Height: 80px, Width: Full stretch
- Background: #2196F3 (Blue)

Title Text:
- Right-click Header → UI → Text - TextMeshPro
- Text: "Nhập nội dung học tập"
- Font Size: 24, Color: White, Alignment: Center

Back Button:
- Right-click Header → UI → Button - TextMeshPro  
- Size: 60x60, Position: Left
- Text: "←", Font Size: 20
```

**2.2 Content Form:**
```
- Right-click ContentInputPanel → UI → Panel (name: ContentForm)
- Add Vertical Layout Group component
- Padding: 20 on all sides
- Spacing: 15
- Child Alignment: Upper Left
```

**2.3 Title Section:**
```
- Right-click ContentForm → Create Empty (name: TitleSection)
- Add Horizontal Layout Group
- Child Force Expand Width: true

Label:
- Right-click TitleSection → UI → Text - TextMeshPro
- Text: "Tiêu đề:", Width: 150
- Font Size: 16, Color: #333333

InputField:
- Right-click TitleSection → UI → InputField - TextMeshPro
- Placeholder: "Nhập tiêu đề bài học..."
- Flexible Width: 1
```

**2.4 Content Type Section:**
```
- Create Empty (name: ContentTypeSection)  
- Add Horizontal Layout Group

Label:
- Text: "Chế độ học:", Width: 150

Toggle Group:
- Right-click → UI → Toggle Group
- Add 2 Toggles:
  
Memorization Toggle:
- Text: "🧠 Học thuộc"
- Background Color: #FF9800 (Orange)

Understanding Toggle:
- Text: "💡 Học hiểu" 
- Background Color: #2196F3 (Blue)
- Is On: true (default)
```

**2.5 Content Section:**
```
Label:
- Text: "Nội dung:", Width: 150

InputField:
- Content Type: Custom
- Input Type: Standard
- Line Type: Multi Line Submit
- Character Limit: 5000
- Height: 300px
- Placeholder: "Dán hoặc nhập nội dung cần học..."
- Scroll Rect: Enable
```

### BƯỚC 3: QUESTION SELECTION PANEL UI

#### Layout Structure:
```
QuestionSelectionPanel
├─ Header
│  ├─ Title ("Chọn loại câu hỏi")
│  ├─ Back Button
│  └─ Content Info Panel
├─ Question Types Container (Scroll View)
│  └─ Content (Vertical Layout Group)
│     └─ [Question Type Items sẽ được tạo runtime]
├─ Summary Panel
│  ├─ Total Questions Text
│  └─ Generate Button
└─ Loading Panel (Overlay)
   ├─ Background Blur
   ├─ Loading Text
   └─ Progress Bar
```

#### Chi tiết setup:

**3.1 Header Setup:**
```
- Panel với height 120px
- Background: Gradient (Blue to Light Blue)

Content Info Panel:
- Show content title, summary, stats
- Background: Semi-transparent white
- Layout: Horizontal với icon và text
```

**3.2 Question Types Scroll View:**
```
Right-click QuestionSelectionPanel → UI → Scroll View
- Name: QuestionTypesScrollView
- Remove Horizontal Scrollbar
- Vertical Scrollbar: Auto Hide
- Movement Type: Clamped

Content Setup:
- Add Vertical Layout Group
- Spacing: 10
- Padding: 10 on all sides
- Child Force Expand Width: true
```

**3.3 Summary Panel:**
```
- Panel ở bottom với height 80px
- Background: #F5F5F5

Total Questions Text:
- "Tổng cộng: 0 câu hỏi"
- Font Size: 18, Color: Gray → Green khi có selection

Generate Button:
- Size: 200x50, Center position
- Text: "Chọn ít nhất 1 loại câu hỏi"  
- Background: Blue khi active, Gray khi disabled
```

### BƯỚC 4: QUESTION TYPE ITEM PREFAB

Tạo prefab cho từng item chọn loại câu hỏi:

#### Structure:
```
QuestionTypeItem (Panel - 400x120)
├─ Icon Text (50x50) - "📝"
├─ Info Panel (Flexible width)
│  ├─ Type Name Text - "Điền chỗ trống"  
│  └─ Description Text - "Câu hỏi có chỗ trống..."
├─ Count Controls (150 width)
│  ├─ Decrease Button ("-")
│  ├─ Count Text ("0")
│  ├─ Increase Button ("+")  
│  └─ Count Slider (0-10)
```

#### Setup Details:

**4.1 Main Panel:**
```
- Right-click Project → Create → UI → Panel
- Name: QuestionTypeItemPrefab
- Size: 400x120
- Background: White với border #E0E0E0
- Add Horizontal Layout Group
- Padding: 15, Spacing: 10
```

**4.2 Icon:**
```
- Text - TextMeshPro component
- Size: 50x50
- Text: "📝" (sẽ được set runtime)
- Font Size: 30, Alignment: Center
```

**4.3 Info Panel:**
```
- Add Vertical Layout Group
- Flexible Width: 1

Type Name Text:
- Font Size: 16, Bold, Color: #333
- Text: "Tên loại câu hỏi"

Description Text:
- Font Size: 12, Color: #666  
- Text: "Mô tả chi tiết..."
- Word Wrap: true
```

**4.4 Count Controls:**
```
- Panel với Horizontal Layout Group
- Width: 150

Decrease Button:
- Size: 30x30, Text: "−"
- Background: #F44336 (Red)

Count Text:
- Size: 40x30, Text: "0"
- Font Size: 16, Alignment: Center

Increase Button:  
- Size: 30x30, Text: "+"
- Background: #4CAF50 (Green)

Count Slider:
- Width: 100, Height: 20
- Min: 0, Max: 10, Whole Numbers: true
- Fill Color: Blue
```

**4.5 Add QuestionTypeSelectionItem Script:**
```
- Add script component: QuestionTypeSelectionItem
- Script path: Assets/Scripts/UI/Learning/QuestionTypeSelectionItem.cs
- Assign all UI references trong inspector:
  • Icon Text: Drag Text component chứa emoji
  • Type Name Text: Drag Text hiển thị tên loại câu hỏi
  • Description Text: Drag Text mô tả
  • Decrease Button: Drag button "-"
  • Count Text: Drag Text hiển thị số
  • Increase Button: Drag button "+"
  • Count Slider: Drag Slider component
  • Background Image: Drag Image component của panel chính
```

**Script Features:**
- ✅ Tự động cập nhật icon, tên và mô tả từ QuestionType
- ✅ Điều khiển số lượng qua buttons và slider (0-10)
- ✅ Visual feedback với màu sắc thay đổi
- ✅ Events cho QuestionSelectionUI lắng nghe
- ✅ Validation và error handling
- ✅ Enable/disable states
- ✅ Auto-assign references trong OnValidate()

### BƯỚC 5: QUESTION SELECTION UI SCRIPT

QuestionSelectionUI script sẽ sử dụng QuestionTypeSelectionItem để:
```
- Tạo items cho từng QuestionType
- Lắng nghe thay đổi count từ các items
- Cập nhật tổng số câu hỏi
- Enable/disable Generate button
- Tạo requests list cho LearningSceneManager
```

### BƯỚC 6: LEARNING SCENE MANAGER SETUP

#### 6.1 Tạo GameObject:
```
- Right-click Hierarchy → Create Empty
- Name: LearningSceneManager
- Position: (0,0,0)
- Add Script: LearningSceneManager
```

#### 6.2 Assign References:
```
            Scene References:
            - Content Input UI: Drag ContentInputPanel với ContentInputUI script
            - Question Selection UI: Drag QuestionSelectionPanel với QuestionSelectionUI script
            - Learning Session Panel: Drag LearningSessionPanel
            - Results Panel: Drag ResultsPanel

Components sẽ được tìm tự động:
- OpenRouterClient sẽ được tạo runtime
```

### BƯỚC 7: RESPONSIVE DESIGN

#### 7.1 Screen Safe Areas:
```
- Tất cả panels sử dụng Anchor Stretch full
- Padding 20px cho mobile safe areas
- Minimum font size 14px để đọc được
```

#### 7.2 Layout Groups Settings:
```
- Vertical Layout Group: Control Child Size Height = true
- Horizontal Layout Group: Child Force Expand Width = true  
- Content Size Fitter: Vertical Fit = Preferred Size khi cần
```

### BƯỚC 8: TESTING CHECKLIST

#### 8.1 Scene Hierarchy Check:
- [ ] Canvas có Canvas Scaler setup đúng
- [ ] 4 panels chính đã tạo với đúng tên
- [ ] Event System có trong scene  
- [ ] LearningSceneManager GameObject tồn tại

#### 8.2 UI Component Check:
- [ ] Tất cả InputFields có Placeholder text
- [ ] Buttons có proper colors (active/disabled states)
- [ ] Text components sử dụng TextMeshPro
- [ ] Layout Groups có proper spacing và padding

#### 8.3 Script References Check:
- [ ] LearningSceneManager có tất cả panel references
- [ ] QuestionSelectionUI script attached vào đúng panel  
- [ ] QuestionTypeItemPrefab có script và references assigned

### BƯỚC 9: BUILD SETTINGS

#### 9.1 Add Scene:
```
- File → Build Settings
- Add Open Scenes 
- Đảm bảo LearningScene trong build index
```

#### 9.2 Player Settings:
```
- Default Orientation: Auto Rotation
- Minimum API Level: 24 (Android 7.0)
- Target Architectures: ARM64
```

## 🎯 NEXT STEPS AFTER UI SETUP

1. **Test Navigation:** Scene transitions giữa panels
2. **API Integration:** Nhập OpenRouter API key để test
3. **Content Input:** Test nhập content và validation  
4. **Question Generation:** Test tạo câu hỏi với AI
5. **Learning Flow:** Complete user journey end-to-end

## 📱 MOBILE OPTIMIZATION TIPS

- **Touch Targets:** Minimum 44px cho buttons
- **Text Readability:** Minimum 14px font size
- **Scroll Performance:** Sử dụng Rect Mask 2D thay Mask
- **Memory:** Pool UI elements thay vì Instantiate/Destroy
- **Battery:** Limit animation và effects
