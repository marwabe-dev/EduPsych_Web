using EduPsych_Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace EduPsych_Web.Controllers
{
    [Route("api/chargily/webhook")]
    [ApiController]
    public class ChargilyWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ChargilyWebhookController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            try
            {
                // 1. التحقق من التوقيع (Security Signature)
                var signature = Request.Headers["Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature)) return BadRequest("Signature missing");

                using var reader = new StreamReader(Request.Body);
                var jsonPayload = await reader.ReadToEndAsync();

                // استخدم المفتاح الخاص بك من appsettings.json
                var secretKey = _configuration["Chargily:SecretKey"] ?? "test_sk_qk5z0Ad7mVJoDvirrnKBk5ZSmJQYa6WSLNPhrZ8o";
                if (!VerifySignature(jsonPayload, signature, secretKey))
                {
                    return BadRequest("Invalid Signature");
                }

                // 2. تحليل بيانات JSON
                using var doc = JsonDocument.Parse(jsonPayload);
                var root = doc.RootElement;
                var eventType = root.GetProperty("type").GetString();

                // 3. معالجة الحدث عند نجاح الدفع فقط
                if (eventType == "checkout.paid")
                {
                    var data = root.GetProperty("data");
                    var metadata = data.GetProperty("metadata");

                    string rawId = "";
                    foreach (var item in metadata.EnumerateArray())
                    {
                        if (item.GetProperty("name").GetString() == "session_id")
                        {
                            rawId = item.GetProperty("value").GetString();
                            break;
                        }
                    }

                    // --- منطق تحديث قاعدة البيانات بناءً على الجداول المرسلة ---

                    if (rawId.StartsWith("psych_")) // حالة: جلسة نفسية (Counsel Session)
                    {
                        long counselId = long.Parse(rawId.Replace("psych_", ""));

                        var session = await _context.CounselSessions
                            .Include(c => c.Counselor)
                            .FirstOrDefaultAsync(c => c.id == counselId);

                        if (session != null)
                        {
                            session.status = "Confirmed"; // تحديث جدول 14

                            // تحديث جدول 15 (Payment) الموحد
                            var payment = await _context.Payments
                                .FirstOrDefaultAsync(p => p.counsel_session_id == counselId);

                            if (payment != null)
                            {
                                payment.status = "Completed"; // أو 'Paid' حسب اختيارك
                                payment.updated_at = DateTime.Now;
                            }

                            // إضافة تنبيه للمرشد (جدول 17)
                            _context.Notifications.Add(new EduPsych_Web.Models.Notification
                            {
                                counselor_id = session.counselor_id,
                                type = "Psych_Payment_Success",
                                message = $"تم دفع ثمن جلسة الدعم نفسي بنجاح من قبل الطالب.",
                                created_at = DateTime.Now,
                                is_read = false
                            });
                        }
                    }
                    else if (long.TryParse(rawId, out var sessionId)) // حالة: حصة تعليمية (Educational Session)
                    {
                        var session = await _context.EducationalSessions
                            .Include(s => s.Lesson)
                            .FirstOrDefaultAsync(s => s.id == sessionId);

                        if (session != null)
                        {
                            session.status = "Confirmed"; // تحديث جدول 13

                            // تحديث جدول 15 (Payment) الموحد
                            var payment = await _context.Payments
                                .FirstOrDefaultAsync(p => p.educational_session_id == sessionId);

                            if (payment != null)
                            {
                                payment.status = "Completed";
                                payment.updated_at = DateTime.Now;
                            }

                            // إضافة تنبيه للأستاذ (جدول 17)
                            _context.Notifications.Add(new EduPsych_Web.Models.Notification
                            {
                                teacher_id = session.Lesson.teacher_id,
                                type = "Payment_Success",
                                message = $"تم دفع ثمن حصة {session.Lesson.title} بنجاح.",
                                created_at = DateTime.Now,
                                is_read = false
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(); // الرد على شارجيلي بـ 200 OK ليتوقفوا عن إرسال الإشعار
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        private bool VerifySignature(string payload, string signature, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return computedSignature == signature;
        }
    }
}