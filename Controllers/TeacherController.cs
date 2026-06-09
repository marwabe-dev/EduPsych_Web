using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduPsych_Web.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1️⃣ لوحة التحكم الرئيسية
        // 1️⃣ لوحة التحكم الرئيسية
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

            // أ. جلب الإشعارات
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.teacher_id == currentTeacherId)
                .OrderByDescending(n => n.created_at)
                .Take(10).AsNoTracking().ToListAsync();

            // ب. جلب الطلبات المعلقة (الحصص المباشرة المجدولة من الطلاب التي تنتظر القبول)
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
                                         CName = c.name,
                                         Type = s.session_type // أونلاين أو حضوري
                                     }).AsNoTracking().ToListAsync();

            ViewBag.PendingRequests = pendingData;

            // ج. جلب الحصص القادمة (المقبولة، المدفوعة، أو المؤكدة) 🔥 [تم التعديل هنا] 🔥
            var upcomingData = await (from s in _context.EducationalSessions
                                      join l in _context.Lessons on s.lesson_id equals l.id
                                      join st in _context.Students on s.student_id equals st.id
                                      join u in _context.Users on st.user_id equals u.id
                                      where l.teacher_id == currentTeacherId &&
                                            (s.status == "Accepted" || s.status == "Paid" || s.status == "Confirmed") // شملنا حالة Confirmed
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

            // د. جلب الدروس والتمارين المنشورة في "المكتبة المجانية" فقط
            ViewBag.TeacherLessons = await _context.Lessons
                .Where(l => l.teacher_id == currentTeacherId && l.is_free == true && l.course_id == null)
                .Select(l => new
                {
                    id = l.id,
                    title = l.title,
                    pdf_url = l.pdf_url,
                    ClassName = _context.Classes.Where(c => c.id == l.class_id).Select(c => c.name).FirstOrDefault(),
                    SubjectName = _context.Subjects.Where(sub => sub.id == l.subject_id).Select(sub => sub.name).FirstOrDefault(),
                    Exercises = _context.Exercises.Where(ex => ex.lesson_id == l.id).Select(ex => new { ex.title, ex.file_url }).ToList()
                }).AsNoTracking().ToListAsync();

            // هـ. المحفظة
            var wallet = await _context.TeacherWallets
                .Where(w => w.teacher_id == currentTeacherId)
                .Select(w => new { balance = w.total_earned - w.withdrawn_amount })
                .FirstOrDefaultAsync();

            ViewBag.WalletBalance = wallet?.balance ?? 0;

            // و. القوائم المنسدلة للنماذج (Modals)
            ViewBag.Subjects = await _context.Subjects.AsNoTracking().ToListAsync();
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();

            return View(teacher);
        }

        // 2️⃣ [ميثود مضافة حديثاً] 🔥 أكشن إنشاء حصة تعليمية مباشرة معروضة للحجز
        // 2️⃣ [ميثود محدثة] 🔥 أكشن إنشاء حصة تعليمية مباشرة وجدولتها للحجز
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLiveSession(long subjectId, long classId, string sessionTitle, string description)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return Unauthorized();

            if (string.IsNullOrEmpty(sessionTitle))
            {
                TempData["Error"] = "عنوان الحصة مطلوب.";
                return RedirectToAction(nameof(Index));
            }

            // إنشاء السجل في جدول Lesson كدرس خاص متاح للحجز المباشر (is_free = false)
            var liveLesson = new Lesson
            {
                subject_id = subjectId,
                class_id = classId,
                teacher_id = teacher.id,
                title = sessionTitle,
                description = string.IsNullOrEmpty(description) ? "حصة مخصصة للحجز المباشر" : description,
                is_free = false, // false لكي تظهر في بروفايل الأستاذ كحصة خاصة للحجز وليست في المكتبة العامة
                course_id = null,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            _context.Lessons.Add(liveLesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم نشر عنوان الحصة المباشرة بنجاح، ويمكن للطلاب الآن حجزها واختيار الأوقات المناسبة لهم!";
            return RedirectToAction(nameof(Index));
        }

        // 3️⃣ قبول طلب الحصة (تتحول الحالة إلى Accepted لتظهر للتلميذ كلمة مقبول ليدفع)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptSession(long sessionId)
        {
            var session = await _context.EducationalSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            session.status = "Accepted"; // حالة القبول التي تطلب من التلميذ الدفع
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم قبول الحصة بنجاح، في انتظار قيام التلميذ بالدفع من محفظته.";
            return RedirectToAction(nameof(Index));
        }

        // 4️⃣ رفض طلب الحصة
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

        // 5️⃣ إضافة أو تحديث رابط قوقل ميت (Google Meet) بعد تأكيد الدفع
        // 5️⃣ إضافة أو تحديث رابط قوقل ميت (Google Meet) بعد تأكيد الدفع 🔥 [مُعدلة وموحدة] 🔥
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMeetingLink(long sessionId, string meetingLink)
        {
            var session = await _context.EducationalSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            // السلسلة المترابطة: التأكد من الدفع سواء كانت الحالة Confirmed أو Paid
            if (session.status != "Confirmed" && session.status != "Paid")
            {
                TempData["Error"] = "لا يمكنك إضافة الرابط إلا بعد أن يقوم التلميذ بالدفع أولاً!";
                return RedirectToAction(nameof(Index));
            }

            session.meeting_link = meetingLink;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث رابط قوقل ميت بنجاح ووصل للتلميذ.";
            return RedirectToAction(nameof(Index));
        }
        // 6️⃣ ميثود رفع المحتوى والدروس المجانية (تم تأمينها لكي لا تختلط بالحصص المباشرة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddContent(long subjectId, long classId, string title, string description, string contentType, IFormFile? pdfFile, IFormFile? coverImage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return Unauthorized();

            string? uploadedFilePath = null;
            if (pdfFile != null && pdfFile.Length > 0)
            {
                uploadedFilePath = await SaveFile(pdfFile, contentType.ToLower() + "s");
            }

            string? coverImagePath = "/uploads/pdf/default_cover.jpg";
            if (coverImage != null && coverImage.Length > 0)
            {
                coverImagePath = await SaveFile(coverImage, "covers");
            }

            if (contentType == "Exercise")
            {
                var helperLesson = new Lesson
                {
                    subject_id = subjectId,
                    class_id = classId,
                    teacher_id = teacher.id,
                    title = "[تمرين مضاف] " + title,
                    description = description ?? "",
                    video_url = coverImagePath,
                    is_free = true, // صريح للمكتبة المجانية
                    course_id = null,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };
                _context.Lessons.Add(helperLesson);
                await _context.SaveChangesAsync();

                var newExercise = new Exercise
                {
                    lesson_id = helperLesson.id,
                    title = title,
                    description = description ?? "",
                    file_url = uploadedFilePath,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };
                _context.Exercises.Add(newExercise);
            }
            else if (contentType == "Exam")
            {
                var newExam = new Exam
                {
                    subject_id = subjectId,
                    title = title,
                    file_url = uploadedFilePath!,
                    exam_type = coverImagePath,
                    created_at = DateTime.Now
                };
                _context.Exams.Add(newExam);
            }
            else // إضافة درس مجاني للمكتبة العادية
            {
                var newLesson = new Lesson
                {
                    subject_id = subjectId,
                    class_id = classId,
                    teacher_id = teacher.id,
                    title = title,
                    description = description ?? "",
                    video_url = coverImagePath,
                    pdf_url = uploadedFilePath,
                    pdf_summary_url = uploadedFilePath,
                    is_free = true, // صريحة جداً للمكتبة المجانية
                    course_id = null,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };
                _context.Lessons.Add(newLesson);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم نشر المحتوى بنجاح في المكتبة المجانية!";
            return RedirectToAction(nameof(Lessons));
        }

        // صفحة الملف الشخصي للأستاذ
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

            ViewBag.LessonsCount = await _context.Lessons.CountAsync(l => l.teacher_id == teacher.id && l.is_free == true);

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

        // صفحة الدورات التدريبية المدفوعة
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Class)
                .Where(c => c.teacher_id == teacher.id)
                .OrderByDescending(c => c.created_at)
                .AsNoTracking()
                .ToListAsync();

            return View(courses);
        }

        // صفحة إدارة المكتبة المجانية
        [HttpGet]
        public async Task<IActionResult> Lessons()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            var freeLessons = await _context.Lessons
                .Include(l => l.Subject)
                .Include(l => l.Class)
                .Where(l => l.teacher_id == teacher.id && l.is_free == true && l.course_id == null)
                .OrderByDescending(l => l.created_at)
                .ToListAsync();

            ViewBag.subject_id = new SelectList(await _context.Subjects.AsNoTracking().ToListAsync(), "id", "name");
            ViewBag.class_id = new SelectList(await _context.Classes.AsNoTracking().ToListAsync(), "id", "name");

            return View(freeLessons);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(long id, string title, string description)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                var formId = Request.Form["id"];
                if (!string.IsNullOrEmpty(formId))
                    lesson = await _context.Lessons.FindAsync(Convert.ToInt64(formId));
            }

            if (lesson == null) return NotFound();

            lesson.title = title;
            lesson.description = description;
            lesson.updated_at = DateTime.Now;

            _context.Update(lesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث البيانات بنجاح!";
            return RedirectToAction(nameof(Lessons));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(long id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
            {
                if (!string.IsNullOrEmpty(lesson.pdf_url))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lesson.pdf_url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الدرس بنجاح.";
            }
            else
            {
                TempData["Error"] = "الدرس غير موجود.";
            }

            return RedirectToAction(nameof(Lessons));
        }

        // صفحة عرض محفظة الأستاذ للأرباح
        [HttpGet]
        public async Task<IActionResult> Wallet()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.user_id == userId);

            if (teacher == null) return NotFound();

            // جلب سجل السحوبات
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

            // جلب الحصص المباشرة (التعليمية) التي تم دفعها أو تأكيدها أو اكتمالها 
            // 🚨 التعديل هنا: نحسب كل حصة دفع التلميذ ثمنها للأستاذ (Paid أو Confirmed أو Completed)
            var teacherSessions = await _context.EducationalSessions
                .Include(s => s.Lesson).ThenInclude(l => l.Teacher)
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.Lesson.teacher_id == teacher.id && (s.status == "Completed" || s.status == "Confirmed" || s.status == "Paid"))
                .ToListAsync();

            decimal totalCourseEarnings = courseSales.Sum(e => e.teacher_commission);
            decimal totalSessionEarnings = teacherSessions.Sum(s => (s.Lesson?.Teacher?.hourly_price ?? 0m) * 0.50m);

            // جلب المحفظة
            var wallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == teacher.id);
            if (wallet == null)
            {
                wallet = new TeacherWallet { teacher_id = teacher.id, total_earned = 0, withdrawn_amount = 0 };
                _context.TeacherWallets.Add(wallet);
            }

            // تحديث الإجمالي بشكل صحيح ليشمل الحصص المدفوعة الجديدة
            wallet.total_earned = totalCourseEarnings + totalSessionEarnings;
            wallet.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            ViewBag.CourseSales = courseSales.OrderByDescending(e => e.enrolled_at).Take(10).ToList();
            ViewBag.SessionEarnings = teacherSessions.OrderByDescending(s => s.scheduled_at).Take(10).ToList();

            return View(wallet);
        }

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

        // 2️⃣ تحديث دالة تقديم طلب السحب (RequestWithdrawal) لحماية الرصيد من الخصم المزدوج
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestWithdrawal(decimal amount)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return NotFound();

            var wallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == teacher.id);
            if (wallet == null) return BadRequest("المحفظة غير موجودة");

            decimal available = wallet.total_earned - wallet.withdrawn_amount;

            if (amount > available || amount < 2000)
            {
                TempData["Error"] = "المبلغ غير صالح! يجب أن يكون على الأقل 2000 دج ولا يتجاوز رصيدك المتاح.";
                return RedirectToAction(nameof(Wallet));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var withdrawalRequest = new WithdrawalRequest
                    {
                        TeacherId = teacher.id,
                        Amount = amount,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.WithdrawalRequests.Add(withdrawalRequest);

                    wallet.withdrawn_amount += amount;
                    wallet.updated_at = DateTime.UtcNow;
                    _context.Update(wallet);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"تم إرسال طلب سحب مبلغ {amount} دج بنجاح.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "حدث خطأ أثناء معالجة الطلب.";
                }
            }

            return RedirectToAction(nameof(Wallet));
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
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();
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
                existingCourse.class_id = course.class_id;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileInfo(long id, decimal hourlyPrice, string bio, IFormFile? profileImage)
        {
            ModelState.Remove("User");
            ModelState.Remove("Specialization");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.id == id);

            if (teacher == null) return NotFound("عذراً، لم يتم العثور على الأستاذ.");

            try
            {
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"teacher_{id}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(profileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(fileStream);
                    }

                    if (teacher.User != null)
                    {
                        teacher.User.profile_picture_url = uniqueFileName;
                        _context.Entry(teacher.User).State = EntityState.Modified;
                    }
                }

                teacher.hourly_price = hourlyPrice;
                teacher.bio = bio;
                teacher.updated_at = DateTime.Now;

                _context.Teachers.Update(teacher);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تحديث البيانات بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء الحفظ: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }
    }
}