using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using AISmartRecall.SharedModels.DTOs;

namespace AISmartRecall.API.Utils
{
    /// <summary>
    /// HTTP Client utility sử dụng UniTask để gọi API
    /// </summary>
    public static class HTTPClient
    {
        /// <summary>
        /// Thực hiện GET request
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="headers">HTTP headers (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        public static async UniTask<T> GetAsync<T>(string url, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            APIConfig.LogDebug($"GET Request: {url}");
            
            using (var request = UnityWebRequest.Get(url))
            {
                // Set headers
                SetHeaders(request, headers);
                
                // Set timeout
                request.timeout = APIConfig.REQUEST_TIMEOUT;
                
                // Send request
                var operation = request.SendWebRequest();
                await operation.WithCancellation(cancellationToken);
                
                // Handle response
                return HandleResponse<T>(request);
            }
        }

        /// <summary>
        /// Thực hiện POST request
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="data">Dữ liệu gửi lên</param>
        /// <param name="headers">HTTP headers (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        public static async UniTask<T> PostAsync<T>(string url, object data = null, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            APIConfig.LogDebug($"POST Request: {url}");
            
            string jsonData = "";
            if (data != null)
            {
                // Thử JsonUtility trước
                try
                {
                    jsonData = JsonUtility.ToJson(data);
                    APIConfig.LogDebug($"Serialized JSON: {jsonData}");
                }
                catch (Exception ex)
                {
                    APIConfig.LogError($"JSON Serialization failed: {ex.Message}");
                    // Fallback to empty JSON
                    jsonData = "{}";
                }
            }
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            using (var request = UnityWebRequest.Put(url, bodyRaw))
            {
                request.method = "POST";
                request.SetRequestHeader("Content-Type", APIConfig.ContentTypes.JSON);
                
                // Set headers
                SetHeaders(request, headers);
                
                // Set timeout
                request.timeout = APIConfig.REQUEST_TIMEOUT;
                
                // Send request
                var operation = request.SendWebRequest();
                await operation.WithCancellation(cancellationToken);
                
                // Handle response
                return HandleResponse<T>(request);
            }
        }

        /// <summary>
        /// Thực hiện POST request với raw JSON string
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="jsonData">JSON string</param>
        /// <param name="headers">HTTP headers (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        public static async UniTask<T> PostRawJsonAsync<T>(string url, string jsonData, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            APIConfig.LogDebug($"POST Raw JSON Request: {url}");
            APIConfig.LogDebug($"JSON Data: {jsonData}");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData ?? "");
            
            using (var request = UnityWebRequest.Put(url, bodyRaw))
            {
                request.method = "POST";
                request.SetRequestHeader("Content-Type", APIConfig.ContentTypes.JSON);
                
                // Set headers
                SetHeaders(request, headers);
                
                // Set timeout
                request.timeout = APIConfig.REQUEST_TIMEOUT;
                
                // Send request
                var operation = request.SendWebRequest();
                await operation.WithCancellation(cancellationToken);
                
                // Handle response
                return HandleResponse<T>(request);
            }
        }

        /// <summary>
        /// Thực hiện PUT request
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="data">Dữ liệu gửi lên</param>
        /// <param name="headers">HTTP headers (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        public static async UniTask<T> PutAsync<T>(string url, object data, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            APIConfig.LogDebug($"PUT Request: {url}");
            
            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (var request = UnityWebRequest.Put(url, bodyRaw))
            {
                request.SetRequestHeader("Content-Type", APIConfig.ContentTypes.JSON);
                
                // Set headers
                SetHeaders(request, headers);
                
                // Set timeout
                request.timeout = APIConfig.REQUEST_TIMEOUT;
                
                // Send request
                var operation = request.SendWebRequest();
                await operation.WithCancellation(cancellationToken);
                
                // Handle response
                return HandleResponse<T>(request);
            }
        }

        /// <summary>
        /// Thực hiện DELETE request
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu response</typeparam>
        /// <param name="url">URL endpoint</param>
        /// <param name="headers">HTTP headers (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response data</returns>
        public static async UniTask<T> DeleteAsync<T>(string url, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            APIConfig.LogDebug($"DELETE Request: {url}");
            
            using (var request = UnityWebRequest.Delete(url))
            {
                // Set headers
                SetHeaders(request, headers);
                
                // Set timeout
                request.timeout = APIConfig.REQUEST_TIMEOUT;
                
                // Send request
                var operation = request.SendWebRequest();
                await operation.WithCancellation(cancellationToken);
                
                // Handle response
                return HandleResponse<T>(request);
            }
        }

        /// <summary>
        /// Set HTTP headers cho request
        /// </summary>
        private static void SetHeaders(UnityWebRequest request, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Xử lý response từ server
        /// </summary>
        private static T HandleResponse<T>(UnityWebRequest request)
        {
            // Log response info
            APIConfig.LogDebug($"Response Code: {request.responseCode}");
            APIConfig.LogDebug($"Response: {request.downloadHandler.text}");

            // Kiểm tra lỗi network
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                APIConfig.LogError($"Network Error: {request.error}");
                throw new Exception($"Network Error: {request.error}");
            }

            // Kiểm tra HTTP status code
            if (request.responseCode >= 400)
            {
                string errorMessage = $"HTTP Error {request.responseCode}: {request.downloadHandler.text}";
                APIConfig.LogError(errorMessage);
                throw new Exception(errorMessage);
            }

            // Parse JSON response
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)request.downloadHandler.text;
                }

                if (string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    return default(T);
                }

                string jsonResponse = request.downloadHandler.text;
                
                // Manual parsing cho SharedModels DTOs
                if (typeof(T) == typeof(LoginResponseDTO))
                {
                    var loginResponse = ManualJsonParser.ParseLoginResponse(jsonResponse);
                    return (T)(object)loginResponse;
                }
                else if (typeof(T) == typeof(RegisterResponseDTO))
                {
                    var registerResponse = ManualJsonParser.ParseRegisterResponse(jsonResponse);
                    return (T)(object)registerResponse;
                }
                else if (typeof(T) == typeof(UserProfileDTO))
                {
                    var userProfile = ManualJsonParser.ParseUserProfile(jsonResponse);
                    return (T)(object)userProfile;
                }
                else if (typeof(T) == typeof(AIProviderDTO[]))
                {
                    var aiProviders = ManualJsonParser.ParseAIProviders(jsonResponse);
                    return (T)(object)aiProviders;
                }
                
                // Fallback to Unity JsonUtility cho other types
                return JsonUtility.FromJson<T>(jsonResponse);
            }
            catch (Exception ex)
            {
                APIConfig.LogError($"JSON Parse Error: {ex.Message}");
                throw new Exception($"JSON Parse Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo Authorization header với Bearer token
        /// </summary>
        public static Dictionary<string, string> CreateAuthHeaders(string token)
        {
            return new Dictionary<string, string>
            {
                { APIConfig.Headers.AUTHORIZATION, $"Bearer {token}" }
            };
        }

        /// <summary>
        /// Tạo headers mặc định cho JSON requests
        /// </summary>
        public static Dictionary<string, string> CreateDefaultHeaders()
        {
            return new Dictionary<string, string>
            {
                { APIConfig.Headers.CONTENT_TYPE, APIConfig.ContentTypes.JSON },
                { APIConfig.Headers.ACCEPT, APIConfig.ContentTypes.JSON }
            };
        }
    }
}