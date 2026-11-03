using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ApiTests.Clients
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public ApiClient(string baseUrl, string authToken = null)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await SendAsync(HttpMethod.Get, endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, string jsonBody)
        {
            return await SendAsync(HttpMethod.Post, endpoint, jsonBody);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
        {
            return await _httpClient.PostAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, string jsonBody)
        {
            return await SendAsync(HttpMethod.Put, endpoint, jsonBody);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content)
        {
            return await _httpClient.PutAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> PatchAsync(string endpoint, string jsonBody)
        {
            return await SendAsync(HttpMethod.Patch, endpoint, jsonBody);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await SendAsync(HttpMethod.Delete, endpoint);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, string jsonBody = null)
        {
            using var request = new HttpRequestMessage(method, endpoint);
            if (jsonBody != null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            return await _httpClient.SendAsync(request);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}