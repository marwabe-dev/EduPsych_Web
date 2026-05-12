using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace EduPsych_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatAiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        // ضع مفتاحك الجديد هنا
        private readonly string _groqApiKey = "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";

        public ChatAiController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskAi([FromBody] ChatRequest request)
        {
            try
            {
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant", // الموديل الذي اخترناه
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

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    var aiMessage = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    return Ok(new { response = aiMessage });
                }

                return BadRequest("عذراً، محرك الذكاء الاصطناعي مستغرق في التفكير حالياً، حاول ثانية.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ فني: {ex.Message}");
            }
        }
    }

    public class ChatRequest { public string Prompt { get; set; } }
}