using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DominiShop.Service
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public AIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _apiKey = configuration["OpenRouter:ApiKey"];
        }

        public async Task<string> SendMessageAsync(IEnumerable<Model.ChatMessage> messages, string systemContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "Error: OpenRouter API key is missing in appsettings.json. Please configure 'OpenRouter:ApiKey'.";
            }

            var apiMessages = new List<object>
            {
                new { role = "system", content = systemContext }
            };

            // Convert our ChatMessage model to API format
            foreach (var msg in messages)
            {
                string role = msg.Role.Equals("AI", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
                apiMessages.Add(new { role = role, content = msg.Text });
            }

            var requestBody = new
            {
                model = "openai/gpt-oss-20b:free",
                messages = apiMessages
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            requestMessage.Headers.Add("HTTP-Referer", "http://localhost"); // Optional, for OpenRouter analytics
            requestMessage.Headers.Add("X-Title", "DominiShop Analytics"); // Optional, for OpenRouter analytics
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(jsonResponse);
                
                var reply = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return reply ?? "Error: No response from AI.";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }
    }
}
