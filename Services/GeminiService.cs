using System.Text;
using System.Text.Json;

namespace LifeNetAssist.MVC.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"] ?? "";
        }

        public async Task<string> AskAsync(string userMessage, string systemContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "AI assistant is not configured. Please add a Gemini API key in appsettings.json.";

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=" + _apiKey;

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemContext } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userMessage } }
                    }
                }
            };

            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "AI request failed (" + (int)response.StatusCode + "). Please try again.";

                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return string.IsNullOrWhiteSpace(text) ? "No response from AI." : text!;
            }
            catch (Exception ex)
            {
                return "AI error: " + ex.Message;
            }
        }
    }
}

