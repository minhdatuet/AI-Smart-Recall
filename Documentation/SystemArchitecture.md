# AI Smart Recall - System Architecture

## 🏗️ Architecture Overview

```
┌─────────────────┐    HTTP/REST    ┌─────────────────┐    ┌─────────────────┐
│                 │ ──────────────→ │                 │ ←──│                 │
│   Unity Client  │                 │  C# Web API     │    │    MongoDB      │
│   (Android)     │ ←────────────── │   (.NET Core)   │ ──→│    Database     │
│                 │                 │                 │    │                 │
└─────────────────┘                 └─────────────────┘    └─────────────────┘
                                            │
                                            ▼
                                    ┌─────────────────┐
                                    │   AI Services   │
                                    │  - Qwen API     │
                                    │  - Gemini API   │
                                    │  - ChatGPT API  │
                                    └─────────────────┘
```

## 📱 Frontend (Unity Client)

### Core Components:
1. **Authentication Manager**
2. **Content Input Manager**
3. **Question Generator Interface**
4. **Learning Mode Controller**
5. **Room Manager (Group Learning)**
6. **Progress Tracker**
7. **UI Manager**

### Key Screens:
- Login/Register
- Main Dashboard
- Content Input
- Learning Mode Selection
- Question Practice
- Room Creation/Join
- Statistics View
- Settings (API Keys)

## 🔧 Backend (C# Web API)

### Controllers:
1. **AuthController** - User authentication
2. **ContentController** - Content management
3. **QuestionController** - Question generation
4. **RoomController** - Group learning rooms
5. **ProgressController** - Learning statistics
6. **AIController** - AI service integration

### Services:
1. **UserService**
2. **ContentService**
3. **AIQuestionService**
4. **RoomService**
5. **ProgressService**

### AI Integration Layer:
- **AIProviderFactory** - Factory pattern for different AI services
- **QwenService**
- **GeminiService**
- **ChatGPTService**

## 🗄️ Database Schema (MongoDB)

### Collections:

#### Users
```json
{
  "_id": ObjectId,
  "username": string,
  "email": string,
  "passwordHash": string,
  "profile": {
    "displayName": string,
    "avatar": string,
    "level": number
  },
  "apiKeys": {
    "qwen": string (encrypted),
    "gemini": string (encrypted),
    "chatgpt": string (encrypted)
  },
  "preferences": {
    "defaultAI": string,
    "learningMode": string
  },
  "createdAt": DateTime,
  "lastActive": DateTime
}
```

#### Contents
```json
{
  "_id": ObjectId,
  "userId": ObjectId,
  "title": string,
  "content": string,
  "contentType": string, // "memorization" | "understanding"
  "tags": [string],
  "createdAt": DateTime,
  "updatedAt": DateTime
}
```

#### Questions
```json
{
  "_id": ObjectId,
  "contentId": ObjectId,
  "type": string, // "fill_blank", "multiple_choice", "flashcard", etc.
  "question": string,
  "options": [string], // for multiple choice
  "correctAnswer": string,
  "explanation": string,
  "difficulty": number,
  "aiProvider": string,
  "createdAt": DateTime
}
```

#### Rooms
```json
{
  "_id": ObjectId,
  "code": string,
  "hostId": ObjectId,
  "name": string,
  "contentId": ObjectId,
  "participants": [ObjectId],
  "settings": {
    "maxParticipants": number,
    "timeLimit": number,
    "questionCount": number
  },
  "status": string, // "waiting", "active", "completed"
  "createdAt": DateTime
}
```

#### Learning Sessions
```json
{
  "_id": ObjectId,
  "userId": ObjectId,
  "contentId": ObjectId,
  "roomId": ObjectId, // null for solo sessions
  "questions": [{
    "questionId": ObjectId,
    "userAnswer": string,
    "isCorrect": boolean,
    "timeSpent": number,
    "attempts": number
  }],
  "totalScore": number,
  "totalTime": number,
  "completedAt": DateTime
}
```

## 🔌 API Endpoints

### Authentication
- POST `/api/auth/register`
- POST `/api/auth/login`
- POST `/api/auth/refresh`
- PUT `/api/auth/profile`

### Content Management
- GET `/api/content/my-contents`
- POST `/api/content/create`
- PUT `/api/content/{id}`
- DELETE `/api/content/{id}`

### Question Generation
- POST `/api/questions/generate`
- GET `/api/questions/by-content/{contentId}`
- POST `/api/questions/custom`

### Room Management
- POST `/api/rooms/create`
- GET `/api/rooms/{code}`
- POST `/api/rooms/{code}/join`
- POST `/api/rooms/{code}/start`
- GET `/api/rooms/{code}/status`

### Progress Tracking
- GET `/api/progress/stats`
- GET `/api/progress/history`
- POST `/api/progress/session`

## 🤖 AI Integration Strategy

### Multi-Provider Support:
1. **Factory Pattern**: AIProviderFactory creates appropriate service
2. **Configuration**: User selects preferred AI and provides API key
3. **Fallback**: If primary AI fails, try secondary options
4. **Prompt Engineering**: Different prompts for different question types

### Question Types by Learning Mode:

#### Memorization Mode:
- Fill in the blank
- Missing word multiple choice
- Flashcard
- Exact typing
- Content-based multiple choice

#### Understanding Mode:
- Content-based multiple choice
- True/False
- Match concepts
- Short answer

## 🔐 Security Considerations

1. **API Key Encryption**: User API keys encrypted in database
2. **JWT Authentication**: Secure user sessions
3. **Input Validation**: Sanitize all user inputs
4. **Rate Limiting**: Prevent API abuse
5. **HTTPS Only**: All communications encrypted

## 📱 Mobile Optimization

1. **Offline Mode**: Cache questions for offline practice
2. **Progressive Loading**: Load content as needed
3. **Network Optimization**: Minimize API calls
4. **Battery Optimization**: Efficient resource usage

## 🚀 Development Phases

### Phase 1 (Month 1): Core Backend + Basic Unity UI
### Phase 2 (Month 2): AI Integration + Advanced Features
### Phase 3 (Month 3): Group Learning + Polish + Testing
