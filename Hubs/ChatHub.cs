using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EduPsych_Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatHub(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendMessage(string roomId, string userId, string message)
        {
            // 1. إرسال رسالة التلميذ فوراً للجميع في الغرفة
            await Clients.All.SendAsync("ReceiveMessage", userId, message, false);

            if (roomId == "AI_Room")
            {
                try
                {
                    // 2. مناداة الـ Controller للحصول على إجابة الذكاء الاصطناعي
                    // ملاحظة: تأكد من تغيير المنفذ (Port) ليتناسب مع مشروعك (مثلاً 7120 أو 5001)
                    var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/ChatAi/ask", new { Prompt = message });

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                        string aiResponse = result.GetProperty("response").GetString();

                        // 3. إرسال رد الذكاء الاصطناعي للواجهة
                        await Clients.All.SendAsync("ReceiveMessage", "مساعد إبداع الذكي", aiResponse, true);
                    }
                }
                catch
                {
                    await Clients.All.SendAsync("ReceiveMessage", "مساعد إبداع الذكي", "عذراً، واجهت مشكلة في الاتصال بسيرفر الإجابات.", true);
                }
            }
        }
    }
}