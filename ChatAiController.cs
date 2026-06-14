using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduPsych_Web.Controllers
{
    // 💡 قمنا بتغيير اسم المسار بالكامل إلى رفيقني لكسر أي كاش في السيرفر
    [Route("api/rafiqnichat")]
    [ApiController]
    public class RafiqniChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _groqApiKey = "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";

        public RafiqniChatController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] NewChatRequest request)
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

                // إذا فشل مفتاح الـ API أو السيرفر، سيعطيك هذا الرد الواضح جداً لتعرف السبب
                return Ok(new { response = $"[رد تجريبي] وصلنا للسيرفر الحقيقي بنجاح ولكن Groq API واجه مشكلة: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Ok(new { response = $"[رد تجريبي] متصل بالسيرفر الجديد ولكن حدث خطأ داخلي: {ex.Message}" });
            }
        }
    }

    public class NewChatRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
    }
}