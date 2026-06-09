using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduPsych_Web.Controllers
{
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            // 1. جلب الـ ID من الجلسة بشكل آمن وتحويله لـ long متوافق مع الداتابيز
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null) return RedirectToAction("Login", "Account");

            long currentUserId = Convert.ToInt64(sessionUserId);

            // 2. جلب ملف الولي وتأمين تحميل بيانات الطالب والـ User المرتبط به
            var parentProfile = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.user_id == currentUserId);

            // تهيئة قوائم فارغة في الـ ViewBag لمنع الـ NullReferenceException في الـ View
            ViewBag.RecentEduSessions = new List<EducationalSession>();
            ViewBag.RecentCounselSessions = new List<CounselSession>();
            ViewBag.LatestReport = null;

            if (parentProfile != null && parentProfile.student_id != null)
            {
                // 3. جلب الجلسات التعليمية الأخيرة للابن مع تفاصيل الأستاذ
                ViewBag.RecentEduSessions = await _context.EducationalSessions
                    .Include(es => es.Lesson)
                        .ThenInclude(l => l.Teacher)
                            .ThenInclude(t => t.User)
                    .Where(es => es.student_id == parentProfile.student_id)
                    .OrderByDescending(es => es.scheduled_at)
                    .Take(3)
                    .ToListAsync();

                // 4. جلب الجلسات النفسية الأخيرة للابن مع تفاصيل المستشار
                ViewBag.RecentCounselSessions = await _context.CounselSessions
                    .Include(cs => cs.Counselor)
                        .ThenInclude(c => c.User)
                    .Where(cs => cs.student_id == parentProfile.student_id)
                    .OrderByDescending(cs => cs.scheduled_at)
                    .Take(3)
                    .ToListAsync();

                // 5. جلب آخر تقييم للابن
                ViewBag.LatestReport = await _context.Reviews
                    .Where(r => r.student_id == parentProfile.student_id)
                    .OrderByDescending(r => r.created_at)
                    .FirstOrDefaultAsync();
            }

            return View(parentProfile);
        }

        // 2️⃣ ميثود ربط الولي بالطالب عن طريق الإيميل (تم إصلاح خطأ البحث هنا)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkStudentByEmail(string studentEmail)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null) return RedirectToAction("Login", "Account");

            long currentUserId = Convert.ToInt64(sessionUserId);

            if (string.IsNullOrEmpty(studentEmail))
            {
                TempData["Error"] = "يرجى إدخال البريد الإلكتروني للطالب.";
                return RedirectToAction("Index");
            }

            // 🛠️ التعديل هنا: استخدام دالة برمجية معيارية متوافقة تماماً مع قواعد البيانات
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.email.Trim().ToLower() == studentEmail.Trim().ToLower());

            if (student == null)
            {
                TempData["Error"] = "عذراً، لم يتم العثور على طالب بهذا البريد الإلكتروني.";
                return RedirectToAction("Index");
            }

            // جلب سجل الولي لتحديثه
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.user_id == currentUserId);

            if (parent != null)
            {
                parent.student_id = student.id; // ربط المعرف بالداتابيز
                parent.updated_at = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم ربط الحساب بنجاح بالطالب: {student.User.first_name} {student.User.last_name}";
            }
            else
            {
                TempData["Error"] = "حدث خطأ في سجل الولي، يرجى المحاولة لاحقاً.";
            }

            return RedirectToAction("Index");
        }
    }
}