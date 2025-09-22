using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Dat.Json;
using CompressionLevel = System.IO.Compression.CompressionLevel;

public class UniTaskWebRequest
{
    private static SimpleJsonInstance simpleJson = new SimpleJsonInstance();
    private static JsonSerializer serializer = new JsonSerializer();

    private static readonly bool ShowDebug = true;

    private static HttpClient _httpClient;

    private static HttpClient httpClient
    {
        get
        {
            if (_httpClient == null)
            {
                //string certPath = "path/to/your/certificate.crt"; // Đường dẫn tới file chứng chỉ của bạn
                //var handler = new CustomCertificateHandler(certPath);
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (
                        sender,
                        cert,
                        chain,
                        sslPolicyErrors
                    ) => true,
                }; // Bỏ qua lỗi server không được tin tưởng bởi unity do chứng chỉ SSL (sử dụng khi dev)
                _httpClient = new(handler);
                System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate (
                    object sender,
                    System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                    System.Security.Cryptography.X509Certificates.X509Chain chain,
                    System.Net.Security.SslPolicyErrors sslPolicyErrors
                )
                {
                    return true; // **** Always accept
                };
                _httpClient.Timeout = TimeSpan.FromSeconds(8);
            }
            return _httpClient;
        }
    }
    public static async UniTask<(Res response, long statusCode)> GetAsync<Res>(
        string url,
        string author = null,
        string accessToken = null,
        int timeout = 8
    )
    {
        if (ShowDebug)
        {
            Debug.Log($"UniTask GetAPI: {url}");
        }

        try
        {
            await UniTask.SwitchToThreadPool();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            ConfigureAuthorizationHeader(author, accessToken);

            var startTime = DateTime.UtcNow;

            using (Stream responseStream = await httpClient.GetStreamAsync(url))
            using (StreamReader reader = new StreamReader(responseStream))
            using (JsonReader jsonReader = new JsonTextReader(reader))
            {
                Res result = default;

                try
                {
                    result = serializer.Deserialize<Res>(jsonReader);

                    if (ShowDebug)
                    {
                        Debug.Log(
                            $"UniTask Get Done at: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms, "
                                + $"url = {url}, response = {result}, Status Code: {(result != null ? 200 : 500)}"
                        );
                    }
                }
                catch (Exception jsonEx)
                {
                    if (ShowDebug)
                    {
                        Debug.LogError($"GetAsync. Deserialization Error: {url} {jsonEx}");
                    }
                }

                await UniTask.Delay(100);
                await UniTask.SwitchToMainThread();

                return (result, result != null ? 200 : 500);
            }
        }
        catch (Exception ex)
        {
            await UniTask.SwitchToMainThread();

            if (ShowDebug)
            {
                Debug.LogError($"GetAsyncError: {url} {ex}");
            }

            return (default, 0); // 0 indicates an exception occurred
        }
    }

    public static async UniTask<(Res response, long statusCode)> PostByteAsync<Res>(
        string url,
        string author,
        string accessToken,
        byte[] requestData = null,
        int timeout = 8
    )
    {
        if (ShowDebug)
        {
            Debug.Log($"UniTask PostAsync: {url}");
        }

        try
        {
            await UniTask.SwitchToThreadPool();

            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            if (requestData == null || requestData.Length <= 0) requestData = new byte[] { 0 };

            var startTime = DateTime.UtcNow;
            var postData = new ByteArrayContent(requestData)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
            };

            ConfigureAuthorizationHeader(author, accessToken);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = postData };
            var response = await httpClient.SendAsync(httpRequest);

            var statusCode = (long)response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();

            if (ShowDebug)
            {
                Debug.Log(
                    $"UniTask Get Done at: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms, url = {url}, response = {responseContent}, Status Code: {statusCode}"
                );
            }

            if (string.IsNullOrEmpty(responseContent))
            {
                await UniTask.SwitchToMainThread();
                return (default, statusCode);
            }

            var result = DeserializeResponse<Res>(response, responseContent, url);

            await UniTask.Delay(100);
            await UniTask.SwitchToMainThread();

            return (result, statusCode);
        }
        catch (Exception ex)
        {
            if (ShowDebug)
            {
                Debug.LogError($"PostByteAsync Error: {url} {ex}");
            }

            await UniTask.SwitchToMainThread();
            return (default, 0); // Indicate an exception with status code 0
        }
    }

    private static T DeserializeResponse<T>(
        HttpResponseMessage response,
        string content,
        string url
    )
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if (ShowDebug)
            {
                Debug.LogError($"PostAsync Error: {url} {response.StatusCode} {content}");
            }
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception ex)
        {
            if (ShowDebug)
            {
                Debug.LogError($"PostByteAsync. Deserialization Error: {url} {ex}");
            }
            return default;
        }
    }
    public static async UniTask<(Res response, long statusCode)> PostAsync<Res>(
        string url,
        string author,
        string accessToken,
        object requestData = null,
        int timeout = 8,
        bool forTool = false,
        IProgress<float> progress = null
    )
    {
        try
        {
            await UniTask.SwitchToThreadPool();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            var startTime = DateTime.UtcNow;

            // Serialize request data
            string requestDataJson = forTool
                ? JsonConvert.SerializeObject(requestData)
                : simpleJson.SerializeObject(requestData);
            requestDataJson ??= @"""""";

            if (ShowDebug)
            {
                Debug.Log($"UniTask PostAsync: {url} requestDataJson = {requestDataJson}");
            }

            // Prepare request content
            var postData = new StringContent(requestDataJson, Encoding.UTF8, "application/json");

            // Configure authorization header
            ConfigureAuthorizationHeader(author, accessToken);

            // Create and send request
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = postData };
            var response = await httpClient.SendAsync(request);

            // Handle progress reporting
            if (progress != null)
            {
                await ReportProgressAsync(response, progress);
            }

            var statusCode = response.StatusCode.GetHashCode();

            // Read response content
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                await UniTask.SwitchToMainThread();
                return (default, statusCode);
            }

            if (ShowDebug)
            {
                Debug.Log(
                    $"UniTask Get Done at: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms, url = {url}, response = {content}, Status Code: {statusCode}"
                );
            }

            // Deserialize response
            var result = DeserializeResponse<Res>(content, response.StatusCode);

            await UniTask.SwitchToMainThread();
            return (result, statusCode);
        }
        catch (Exception e)
        {
            if (ShowDebug)
            {
                Debug.LogError($"PostAsyncError: {url} {e}");
            }

            await UniTask.SwitchToMainThread();
            return (default, 0);
        }
    }

    /// <summary>
    /// HTTP PUT method cho API calls
    /// </summary>
    public static async UniTask<(Res response, long statusCode)> PutAsync<Res>(
        string url,
        string author,
        string accessToken,
        object requestData = null,
        int timeout = 8,
        bool forTool = false,
        IProgress<float> progress = null
    )
    {
        try
        {
            await UniTask.SwitchToThreadPool();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            var startTime = DateTime.UtcNow;

            // Serialize request data
            string requestDataJson = forTool
                ? JsonConvert.SerializeObject(requestData)
                : simpleJson.SerializeObject(requestData);
            requestDataJson ??= @"""""";

            if (ShowDebug)
            {
                Debug.Log($"UniTask PutAsync: {url} requestDataJson = {requestDataJson}");
            }

            // Prepare request content
            var putData = new StringContent(requestDataJson, Encoding.UTF8, "application/json");

            // Configure authorization header
            ConfigureAuthorizationHeader(author, accessToken);

            // Create and send request with PUT method
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = putData };
            var response = await httpClient.SendAsync(request);

            // Handle progress reporting
            if (progress != null)
            {
                await ReportProgressAsync(response, progress);
            }

            var statusCode = response.StatusCode.GetHashCode();

            // Read response content
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                await UniTask.SwitchToMainThread();
                return (default, statusCode);
            }

            if (ShowDebug)
            {
                Debug.Log(
                    $"UniTask PutAsync Done at: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms, url = {url}, response = {content}, Status Code: {statusCode}"
                );
            }

            // Deserialize response
            var result = DeserializeResponse<Res>(content, response.StatusCode);

            await UniTask.SwitchToMainThread();
            return (result, statusCode);
        }
        catch (Exception e)
        {
            if (ShowDebug)
            {
                Debug.LogError($"PutAsyncError: {url} {e}");
            }

            await UniTask.SwitchToMainThread();
            return (default, 0);
        }
    }

    private static void ConfigureAuthorizationHeader(string author, string accessToken)
    {
       // Debug.LogError(accessToken);
        if (!string.IsNullOrEmpty(author) && !string.IsNullOrEmpty(accessToken))
        {
            if (
                httpClient.DefaultRequestHeaders.Authorization == null
                || httpClient.DefaultRequestHeaders.Authorization.Parameter != accessToken
            )
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );
            }
        }
    }

    private static async UniTask ReportProgressAsync(
        HttpResponseMessage response,
        IProgress<float> progress
    )
    {
        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        float totalRead = 0;
        int bytesRead;

        long assumedTotalSize = 3 * 1024 * 1024; // 3 MB default assumption
        var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(assumedTotalSize);

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += bytesRead;
            progress.Report(totalRead / totalBytes);
        }

        progress.Report(1.0f);
    }

    private static Res DeserializeResponse<Res>(string content, HttpStatusCode statusCode)
    {
        try
        {
            if (statusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.Accepted)
            {
                return JsonConvert.DeserializeObject<Res>(content);
            }

            if (ShowDebug)
            {
                Debug.LogError($"PostAsync Error: StatusCode = {statusCode}, Content = {content}");
            }
        }
        catch (Exception jsonEx)
        {
            if (ShowDebug)
            {
                Debug.LogError($"Deserialization Error: {jsonEx}");
            }
        }

        return default;
    }

    public static byte[] Decompress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        {
            using (var outputStream = new MemoryStream())
            {
                using (
                    var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress)
                )
                {
                    decompressStream.CopyTo(outputStream);
                }

                return outputStream.ToArray();
            }
        }
    }

    public static byte[] Compress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }

            return memoryStream.ToArray();
        }
    }

    public static NameValueCollection ParseQueryString(string query)
    {
        if (query.Length == 0)
            return null;
        var result = new NameValueCollection();
        var split1 = query.Split('?');
        if (split1.Length > 1 && string.IsNullOrEmpty(split1[1]) == false)
        {
            var split2 = split1[1].Split('&');
            for (var i = 0; i < split2.Length; i++)
                if (string.IsNullOrEmpty(split2[i]) == false)
                {
                    var split3 = split2[i].Split('=');
                    result.Add(split3[0], split3[1]);
                }
        }

        return result;
    }

    private static string _areaCode;

    // các quốc gia chặn google
    private static Dictionary<string, string> alternativeUrls = new()
    {
        { "CN", "https://www.baidu.com" }, // Trung Quốc
        { "KP", "http://www.kcckp.net" }, // Triều Tiên
        { "CU", "https://www.ecured.cu" }, // Cuba
        { "IR", "https://www.aparat.com" }, // Iran
        { "SY", "http://www.syrianews.com" }, // Syria
        { "RU", "https://www.yandex.ru" }, // Nga
        { "VE", "https://www.eluniversal.com" }, // Venezuela
    };

    // private static async UniTask<string> GetUserCountry()
    // {
    //     try
    //     {
    //         // web check khu vực bị giới hạn request trong 1 ngày nên lưu vào playerPrefs (có thể gây lỗi nếu user bay qua nước khác)
    //         if (string.IsNullOrWhiteSpace(UserData.CountryCode))
    //         {
    //             string response = await httpClient.GetStringAsync("https://ipinfo.io/country");
    //             UserData.CountryCode = response.Trim();
    //         }
    //         return UserData.CountryCode;
    //
    //         //string           response   = await httpClient.GetStringAsync("https://ipinfo.io/country");
    //         //return response.Trim();
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogError(e);
    //         return "";
    //     }
    // }
    // public static async UniTask GetRegionFromWebAsync(string url)
    // {
    //     try 
    //     {
    //         string savedCountry = PlayerPrefs.GetString(UserData.CountryPlayerPrefs);
    //         if (!string.IsNullOrWhiteSpace(savedCountry))
    //         {
    //             UserData.CountryCode = savedCountry;
    //             return;
    //         }
    //         using var webRequest = UnityWebRequest.Get(url);
    //         var       operation  = await webRequest.SendWebRequest();
    //         if (operation.result != UnityWebRequest.Result.Success)
    //         {
    //             Debug.LogError($"Web request failed: {operation.error}");
    //             return;
    //         }
    //         string countryCode = operation.downloadHandler.text
    //                                       .Trim()              
    //                                       .ToUpperInvariant();
    //
    //         if (!string.IsNullOrWhiteSpace(countryCode) && countryCode.Length <= 3)
    //         {
    //             UserData.CountryCode = countryCode;
    //             PlayerPrefs.SetString(UserData.CountryPlayerPrefs, countryCode);
    //             PlayerPrefs.Save();
    //         }
    //
    //         Debug.Log($"Country code retrieved: {UserData.CountryCode}");
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.LogError($"Error retrieving country code: {ex.Message}");
    //         UserData.CountryCode = string.Empty;
    //     }
    // }
    private static string GetUrlCheck()
    {
        // if (_areaCode.IsNullOrEmpty())
        //     _areaCode = UserData.CountryCode; 
        // if (alternativeUrls.TryGetValue(_areaCode, out string url))
        // {
        //     return url;
        // }

        return "https://www.google.com";
    }

    /// <summary>
    ///  Kiểm tra xem có kết nối được tới server game không
    /// không được dùng để check trong update
    /// </summary>
    public static async UniTask<bool> IsConnectToServer(string serverURL,int timeOutMs = 4000)
    {
        try
        {
#if UNITY_EDITOR
            // if (!ConfigData.IsConnectInternet)
            //     return false;
#else
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;
#endif
            return true;

            await UniTask.SwitchToThreadPool();
            string url = GetUrlCheck(); //serverURL + ApiList.CheckConnectToServer;

            using HttpRequestMessage requestMessage = new(HttpMethod.Head, url);
            httpClient.Timeout = TimeSpan.FromMilliseconds(timeOutMs);

            using HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
            bool isConnected = response.IsSuccessStatusCode;

            await UniTask.SwitchToMainThread();

            if (isConnected)
                Debug.Log($"[Check connect to server] Successfully connected to server: {url}");
            else
                Debug.LogError(
                    $"[NAB][Check connect to server] Failed to connect to server: {url} - with status code: {response.StatusCode}"
                );

            return isConnected;
        }
        catch (TaskCanceledException)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogWarning(
                "[NAB][Check connect to server] Request timed out, No connection to server."
            );

            return false;
        }
        catch (HttpRequestException httpRequestEx)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogError($"[NAB][Check connect to server] Connection failed: {httpRequestEx.Message} - {httpRequestEx.InnerException?.Message}");

            return false;
        }
        catch (Exception e)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogError(
                $"[NAB][Check connect to server] Exception during server check: {e.Message}"
            );

            return false;
        }


    }
}

public class RequestResponeHandler
{
    //Khoảng lệch thời gian giữa Server và Client
    private static TimeSpan diffirentTime = TimeSpan.Zero;

    private static float unityTimeServer;
    private static DateTime serverTime = DateTime.UtcNow;

    public static DateTime ServerTime
    {
        get => serverTime;
        set
        {
            unityTimeServer = Time.realtimeSinceStartup;
            serverTime = value;
            diffirentTime = value - DateTime.UtcNow;
        }
    }

    //Giờ hiện tại trên Server
    public static DateTime UtcNow =>
        ServerTime.AddSeconds(Time.realtimeSinceStartup - unityTimeServer);

    //public static DateTime UtcNow => DateTime.UtcNow;
    /// <summary>
    /// Truyền vào một thời gian trên Server sẽ và trả ra thời gian tương ứng dưới Client
    /// </summary>
    /// <param name="serverTime">Giờ Server</param>
    /// <returns>Giờ Client</returns>
    public static DateTime GetClientTimeFromServerTime(DateTime serverTime)
    {
        return serverTime - diffirentTime;
    }

    /// <summary>
    /// convert thời gian từ UTC sang thời gian Local
    /// </summary>
    /// <param name="utcTime"></param>
    /// <returns></returns>
    public static DateTime ConvertUtcToLocal(DateTime utcTime)
    {
        TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimeZone);
        return localTime;
    }
}
