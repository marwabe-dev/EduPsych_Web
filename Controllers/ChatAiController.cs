using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduPsych_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatAiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        // ⚠️ تنبيه أمني: يفضل نقل هذا المفتاح لملف appsettings.json لاحقاً
        private readonly string _groqApiKey = "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";

        public ChatAiController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskAi([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { response = "الرجاء كتابة سؤالك أولاً." });
            }

            try
            {
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = new[]
                    {
                        new { role = "system", content = "أنت 'مساعد إبداع الذكي'، خبير تربوي في منصة EduPsych. أجب على أسئلة التلاميذ بأسلوب تعليمي، واضح، ومبسط باللغة العربية." },
                        new { role = "user", content = request.Prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);

                var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var aiMessage = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    return Ok(new { response = aiMessage });
                }

                // 🔍 [تعديل جوهري] إذا فشل الاتصال بـ Groq، سنعرض السبب القادم منهم مباشرة في الشات لنعرف المشكلة
                return BadRequest(new { response = $"فشل محرك الذكاء الاصطناعي. الرد من السيرفر: {response.StatusCode} - {responseString}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { response = $"خطأ فني داخلي: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
    }
}