using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;


namespace EduPsych_Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public StudentController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1️⃣ الصفحة الرئيسية للتلميذ
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .Include(s => s.Class)
                    .ThenInclude(c => c.Streams)
                .FirstOrDefaultAsync(s => s.user_id == userId);

            if (student == null) return BadRequest("بيانات التلميذ غير موجودة");

            var recentSessions = await _context.EducationalSessions
                .Include(es => es.Lesson)
                    .ThenInclude(l => l!.Teacher)
                        .ThenInclude(t => t!.User)
                .Where(es => es.student_id == student.id)
                .OrderByDescending(es => es.created_at)
                .Take(10)
                .ToListAsync();

            // جلب الشعبة لعرضها في البروفايل
            var studentStream = await _context.Streams
                .FirstOrDefaultAsync(st => st.class_id == student.class_id);

            ViewBag.RecentSessions = recentSessions;
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "تلميذ";
            ViewBag.StreamName = studentStream?.name ?? "عام";
            // أضف هذا السطر قبل return View(student);
            ViewBag.ClassName = student.Class?.name ?? "غير محدد";
            return View(student);
        }


        // 2️⃣ ميثودز المسار التعليمي (بدون تكرار)

        // اختيار الصف
        public async Task<IActionResult> EducationalPath()
        {
            var classes = await _context.Classes.ToListAsync();
            return View(classes);
        }

        // اختيار الشعبة
        public async Task<IActionResult> SelectStream(long classId)
        {
            var streams = await _context.Streams
                .Where(s => s.class_id == classId)
                .ToListAsync();

            var className = await _context.Classes
                .Where(c => c.id == classId)
                .Select(c => c.name)
                .FirstOrDefaultAsync();

            ViewBag.ClassName = className;
            ViewBag.ClassId = classId;

            return View(streams);
        }

        // اختيار المادة
       

        // 3️⃣ الدعم النفسي والمرشدين
        public async Task<IActionResult> PsychologicalSupport()
        {
            var specs = await _context.PsychSpecializations.ToListAsync();
            return View(specs);
        }

        public async Task<IActionResult> FilterCounselors(long specId)
        {
            // 1. جلب المستشارين النفسيين فقط المرتبطين بهذا المعرف
            var counselors = await _context.Counselors
                .Include(c => c.User)
                .Where(c => c.specialization_id == specId) // التأكد أن الربط مع جدول المستشارين
                .ToListAsync();

            // 2. التصحيح: جلب الاسم من جدول التخصصات النفسية وليس الجدول العام
            var specName = await _context.PsychSpecializations
                .Where(s => s.id == specId)
                .Select(s => s.name)
                .FirstOrDefaultAsync();

            // إذا لم يجد تخصصاً نفسياً بهذا الاسم، فهذا يعني أن هناك خطأ في الطلب
            if (string.IsNullOrEmpty(specName))
            {
                return RedirectToAction("PsychologicalSupport");
            }

            ViewBag.SpecName = specName;
            return View(counselors);
        }

        public async Task<IActionResult> CounselorProfile(long id)
        {
            // التأكد من جلب البيانات من جدول المستشارين فقط وبشرط الـ ID
            var counselor = await _context.Counselors
                .Include(c => c.User)
                .Include(c => c.PsychSpecialization) // تأكد أن هذا هو اسم العلاقة في الموديل
                .FirstOrDefaultAsync(c => c.id == id);

            // إذا لم يجد مستشاراً بهذا الرقم، أو وجد مستخدماً ليس مستشاراً
            if (counselor == null)
            {
                return RedirectToAction("PsychologicalSupport");
            }

            // لتصحيح التخصص يدوياً في حال وجود مشكلة في الـ Include
            if (counselor.PsychSpecialization == null)
            {
                ViewBag.RealSpecName = await _context.PsychSpecializations
                    .Where(s => s.id == counselor.specialization_id)
                    .Select(s => s.name)
                    .FirstOrDefaultAsync();
            }
            else
            {
                ViewBag.RealSpecName = counselor.PsychSpecialization.name;
            }

            return View(counselor);
        }

        // 4️⃣ الذكاء الاصطناعي (Gemini)
        public IActionResult Chat()
        {
            return View();
        }

        
        public async Task<IActionResult> SelectSubject(long streamId)
        {// جلب الشعبة الحالية لمعرفة الصف التابعة له
         // جلب المواد المرتبطة بالشعبة
            var subjects = await _context.Subjects
                .Where(s => s.stream_id == streamId)
                .ToListAsync();

            // نرسل قائمة المواد لصفحة "اختيار المادة"
            return View(subjects);
        }

        // هذه هي الميثود الجديدة التي تفتح "تفاصيل المادة" (الدروس والتمارين)

        // 1. ميثود تفاصيل المادة (تأكد أنها نسخة واحدة فقط في الملف)
        public async Task<IActionResult> SubjectDetails(long subjectId)
        {
            // جلب بيانات المادة
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.id == subjectId);

            if (subject == null) return NotFound();

            // جلب الدروس التابعة لهذه المادة
            var lessons = await _context.Lessons
                .Where(l => l.subject_id == subjectId)
                .ToListAsync();

            // جلب التمارين التابعة لدروس هذه المادة
            var lessonIds = lessons.Select(l => l.id).ToList();
            var exercises = await _context.Exercises
                .Where(e => lessonIds.Contains(e.lesson_id))
                .ToListAsync();

            // جلب الاختبارات (استخدمنا Try-Catch مؤقتاً لتجنب الكراش إذا لم تضف DbSet Exams بعد)
            try
            {
                ViewBag.Exams = await _context.Exams
                    .Where(ex => ex.subject_id == subjectId)
                    .ToListAsync();
            }
            catch
            {
                ViewBag.Exams = new List<Exam>(); // قائمة فارغة في حال عدم وجود الجدول
            }

            ViewBag.Lessons = lessons;
            ViewBag.Exercises = exercises;

            return View(subject);
        }
        // GET: Student/BookSession?counselorId=2
        public async Task<IActionResult> BookPsychSession(long counselorId)
        {
            var counselor = await _context.Counselors
        .Include(c => c.User)
        .Include(c => c.PsychSpecialization)
        .FirstOrDefaultAsync(c => c.id == counselorId);

            if (counselor == null)
            {
                return NotFound("عذراً، لم يتم العثور على هذا المرشد.");
            }

            ViewBag.SpecName = counselor.PsychSpecialization?.name ?? "مستشار نفسي";

            // 🔴 التصحيح هنا: يجب أن نكتب اسم الملف الذي أنشأته حرفياً 🔴
            return View("BookSessionpsy", counselor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBookingRequest(long counselorId, DateTime scheduledAt, string type, string? location)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            var counselor = await _context.Counselors.FindAsync(counselorId);

            if (student != null && counselor != null)
            {
                try
                {
                    // 1️⃣ إنشاء سجل الجلسة
                    var session = new CounselSession
                    {
                        counselor_id = counselorId,
                        student_id = student.id,
                        scheduled_at = scheduledAt,
                        session_type = type ?? "Online",
                        status = "Pending",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    _context.CounselSessions.Add(session);
                    await _context.SaveChangesAsync(); // نحفظ أولاً لنحصل على ID الجلسة

                    // 2️⃣ إنشاء سجل الدفع (Payment)
                    var payment = new Payment
                    {
                        student_id = student.id,
                        counsel_session_id = session.id, // الربط بالجلسة التي أنشئت للتو
                        amount = counselor.hourly_price ?? 0m,
                        status = "Pending",
                        // إضافة حقول الوقت في جدول Payment
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "تم إرسال طلب الحجز بنجاح وهو بانتظار موافقة المستشار.";
                    return RedirectToAction("PsychologicalSupport");
                }
                catch (Exception ex)
                {
                    // في حال حدوث خطأ، نعيد المستخدم للصفحة مع رسالة توضيحية
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.InnerException?.Message);
                    return View("BookSessionpsy", counselor);
                }
            }

            return BadRequest("بيانات الطالب أو المستشار غير صحيحة.");


        }

        public async Task<IActionResult> MySessions()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            if (student == null) return Unauthorized();

            // جلب الجلسات مع التأكد من جلب بيانات المستخدم (الاسم) والسعر
            var sessions = await _context.CounselSessions
                .Include(cs => cs.Counselor)
                    .ThenInclude(c => c.User)
                .Where(cs => cs.student_id == student.id)
                .OrderByDescending(cs => cs.created_at)
                .ToListAsync();

            // جلب المدفوعات
            var payments = await _context.Payments
                .Where(p => p.student_id == student.id)
                .ToListAsync();

            ViewBag.Payments = payments;

            return View(sessions);
        }

        public async Task<IActionResult> ProcessPayment(long paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();

            payment.status = "Paid"; // تم الدفع بنجاح
            payment.updated_at = DateTime.Now;

            // تحديث الجلسة لتصبح "Confirmed" (مؤكدة نهائياً)
            var session = await _context.CounselSessions.FindAsync(payment.counsel_session_id);
            if (session != null)
            {
                session.status = "Confirmed";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم الدفع بنجاح، موعدك الآن مؤكد!";
            return RedirectToAction("MySessions");
        }


       



        // ميثود استكشاف الأساتذة بناءً على المادة
        public async Task<IActionResult> ExploreTeachers(long subjectId)
        {
            // 1. جلب اسم المادة للعرض في الواجهة
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null) return NotFound();

            // 2. جلب قائمة المعرفات (IDs) للأساتذة الذين لديهم دروس في هذه المادة
            var teacherIds = await _context.Lessons
                .Where(l => l.subject_id == subjectId)
                .Select(l => l.teacher_id)
                .Distinct()
                .ToListAsync();

            // 3. جلب بيانات هؤلاء الأساتذة مع بياناتهم الشخصية من جدول Users
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => teacherIds.Contains(t.id))
                .ToListAsync();

            ViewBag.SubjectName = subject.name;
            ViewBag.SubjectId = subjectId;

            return View(teachers);
        }


        // ميثود عرض ملف الأستاذ وحجز حصة
        public async Task<IActionResult> TeacherProfile(long id, long subjectId)
        {
            // 1. جلب بيانات الأستاذ مع معلومات المستخدم (الاسم، الصورة..)
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.id == id);

            if (teacher == null) return NotFound();

            // 2. جلب الدروس الثابتة لهذه المادة المرتبطة بهذا الأستاذ
            var lessons = await _context.Lessons
                .Where(l => l.subject_id == subjectId && l.teacher_id == id)
                .ToListAsync();

            // 3. جلب اسم المادة لعرضها في العنوان
            var subject = await _context.Subjects.FindAsync(subjectId);

            ViewBag.Lessons = lessons;
            ViewBag.SubjectName = subject?.name ?? "المادة";
            ViewBag.SubjectId = subjectId;

            return View(teacher);
        }



        // 5️⃣ ميثود فتح صفحة تحديد موعد الحصة (GET)
        public async Task<IActionResult> BookSession(long lessonId, string type = "Online")
        {
            // جلب الدرس مع بيانات الأستاذ والمستخدم (User) الخاص به
            // لاحظ استخدام اسم الجدول "Lessons" كما هو في DbContext الخاص بك
            var lesson = await _context.Lessons
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(l => l.id == lessonId);

            if (lesson == null) return NotFound("الدرس غير موجود");

            // نرسل هذه البيانات للواجهة لتعبئة الحقول المخفية (Hidden Inputs)
            ViewBag.LessonId = lessonId;
            ViewBag.SessionType = type;

            // نرسل الموديل Teacher لأن صفحة BookSession.cshtml تتوقعه (@model Teacher)
            return View(lesson.Teacher);
        }

        // ميثود جديدة مخصصة لحجز جلسات المرشد النفسي
        public async Task<IActionResult> BookCounseling(long counselorId)
        {
            // البحث في جدول المرشدين (Counselors) وليس الدروس
            var counselor = await _context.Counselors
                .Include(c => c.User)
                .Include(c => c.PsychSpecialization)
                .FirstOrDefaultAsync(c => c.id == counselorId);

            if (counselor == null) return NotFound("المرشد النفسي غير موجود");

            // نرسل هذه البيانات للـ View (يمكنك استخدام نفس الـ View أو إنشاء واحد جديد)
            ViewBag.CounselorId = counselorId;

            // نرسل الموديل Counselor للواجهة
            return View("BookCounseling", counselor);
        }












        // ميثود عرض قائمة دروسي (التعليمية)
        public async Task<IActionResult> MyEducationalLessons()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            if (student == null) return Unauthorized();

            // --- الجزء الجديد: تحديث الحالة تلقائياً عند الدفع ---
            // نبحث عن أي جلسة "Accepted" ولها سجل دفع "Completed" في جدول المدفوعات
            var paidSessions = await _context.Payments
                .Where(p => p.student_id == student.id && p.status == "Completed" && p.educational_session_id != null)
                .Select(p => p.educational_session_id)
                .ToListAsync();

            var sessionsToUpdate = await _context.EducationalSessions
                .Where(s => paidSessions.Contains(s.id) && s.status == "Accepted")
                .ToListAsync();

            if (sessionsToUpdate.Any())
            {
                foreach (var session in sessionsToUpdate)
                {
                    session.status = "Confirmed"; // تحديث الحالة لتم الدفع
                }
                await _context.SaveChangesAsync();
            }
            // ---------------------------------------------------

            var myLessons = await _context.EducationalSessions
                .Include(s => s.Lesson).ThenInclude(l => l.Teacher).ThenInclude(t => t.User)
                .Where(s => s.student_id == student.id)
                .OrderByDescending(s => s.created_at)
                .ToListAsync();

            return View(myLessons);
        }



    


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(long lessonId, string type, DateTime scheduledAt, string? location)
        {
            // 1. التأكد من هوية الطالب من الجلسة (Session)
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // جلب بيانات التلميذ من جدول Students
            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            if (student == null) return BadRequest("بيانات الطالب غير موجودة");

            // 2. إنشاء سجل جديد في جدول الحصص التعليمية (EducationalSessions)
            // هذا هو الجدول رقم 13 في قاعدة بياناتك
            var newSession = new EducationalSession
            {
                lesson_id = lessonId,
                student_id = student.id,
                session_type = type ?? "Online",
                scheduled_at = scheduledAt,
                location = location,
                status = "Pending", // الحالة تبدأ بـ "قيد الانتظار" حتى يوافق الأستاذ
                created_at = DateTime.Now
            };

            try
            {
                // حفظ في قاعدة البيانات
                _context.EducationalSessions.Add(newSession);
                await _context.SaveChangesAsync();

                // رسالة نجاح تظهر للتلميذ
                TempData["Success"] = "تم إرسال طلب الحجز بنجاح! انتظر موافقة الأستاذ لتتمكن من الدفع.";

                // التوجه لصفحة "دروسي" لمتابعة حالة الطلب (الميثود التي أنشأناها مؤخراً)
                return RedirectToAction("MyEducationalLessons");
            }
            catch (Exception ex)
            {
                // في حال حدوث خطأ أثناء الحفظ
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ الحجز: " + ex.Message);
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> PaymentPage(long? sessionId, long? counselId)
        {
            string uniqueMetadataId = "";
            decimal price = 0;
            string returnUrl = "";

            // 1️⃣ أولاً: التحقق من نوع الجلسة (نفسية أم تعليمية)
            if (counselId.HasValue && counselId.Value > 0)
            {
                // الجانب النفسي
                var psychSession = await _context.CounselSessions
                    .Include(c => c.Counselor)
                    .FirstOrDefaultAsync(c => c.id == counselId.Value);

                if (psychSession == null) return NotFound("الجلسة النفسية غير موجودة");

                // التحقق من الحالة (مرن)
                var status = psychSession.status?.Trim().ToLower() ?? "";
                if (status != "approved" && status != "accepted")
                {
                    return BadRequest($"هذه الجلسة لم يتم الموافقة عليها بعد. الحالة الحالية: {psychSession.status}");
                }

                uniqueMetadataId = "psych_" + counselId.Value.ToString();
                price = psychSession.Counselor?.hourly_price ?? 0;
                returnUrl = Url.Action("MyCounselSessions", "Student", null, Request.Scheme);
            }
            else if (sessionId.HasValue && sessionId.Value > 0)
            {
                // 2️⃣ ثانياً: الجانب التعليمي (الذي كان يعمل سابقاً)
                var eduSession = await _context.EducationalSessions
                    .Include(s => s.Lesson)
                        .ThenInclude(l => l.Teacher)
                    .FirstOrDefaultAsync(s => s.id == sessionId.Value);

                if (eduSession == null) return NotFound("الحصة التعليمية غير موجودة");

                uniqueMetadataId = sessionId.Value.ToString();
                price = eduSession.Lesson?.Teacher?.hourly_price ?? 0;
                returnUrl = Url.Action("MyEducationalLessons", "Student", null, Request.Scheme);
            }
            else
            {
                return BadRequest("لم يتم تحديد أي جلسة للدفع.");
            }

            // 3️⃣ ثالثاً: الاتصال بـ Chargily (هذا الجزء مشترك ولا يتغير)
            try
            {
                var apiKey = _configuration["Chargily:SecretKey"];
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var checkoutData = new
                {
                    amount = (int)price,
                    currency = "dzd",
                    success_url = returnUrl,
                    failure_url = returnUrl,
                    metadata = new[] {
                new { name = "session_id", value = uniqueMetadataId }
            }
                };

                var response = await client.PostAsJsonAsync("https://pay.chargily.net/test/api/v2/checkouts", checkoutData);

                if (response.IsSuccessStatusCode)
                {
                    // تحديث الحالة يدوياً للـ Localhost
                    if (counselId.HasValue)
                    {
                        var session = await _context.CounselSessions.FindAsync(counselId.Value);
                        if (session != null) { session.status = "Confirmed"; }
                    }
                    else if (sessionId.HasValue)
                    {
                        var session = await _context.EducationalSessions.FindAsync(sessionId.Value);
                        if (session != null) { session.status = "Confirmed"; }
                    }

                    await _context.SaveChangesAsync();

                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    string url = result.GetProperty("checkout_url").GetString();
                    return Redirect(url);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("خطأ فني: " + ex.Message);
            }

            return BadRequest("فشل إنشاء جلسة الدفع، تأكد من مفتاح API لشارجيلي."); 
        }









        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCounselBooking(long counselorId, DateTime scheduledAt, string sessionType = "Online")
        {
            // 1. التأكد من تسجيل دخول الطالب
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            if (student == null) return Unauthorized();

            // 2. إنشاء سجل جديد في جدول counsel_session (الجدول رقم 14)
            var newCounselSession = new CounselSession
            {
                counselor_id = counselorId,
                student_id = student.id,
                session_type = sessionType,
                scheduled_at = scheduledAt,
                status = "Pending", // الحالة الافتراضية حتى يوافق المرشد
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            try
            {
                _context.CounselSessions.Add(newCounselSession);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إرسال طلب حجز الجلسة النفسية بنجاح! يرجى انتظار موافقة المرشد.";

                // التوجيه لصفحة عرض الجلسات النفسية للطالب
                return RedirectToAction("MyCounselSessions");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "خطأ في الحفظ: " + ex.Message);
                return RedirectToAction("CounselorProfile", new { id = counselorId });
            }
        }





        public async Task<IActionResult> MyCounselSessions()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);

            // جلب الجلسات النفسية مع بيانات المرشد والمستخدم
            var sessions = await _context.CounselSessions
                .Include(cs => cs.Counselor)
                    .ThenInclude(c => c.User)
                .Where(cs => cs.student_id == student.id)
                .OrderByDescending(cs => cs.created_at)
                .ToListAsync();

            return View(sessions);
        }



        [HttpPost]
        public async Task<IActionResult> ConfirmCounselingPayment(long sessionId)
        {
            // 1. البحث عن سجل الدفع المرتبط بهذه الجلسة
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.counsel_session_id == sessionId);
            var session = await _context.CounselSessions.FindAsync(sessionId);

            if (payment != null && session != null)
            {
                // 2. تحديث حالة الدفع إلى "تم الدفع"
                payment.status = "Paid";
                payment.updated_at = DateTime.Now;

                // 3. تحديث حالة الجلسة نفسها لكي يعرف المرشد أن الطالب دفع
                session.status = "Paid";

                await _context.SaveChangesAsync();

                TempData["Success"] = "تمت عملية الدفع بنجاح! يمكنك الآن التواصل مع المرشد.";
            }

            return RedirectToAction("MyCounselSessions", "Student");
        }









        public async Task<IActionResult> Courses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // جلب بيانات الطالب مع الصف (Class)
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.user_id == userId);

            if (student == null) return Content("بيانات الطالب غير مكتملة.");

            // جلب الدورات المخصصة لمستوى الطالب (class_id)
            var availableCourses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .Where(c => c.class_id == student.class_id && c.is_published == true)
                .AsNoTracking()
                .ToListAsync();

            // جلب معرفات الدورات التي اشتراها الطالب (لن تظهر له "شراء" بل "دخول")
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.student_id == student.id)
                .Select(e => e.course_id)
                .ToListAsync();

            ViewBag.EnrolledCourseIds = enrolledCourseIds;
            ViewBag.StudentName = student.User?.first_name;
            ViewBag.ClassName = student.Class?.name;

            return View(availableCourses);
        }
















        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyCourse(long courseId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null || student == null) return NotFound();

            // 1. التحقق من عدم التكرار
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.student_id == student.id && e.course_id == course.id);
            if (alreadyEnrolled) return RedirectToAction("ViewCourse", new { id = courseId });

            // 2. جلب محفظة الطالب وفحص الرصيد
            var wallet = await _context.StudentWallets.FirstOrDefaultAsync(w => w.student_id == student.id);
            if (wallet == null || wallet.balance < course.price)
            {
                TempData["Error"] = $"رصيدك غير كافٍ. تحتاج إلى {course.price} دج.";
                return RedirectToAction("Courses");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // أ- خصم المبلغ من التلميذ
                    wallet.balance -= course.price;
                    wallet.updated_at = DateTime.Now;

                    // ب- حساب العمولات (50% لكل طرف)
                    decimal teacherMoney = course.price * 0.50m;
                    decimal platformMoney = course.price - teacherMoney;

                    // ج- إضافة الربح لمحفظة الأستاذ (تأكد أن الجدول اسمه TeacherWallets في DbContext)
                    var tWallet = await _context.TeacherWallets
                        .FirstOrDefaultAsync(w => w.teacher_id == course.teacher_id);

                    if (tWallet == null)
                    {
                        tWallet = new TeacherWallet { teacher_id = course.teacher_id, total_earned = 0 };
                        _context.TeacherWallets.Add(tWallet);
                    }
                    tWallet.total_earned += teacherMoney;
                    tWallet.updated_at = DateTime.Now;

                    // د- تسجيل الاشتراك والعملية المالية في جدول واحد (CourseEnrollment)
                    // هذا هو التصحيح للخطأ الذي ظهر لك
                    var enrollment = new CourseEnrollment
                    {
                        student_id = student.id,
                        course_id = course.id,
                        amount_paid = course.price,
                        platform_commission = platformMoney, // الحقل كما في SQL الخاص بك
                        teacher_commission = teacherMoney,   // الحقل كما في SQL الخاص بك
                        enrolled_at = DateTime.Now            // تأكد من وجود هذا الحقل في الموديل
                    };

                    _context.CourseEnrollments.Add(enrollment);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تم الشراء بنجاح!";
                    return RedirectToAction("MyCourses");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "حدث خطأ أثناء المعاملة.";
                    return RedirectToAction("Courses");
                }
            }
        }








        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.user_id == userId);

            // جلب الدورات المشترك فيها الطالب فقط عبر جدول course_enrollments
            var enrolledCourses = await _context.CourseEnrollments
                .Where(e => e.student_id == student.id)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .Select(e => e.Course)
                .ToListAsync();

            return View(enrolledCourses);
        }







        [HttpGet]
        public async Task<IActionResult> ViewCourse(long id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);

            // التحقق من أن الطالب اشترى الدورة فعلاً قبل فتح المحتوى
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.student_id == student.id && e.course_id == id);

            if (!isEnrolled)
            {
                TempData["Error"] = "يجب شراء الدورة أولاً للوصول إلى المحتوى.";
                return RedirectToAction("Courses");
            }

            var course = await _context.Courses
                .Include(c => c.Lessons) // جلب الدروس التابعة للدورة
                .FirstOrDefaultAsync(c => c.id == id);

            return View(course);
        }







        public async Task<IActionResult> MyWallet(long? paySessionId, long? payCounselId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);

            var wallet = await _context.StudentWallets.FirstOrDefaultAsync(w => w.student_id == student.id);

            // جلب سجل طلبات الشحن الخاصة بهذا الطالب (الكود الأصلي الخاص بك)
            ViewBag.DepositRequests = await _context.DepositRequests
                .Where(d => d.student_id == student.id)
                .OrderByDescending(d => d.created_at)
                .ToListAsync();

            // --- الإضافة الجديدة لدعم نظام الدفع الذكي ---
            if (payCounselId.HasValue)
            {
                var session = await _context.CounselSessions
                    .Include(c => c.Counselor)
                    .FirstOrDefaultAsync(c => c.id == payCounselId);

                ViewBag.PayAmount = session?.Counselor?.hourly_price ?? 0;
                ViewBag.PayDescription = "جلسة إرشاد نفسي";
                ViewBag.TargetCounselId = payCounselId;
            }
            else if (paySessionId.HasValue)
            {
                var session = await _context.EducationalSessions
                    .Include(s => s.Lesson).ThenInclude(l => l.Teacher)
                    .FirstOrDefaultAsync(s => s.id == paySessionId);

                ViewBag.PayAmount = session?.Lesson?.Teacher?.hourly_price ?? 0; // أو السعر الذي تحدده للدرس
                ViewBag.PayDescription = $"حصة تعليمية: {session?.Lesson?.title}";
                ViewBag.TargetSessionId = paySessionId;
            }
            // ------------------------------------------

            return View(wallet);
        }




        [HttpPost]
        public async Task<IActionResult> UploadReceipt(decimal amount, IFormFile receiptFile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);

            if (receiptFile != null && amount > 0)
            {
                // حفظ الصورة في مجلد uploads/receipts
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                var fileName = Guid.NewGuid() + Path.GetExtension(receiptFile.FileName);
                using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await receiptFile.CopyToAsync(stream);
                }

                // تسجيل الطلب في قاعدة البيانات
                var deposit = new DepositRequest
                {
                    student_id = student.id,
                    amount = amount,
                    receipt_image_url = "/uploads/receipts/" + fileName,
                    status = "Pending"
                };

                _context.DepositRequests.Add(deposit);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إرسال وصل الدفع بنجاح. سيتم مراجعته وشحن رصيدك خلال ساعات.";
            }
            return RedirectToAction("MyWallet");
        }





        [HttpPost]
        public async Task<IActionResult> PaySessionFromWallet(long? sessionId, long? counselId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == userId);
            if (student == null) return BadRequest("بيانات الطالب غير موجودة");

            // جلب محفظة الطالب
            var wallet = await _context.StudentWallets.FirstOrDefaultAsync(w => w.student_id == student.id);

            decimal price = 0;
            string sessionType = "";

            // 1. تحديد نوع الجلسة والسعر
            if (counselId.HasValue)
            {
                var session = await _context.CounselSessions.Include(c => c.Counselor).FirstOrDefaultAsync(c => c.id == counselId);
                if (session == null) return NotFound("الجلسة النفسية غير موجودة");

                price = session.Counselor?.hourly_price ?? 0;
                sessionType = "Counsel";
            }
            else if (sessionId.HasValue)
            {
                var session = await _context.EducationalSessions.Include(s => s.Lesson).ThenInclude(l => l.Teacher).FirstOrDefaultAsync(s => s.id == sessionId);
                if (session == null) return NotFound("الحصة التعليمية غير موجودة");

                price = session.Lesson?.Teacher?.hourly_price ?? 0;
                sessionType = "Edu";
            }
            else
            {
                return BadRequest("لم يتم تحديد جلسة للدفع");
            }

            // 2. التحقق من كفاية الرصيد
            if (wallet == null || wallet.balance < price)
            {
                TempData["Error"] = "رصيدك غير كافٍ، يرجى شحن المحفظة أولاً.";
                return RedirectToAction("MyWallet", new { paySessionId = sessionId, payCounselId = counselId });
            }

            // 3. خصم من الطالب وتوزيع الأرباح
            wallet.balance -= price;
            decimal halfAmount = price * 0.5m;

            if (sessionType == "Counsel")
            {
                var session = await _context.CounselSessions.FindAsync(counselId);
                session.status = "Paid";
                session.amount_paid = price;
                session.counselor_commission = halfAmount;
                session.platform_commission = halfAmount;

                // إضافة الربح لمحفظة المرشد
                var cWallet = await _context.CounselorWallets.FirstOrDefaultAsync(w => w.counselor_id == session.counselor_id);
                if (cWallet != null)
                {
                    cWallet.total_earned += halfAmount;
                    cWallet.updated_at = DateTime.Now;
                }
            }
            else
            {
                var session = await _context.EducationalSessions.Include(es => es.Lesson).FirstOrDefaultAsync(es => es.id == sessionId);
                session.status = "Paid";
                // إذا كان لديك حقول عمولات في جدول الجلسات التعليمية أضفها هنا مثل الجلسات النفسية

                // إضافة الربح لمحفظة الأستاذ
                var tWallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == session.Lesson.teacher_id);
                if (tWallet != null)
                {
                    tWallet.total_earned += halfAmount;
                    tWallet.updated_at = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم دفع ثمن الحصة بنجاح وتأكيد موعدك.";
            return RedirectToAction("Index");
        }
    }
}