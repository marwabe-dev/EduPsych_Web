using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace EduPsych_Web.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // لوحة التحكم الرئيسية
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Specialization)
                .FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher == null) return Content("بيانات الأستاذ غير موجودة.");

            long currentTeacherId = teacher.id;

            // 1. جلب الإشعارات
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.teacher_id == currentTeacherId)
                .OrderByDescending(n => n.created_at)
                .Take(10).AsNoTracking().ToListAsync();

            // 2. جلب الطلبات المعلقة
            var pendingData = await (from s in _context.EducationalSessions
                                     join l in _context.Lessons on s.lesson_id equals l.id
                                     join st in _context.Students on s.student_id equals st.id
                                     join u in _context.Users on st.user_id equals u.id
                                     join c in _context.Classes on st.class_id equals c.id
                                     where l.teacher_id == currentTeacherId && s.status == "Pending"
                                     select new
                                     {
                                         Id = s.id,
                                         Date = s.scheduled_at,
                                         LTitle = l.title,
                                         SName = u.first_name + " " + u.last_name,
                                         CName = c.name
                                     }).AsNoTracking().ToListAsync();

            ViewBag.PendingRequests = pendingData;

            // 3. جلب الحصص القادمة
            var upcomingData = await (from s in _context.EducationalSessions
                                      join l in _context.Lessons on s.lesson_id equals l.id
                                      join st in _context.Students on s.student_id equals st.id
                                      join u in _context.Users on st.user_id equals u.id
                                      where l.teacher_id == currentTeacherId &&
                                            (s.status == "Accepted" || s.status == "Paid")
                                      select new
                                      {
                                          Id = s.id,
                                          Date = s.scheduled_at,
                                          LTitle = l.title,
                                          SName = u.first_name + " " + u.last_name,
                                          Link = s.meeting_link,
                                          Status = s.status
                                      }).AsNoTracking().ToListAsync();

            ViewBag.UpcomingSessions = upcomingData;

            // 4. جلب الدروس والتمارين
            ViewBag.TeacherLessons = await _context.Lessons
                .Where(l => l.teacher_id == currentTeacherId)
                .Select(l => new {
                    id = l.id,
                    title = l.title,
                    pdf_url = l.pdf_url,
                    ClassName = _context.Classes.Where(c => c.id == l.class_id).Select(c => c.name).FirstOrDefault(),
                    SubjectName = _context.Subjects.Where(sub => sub.id == l.subject_id).Select(sub => sub.name).FirstOrDefault(),
                    Exercises = _context.Exercises.Where(ex => ex.lesson_id == l.id).Select(ex => new { ex.title, ex.file_url }).ToList()
                }).AsNoTracking().ToListAsync();

            // 5. المحفظة
            var wallet = await _context.TeacherWallets
                .Where(w => w.teacher_id == currentTeacherId)
                .Select(w => new { balance = w.total_earned - w.withdrawn_amount })
                .FirstOrDefaultAsync();

            ViewBag.WalletBalance = wallet?.balance ?? 0;

            // 6. القوائم المنسدلة
            ViewBag.Subjects = await _context.Subjects.AsNoTracking().ToListAsync();
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();

            return View(teacher);
        }

        // صفحة الملف الشخصي
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Specialization)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher == null) return NotFound();

            ViewBag.LessonsCount = await _context.Lessons.CountAsync(l => l.teacher_id == teacher.id);

            var sessionsStatus = await (from s in _context.EducationalSessions
                                        join l in _context.Lessons on s.lesson_id equals l.id
                                        where l.teacher_id == teacher.id
                                        select s.status).ToListAsync();

            ViewBag.ConfirmedSessionsCount = sessionsStatus.Count(s => s == "Confirmed" || s == "Paid" || s == "Accepted");

            ViewBag.TeacherSessions = await (from s in _context.EducationalSessions
                                             join l in _context.Lessons on s.lesson_id equals l.id
                                             where l.teacher_id == teacher.id
                                             select new EducationalSession
                                             {
                                                 id = s.id,
                                                 scheduled_at = s.scheduled_at,
                                                 status = s.status,
                                                 Lesson = new Lesson { title = l.title }
                                             }).AsNoTracking().ToListAsync();

            return View(teacher);
        }

        // صفحة الدورات التدريبية (تمت إضافة Include للـ Class)
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Class) // ضروري لعرض اسم المستوى
                .Where(c => c.teacher_id == teacher.id)
                .OrderByDescending(c => c.created_at)
                .AsNoTracking()
                .ToListAsync();

            return View(courses);
        }

        // --- الأكشنز الخاصة بالعمليات (Post) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptSession(long sessionId)
        {
            var session = await _context.EducationalSessions.FindAsync(sessionId);
            if (session == null) return NotFound();
            session.status = "Accepted";
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم قبول الحصة بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMeetingLink(long sessionId, string meetingLink)
        {
            var session = await _context.EducationalSessions.FindAsync(sessionId);
            if (session == null) return NotFound();
            session.meeting_link = meetingLink;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث رابط الحصة بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSession(long sessionId)
        {
            var session = await _context.EducationalSessions.FindAsync(sessionId);
            if (session == null) return NotFound();
            session.status = "Rejected";
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم رفض الطلب بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddContent(long subjectId, long classId, string title, string description,
            string exerciseTitle, string exerciseDescription, IFormFile? pdfFile, IFormFile? exercisePdf)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return Unauthorized();

            string? lessonPdfPath = null;
            string? exercisePdfPath = null;
            if (pdfFile != null) lessonPdfPath = await SaveFile(pdfFile, "pdf");
            if (exercisePdf != null) exercisePdfPath = await SaveFile(exercisePdf, "exercises");

            var newLesson = new Lesson
            {
                subject_id = subjectId,
                class_id = classId,
                teacher_id = teacher.id,
                title = title,
                description = description ?? "",
                pdf_url = lessonPdfPath,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };
            _context.Lessons.Add(newLesson);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(exerciseTitle))
            {
                _context.Exercises.Add(new Exercise
                {
                    lesson_id = newLesson.id,
                    title = exerciseTitle,
                    description = exerciseDescription ?? "",
                    file_url = exercisePdfPath,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "تم نشر الدرس بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", folder);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folder}/{fileName}";
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            ViewBag.Subjects = await _context.Subjects.AsNoTracking().ToListAsync();
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course, IFormFile? thumbnail)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return Unauthorized();

            if (thumbnail != null)
            {
                course.thumbnail_url = await SaveFile(thumbnail, "courses/thumbnails");
            }

            course.teacher_id = teacher.id;
            course.created_at = DateTime.Now;
            course.is_published = true;

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء الدورة التعليمية بنجاح!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(long id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewBag.Subjects = await _context.Subjects.AsNoTracking().ToListAsync();
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync(); // إضافة القائمة المنسدلة للصف الدراسي
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(long id, Course course, IFormFile? thumbnail)
        {
            if (id != course.id) return NotFound();

            try
            {
                var existingCourse = await _context.Courses.FindAsync(id);
                if (existingCourse == null) return NotFound();

                existingCourse.title = course.title;
                existingCourse.description = course.description;
                existingCourse.price = course.price;
                existingCourse.subject_id = course.subject_id;
                existingCourse.class_id = course.class_id; // تحديث الصف الدراسي
                existingCourse.promo_video_url = course.promo_video_url;

                if (thumbnail != null)
                {
                    existingCourse.thumbnail_url = await SaveFile(thumbnail, "courses/thumbnails");
                }

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث الدورة بنجاح";
            }
            catch (Exception)
            {
                TempData["Error"] = "حدث خطأ أثناء التحديث";
            }
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(long id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الدورة بنجاح";
            return RedirectToAction(nameof(Courses));
        }



        public async Task<IActionResult> MyCourses()
        {
            // 1. جلب معرف المستخدم الحالي (الأستاذ) من الجلسة
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            // 2. جلب دورات الأستاذ
            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.teacher_id == teacher.id)
                .ToListAsync();

            // 3. جلب كل الاشتراكات في دورات هذا الأستاذ لعرضها في المودال
            ViewBag.AllEnrollments = await _context.CourseEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Where(e => e.Course.teacher_id == teacher.id)
                .ToListAsync();

            return View(courses);
        }
        public async Task<IActionResult> Lessons()
        {
            // 1. الحصول على معرف الأستاذ من الجلسة
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            // 2. جلب الدروس المجانية فقط (المكتبة المجانية)
            // وجلب الدروس التي ليست جزءاً من دورة مدفوعة أو التي تم تحديدها كمجانية
            var freeLessons = await _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.Class)
                .Where(l => l.teacher_id == teacher.id && l.is_free == true)
                .OrderByDescending(l => l.created_at)
                .ToListAsync();

            return View(freeLessons);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(long id, string title, string video_url, string pdf_url, string description)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            // تحديث البيانات
            lesson.title = title;
            lesson.video_url = video_url;
            lesson.pdf_url = pdf_url;
            lesson.description = description;
            lesson.updated_at = DateTime.UtcNow;

            _context.Update(lesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث الدرس بنجاح";
            return RedirectToAction(nameof(Lessons));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(Lesson lesson)
        {
            // جلب معرف الأستاذ من السيشن
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher != null)
            {
                lesson.teacher_id = teacher.id;
                lesson.is_free = true; // نحدد أنه مجاني لأنه في المكتبة المجانية
                lesson.created_at = DateTime.UtcNow;
                lesson.updated_at = DateTime.UtcNow;

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم نشر الدرس المجاني بنجاح!";
            }

            return RedirectToAction(nameof(Lessons));
        }






        public async Task<IActionResult> Wallet()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher == null) return NotFound();

            // جلب سجل السحوبات (الجديد)
            ViewBag.WithdrawalHistory = await _context.WithdrawalRequests
                .Where(r => r.TeacherId == teacher.id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // جلب مبيعات الدورات
            var courseSales = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Where(e => e.Course.teacher_id == teacher.id)
                .ToListAsync();

            decimal totalCourseEarnings = courseSales.Sum(e => e.teacher_commission);

            // جلب الحصص المكتملة
            var completedSessions = await _context.EducationalSessions
                .Include(s => s.Lesson).ThenInclude(l => l.Teacher)
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.Lesson.teacher_id == teacher.id && s.status == "Completed")
                .ToListAsync();

            decimal totalSessionEarnings = completedSessions.Sum(s => (s.Lesson?.Teacher?.hourly_price ?? 0m) * 0.50m);

            // تحديث المحفظة
            var wallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == teacher.id);
            if (wallet == null)
            {
                wallet = new TeacherWallet { teacher_id = teacher.id, total_earned = 0, withdrawn_amount = 0 };
                _context.TeacherWallets.Add(wallet);
            }

            wallet.total_earned = totalCourseEarnings + totalSessionEarnings;
            wallet.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            ViewBag.CourseSales = courseSales.OrderByDescending(e => e.enrolled_at).Take(10).ToList();
            ViewBag.SessionEarnings = completedSessions.OrderByDescending(s => s.scheduled_at).Take(10).ToList();

            return View(wallet);
        }

        // 2. ميثود جديدة لتحديث بيانات الـ CCP و RIB (لكي يراها الأدمن)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBankDetails(string ccp, string rib)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher != null)
            {
                teacher.ccp_number = ccp;
                teacher.rib_number = rib;
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث بيانات الحساب البنكي/البريدي بنجاح.";
            }

            return RedirectToAction(nameof(Wallet));
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestWithdrawal(decimal amount)
        {
            // 1. التأكد من هوية الأستاذ
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            // 2. جلب المحفظة
            var wallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == teacher.id);
            if (wallet == null) return BadRequest("المحفظة غير موجودة");

            // 3. حساب الرصيد المتاح
            decimal available = wallet.total_earned - wallet.withdrawn_amount;

            // 4. التحقق من صحة المبلغ
            if (amount > available || amount < 2000)
            {
                TempData["Error"] = "المبلغ غير صالح! يجب أن يكون على الأقل 2000 دج ولا يتجاوز رصيدك المتاح.";
                return RedirectToAction(nameof(Wallet));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 5. إنشاء سجل طلب السحب (هذا هو الجزء الذي كان ناقصاً!)
                    var withdrawalRequest = new WithdrawalRequest
                    {
                        TeacherId = teacher.id,
                        Amount = amount,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.WithdrawalRequests.Add(withdrawalRequest);

                    // 6. تحديث المحفظة (إضافة المبلغ للمسحوبات)
                    wallet.withdrawn_amount += amount;
                    wallet.updated_at = DateTime.UtcNow;
                    _context.Update(wallet);

                    // حفظ التغييرات والالتزام بالمعاملة
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"تم إرسال طلب سحب مبلغ {amount} دج بنجاح.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "حدث خطأ أثناء معالجة الطلب.";
                }
            }

            return RedirectToAction(nameof(Wallet));
        }
    }
}