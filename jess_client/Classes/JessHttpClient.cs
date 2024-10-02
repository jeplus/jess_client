using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace ensims.jess_client.Classes {

    public class JessHttpClient {

        private static readonly ILog log = LogManager.GetLogger(typeof(JessHttpClient));

        private readonly HttpClient _httpClient;
        private const string SessionKeyCookieName = "session_token";

        public JessHttpClient() {

            var handler = new HttpClientHandler {
                // CookieContainer = new CookieContainer()
                UseCookies = false
            };
            _httpClient = new HttpClient(handler);
            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            // Ignore invalid SSL certs
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; };
        }

        private HttpResponseMessage SendRequestSync(HttpRequestMessage request) {
            // Add the session key cookie to the request if available
            if (!string.IsNullOrEmpty(GlobalUtility.Config.SessionKey)) {
                request.Headers.Add("Cookie", $"{SessionKeyCookieName}={GlobalUtility.Config.SessionKey}");
                // _httpClient.DefaultRequestHeaders.Add("Cookie", $"{SessionKeyCookieName}={GlobalUtility.Config.SessionKey}");
            }

            // Send the request
            HttpResponseMessage response = _httpClient.SendAsync(request).GetAwaiter().GetResult();

            // Extract the session key cookie from the response
            if (response.Headers.Contains("Set-Cookie")) {
                var cookies = response.Headers.GetValues("Set-Cookie");
                foreach (var cookie in cookies) {
                    if (cookie.StartsWith($"{SessionKeyCookieName}=")) {
                        GlobalUtility.Config.SessionKey = cookie.Split('=') [1].Split(';') [0];

                        break;
                    }
                }
            }

            // Ensure successful status code
            response.EnsureSuccessStatusCode();

            return response;
        }

        public T SendRequest<T>(HttpRequestMessage request) {
            var response = SendRequestSync(request);
            string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public T Get<T>(string url) {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return SendRequest<T>(request);
        }

        public T Post<T>(string url, object data) {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return SendRequest<T>(request);
        }

        public async Task<T> SendRequestAsync<T>(HttpRequestMessage request) {
            // Add the session key cookie to the request if available
            if (!string.IsNullOrEmpty(GlobalUtility.Config.SessionKey)) {
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"{SessionKeyCookieName}={GlobalUtility.Config.SessionKey}");
            }

            // Send the request
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            // Extract the session key cookie from the response
            IEnumerable<string> cookies;
            if (response.Headers.TryGetValues ("Set-Cookie", out cookies)) {
                foreach (var cookie in cookies) {
                    if (cookie.StartsWith($"{SessionKeyCookieName}=")) {
                        GlobalUtility.Config.SessionKey = cookie.Split('=') [1].Split(';') [0];
                        break;
                    }
                }
            }

            // Ensure successful status code
            response.EnsureSuccessStatusCode();

            // Read and deserialize the response content
            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<T> GetAsync<T>(string url) {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendRequestAsync<T>(request);
        }

        public async Task<T> PostAsync<T>(string url, object data) {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendRequestAsync<T>(request);
        }

        public async Task<T> UploadFilesAsync<T>(string url, IEnumerable<FileInfo> files, Dictionary<string, string> formData) {
            using (var content = new MultipartFormDataContent()) {
                // Add file content
                foreach (var file in files) {
                    var fileContent = new StreamContent(file.OpenRead());
                    content.Add(fileContent, "file", file.Name);
                }

                // Add other form data
                foreach (var kvp in formData) {
                    content.Add(new StringContent(kvp.Value), kvp.Key);
                }

                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, url) {
                    Content = content
                };

                // Send request and process response
                return await SendRequestAsync<T>(request);
            }
        }

        // Synchronous version of UploadFiles
        public T UploadFiles<T>(string url, IEnumerable<FileInfo> files, Dictionary<string, string> formData) {
            return UploadFilesAsync<T>(url, files, formData).GetAwaiter().GetResult();
        }

        public async Task<string> DownloadFileAsync(string url, string folderPath) {
            // Add the session key cookie to the request if available
            if (!string.IsNullOrEmpty(GlobalUtility.Config.SessionKey)) {
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"{SessionKeyCookieName}={GlobalUtility.Config.SessionKey}");
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string fileName = GetFileNameFromResponse(response);
            string filePath = Path.Combine(folderPath, fileName);

            using (var fs = new FileStream(filePath, FileMode.CreateNew)) {
                await response.Content.CopyToAsync(fs);
            }

            return filePath;
        }

        public string DownloadFile(string url, string folderPath) {
            return DownloadFileAsync(url, folderPath).GetAwaiter().GetResult();
        }

        private string GetFileNameFromResponse(HttpResponseMessage response) {
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition != null && !string.IsNullOrEmpty(contentDisposition.FileName)) {
                return contentDisposition.FileName.Trim('"');
            }

            // If no filename is provided in the header, generate one based on the current timestamp
            return $"download_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(response.RequestMessage.RequestUri.AbsolutePath)}";
        }

    }
}