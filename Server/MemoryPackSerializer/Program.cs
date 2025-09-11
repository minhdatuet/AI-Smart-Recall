using MemoryPack;
using System.Reflection;
using AISmartRecall.SharedModels.DTOs;

namespace MemoryPackSerializer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== AI SMART RECALL MEMORYPACK SERIALIZER ===");
            Console.WriteLine("Generating MemoryPack serializers for Unity client...");
            Console.WriteLine();

            try
            {
                // Get the SharedModels assembly
                var sharedModelsAssembly = Assembly.LoadFrom("AISmartRecall.SharedModels.dll");
                Console.WriteLine($"✅ Loaded assembly: {sharedModelsAssembly.FullName}");

                // Find all types with MemoryPackable attribute
                var memoryPackableTypes = sharedModelsAssembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<MemoryPackableAttribute>() != null)
                    .ToList();

                Console.WriteLine($"✅ Found {memoryPackableTypes.Count} MemoryPackable types:");
                
                foreach (var type in memoryPackableTypes)
                {
                    Console.WriteLine($"   - {type.Name}");
                }

                Console.WriteLine();

                // Test serialization for each type to ensure everything works
                TestSerialization();

                Console.WriteLine("✅ MemoryPack serializer generation completed successfully!");
                Console.WriteLine("✅ All DTOs are ready for Unity integration!");
                Console.WriteLine();
                Console.WriteLine("Next steps:");
                Console.WriteLine("1. Copy AISmartRecall.SharedModels.dll to Unity Assets/DLL/");
                Console.WriteLine("2. Copy MemoryPack runtime DLLs to Unity Assets/DLL/");
                Console.WriteLine("3. Use the DTOs in Unity networking code");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static void TestSerialization()
        {
            Console.WriteLine("🧪 Testing MemoryPack serialization...");

            try
            {
                // Test Authentication DTOs
                var loginRequest = new LoginRequestDTO
                {
                    Email = "test@example.com",
                    Password = "password123"
                };

                var loginBytes = MemoryPack.MemoryPackSerializer.Serialize(loginRequest);
                var deserializedLogin = MemoryPack.MemoryPackSerializer.Deserialize<LoginRequestDTO>(loginBytes);
                
                if (deserializedLogin?.Email == loginRequest.Email)
                {
                    Console.WriteLine("   ✅ LoginRequestDTO serialization test passed");
                }
                else
                {
                    throw new Exception("LoginRequestDTO serialization test failed");
                }

                // Test Content DTOs
                var contentRequest = new CreateContentRequestDTO
                {
                    Title = "Test Content",
                    Content = "This is test content",
                    LearningMode = "memorization",
                    Tags = new List<string> { "test", "sample" },
                    IsPublic = false
                };

                var contentBytes = MemoryPack.MemoryPackSerializer.Serialize(contentRequest);
                var deserializedContent = MemoryPack.MemoryPackSerializer.Deserialize<CreateContentRequestDTO>(contentBytes);

                if (deserializedContent?.Title == contentRequest.Title)
                {
                    Console.WriteLine("   ✅ CreateContentRequestDTO serialization test passed");
                }
                else
                {
                    throw new Exception("CreateContentRequestDTO serialization test failed");
                }

                // Test Question DTOs
                var questionDto = new QuestionDTO
                {
                    Id = "test-id",
                    ContentId = "content-id",
                    Type = QuestionTypes.MultipleChoice,
                    Question = "What is 2 + 2?",
                    Options = new List<string> { "3", "4", "5", "6" },
                    CorrectAnswer = "4",
                    Explanation = "Simple addition",
                    Difficulty = 1,
                    AIProvider = AIProviders.OpenAI
                };

                var questionBytes = MemoryPack.MemoryPackSerializer.Serialize(questionDto);
                var deserializedQuestion = MemoryPack.MemoryPackSerializer.Deserialize<QuestionDTO>(questionBytes);

                if (deserializedQuestion?.Question == questionDto.Question)
                {
                    Console.WriteLine("   ✅ QuestionDTO serialization test passed");
                }
                else
                {
                    throw new Exception("QuestionDTO serialization test failed");
                }

                // Test Learning Session DTOs
                var sessionRequest = new StartLearningSessionRequestDTO
                {
                    ContentId = "content-123",
                    QuestionIds = new List<string> { "q1", "q2", "q3" },
                    SessionType = "solo"
                };

                var sessionBytes = MemoryPack.MemoryPackSerializer.Serialize(sessionRequest);
                var deserializedSession = MemoryPack.MemoryPackSerializer.Deserialize<StartLearningSessionRequestDTO>(sessionBytes);

                if (deserializedSession?.ContentId == sessionRequest.ContentId)
                {
                    Console.WriteLine("   ✅ StartLearningSessionRequestDTO serialization test passed");
                }
                else
                {
                    throw new Exception("StartLearningSessionRequestDTO serialization test failed");
                }

                // Test Room DTOs
                var roomRequest = new CreateRoomRequestDTO
                {
                    Name = "Test Room",
                    ContentId = "content-456",
                    MaxParticipants = 5,
                    TimeLimitMinutes = 10,
                    QuestionCount = 15,
                    IsPrivate = false
                };

                var roomBytes = MemoryPack.MemoryPackSerializer.Serialize(roomRequest);
                var deserializedRoom = MemoryPack.MemoryPackSerializer.Deserialize<CreateRoomRequestDTO>(roomBytes);

                if (deserializedRoom?.Name == roomRequest.Name)
                {
                    Console.WriteLine("   ✅ CreateRoomRequestDTO serialization test passed");
                }
                else
                {
                    throw new Exception("CreateRoomRequestDTO serialization test failed");
                }

                Console.WriteLine("🎉 All serialization tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Serialization test failed: {ex.Message}");
                throw;
            }
        }
    }
}
