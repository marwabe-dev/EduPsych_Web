using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduPsych_Web.Controllers
{
    public class CounselorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CounselorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. عرض لوحة تحكم المرشد والجلسات (مع المحفظة) ---
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var counselor = await _context.Counselors
                .FirstOrDefaultAsync(c => c.user_id == (long)userId);

            if (counselor == null) return NotFound();

            // جلب المحفظة من الجدول الجديد counselor_wallet
            var wallet = await _context.CounselorWallets
                .FirstOrDefaultAsync(w => w.counselor_id == counselor.id);

            // حساب الرصيد المتاح (الإجمالي - المسحوب)
            ViewBag.WalletBalance = (wallet?.total_earned ?? 0) - (wallet?.withdrawn_amount ?? 0);

            // جلب الجلسات بالكامل للمرشد الحالي
            var allSessions = await _context.CounselSessions
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.counselor_id == counselor.id)
                .OrderByDescending(s => s.scheduled_at)
                .ToListAsync();

            // 📊 ---------------------------------------------------------
            // [إضافة برمجية جوهرية] حساب الإحصائيات وإرسالها لكروت الـ View
            // ---------------------------------------------------------
            var today = DateTime.Today;

            ViewBag.TotalSessions = allSessions.Count;

            // حساب الحجوزات الجديدة بانتظار التأكيد (Pending)
            ViewBag.PendingSessions = allSessions.Count(s => s.status == "Pending");

            // حساب جلسات اليوم (المطابقة لتاريخ اليوم الحالي)
            ViewBag.TodaySessions = allSessions.Count(s => s.scheduled_at.Date == today);

            // حساب الجلسات القادمة المعتمدة (Approved أو Paid وتاريخها مستقبلي)
            ViewBag.UpcomingSessions = allSessions.Count(s => (s.status == "Approved" || s.status == "Paid") && s.scheduled_at >= DateTime.Now);

            return View(allSessions);
        }
        // --- 2. تحديث الحالة عبر AJAX ---
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(long id, string status)
        {
            var session = await _context.CounselSessions.FindAsync(id);
            if (session == null)
            {
                return Json(new { success = false, message = "الجلسة غير موجودة" });
            }

            session.status = status;
            session.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, newStatus = status });
        }

        // --- 3. قبول الجلسة وإنشاء سجل دفع ---
        // --- 1. تحديث دالة قبول الجلسة بالاسم الصحيح الموحد "Approved" ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptSession(long sessionId)
        {
            var session = await _context.CounselSessions
                .Include(s => s.Counselor)
                .FirstOrDefaultAsync(s => s.id == sessionId);

            if (session == null) return NotFound();

            session.status = "Approved"; // 🌟 تم التوحيد إلى Approved بدلاً من Accepted
            session.updated_at = DateTime.Now;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.counsel_session_id == sessionId);

            if (payment == null)
            {
                var newPayment = new Payment
                {
                    student_id = session.student_id,
                    counsel_session_id = session.id,
                    amount = session.Counselor?.hourly_price ?? 0,
                    status = "Pending", // دفع الطالب لا يزال معلقاً
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };
                _context.Payments.Add(newPayment);
            }
            else
            {
                payment.amount = session.Counselor?.hourly_price ?? 0;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // --- 4. تحديث رابط الجلسة (Zoom/Google Meet) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCounselingLink(long sessionId, string meetingLink)
        {
            var session = await _context.CounselSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            session.meeting_link = meetingLink;
            session.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ رابط الجلسة بنجاح.";
            return RedirectToAction("Index");
        }

        // --- 5. وظيفة تحديث بيانات الحساب المالي (جديد) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBankDetails(string ccp, string rib)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.user_id == (long)userId);

            if (counselor != null)
            {
                counselor.ccp_number = ccp;
                counselor.rib_number = rib;
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث بيانات الحساب المالي بنجاح.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RequestWithdrawal(decimal amount)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.user_id == (long)userId);
            var wallet = await _context.CounselorWallets.FirstOrDefaultAsync(w => w.counselor_id == counselor.id);

            decimal available = (wallet?.total_earned ?? 0) - (wallet?.withdrawn_amount ?? 0);

            if (amount >= 2000 && amount <= available)
            {
                var request = new WithdrawalRequest
                {
                    CounselorId = counselor.id, // نضع المعرف في خانة المرشد
                    TeacherId = null,           // نترك خانة الأستاذ فارغة
                    Amount = amount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                wallet.withdrawn_amount += amount;
                _context.WithdrawalRequests.Add(request);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم إرسال طلب السحب بنجاح" });
            }
            return Json(new { success = false, message = "الرصيد غير كافٍ" });
        }

        public async Task<IActionResult> FinancialManagement()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.user_id == (long)userId);

            if (counselor == null) return NotFound();

            // 1. جلب المحفظة (الرصيد)
            var wallet = await _context.CounselorWallets.FirstOrDefaultAsync(w => w.counselor_id == counselor.id);

            // 2. جلب سجل الحجوزات المدفوعة فقط (أو التي لها سجل دفع)
            var paidSessions = await _context.CounselSessions
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.counselor_id == counselor.id && s.status == "Paid")
                .OrderByDescending(s => s.scheduled_at)
                .ToListAsync();

            ViewBag.WalletBalance = (wallet?.total_earned ?? 0) - (wallet?.withdrawn_amount ?? 0);
            ViewBag.Counselor = counselor;

            return View(paidSessions);
        }




        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null) return RedirectToAction("Login", "Account");

            long userId = Convert.ToInt64(sessionUserId);

            // جلب المرشد مع بيانات المستخدم (User) والتخصص (PsychSpecialization)
            var counselor = await _context.Counselors
                .Include(c => c.User)
                .Include(c => c.PsychSpecialization) // تأكدي أن هذا هو اسم العلاقة في الموديل
                .FirstOrDefaultAsync(c => c.user_id == userId);

            if (counselor == null) return NotFound();

            return View(counselor);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Counselor updatedCounselor, string ccp, string rib, string phone, string phone_number, IFormFile? profilePicture)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null) return RedirectToAction("Login", "Account");

            long userId = Convert.ToInt64(sessionUserId);

            // جلب بيانات المرشد مع بيانات المستخدم (User)
            var counselor = await _context.Counselors
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.user_id == userId);

            if (counselor != null && counselor.User != null)
            {
                // 1. تحديث أرقام الهاتف في جدول Users
                counselor.User.phone = phone;
                counselor.User.phone_number = phone_number;

                // 2. معالجة رفع الصورة الشخصية
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    // تحديد مسار المجلد (wwwroot/uploads/profiles)
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    // إنشاء اسم فريد للصورة لمنع التكرار
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }

                    // حفظ مسار الصورة في قاعدة البيانات
                    counselor.User.profile_picture_url = "/uploads/profiles/" + fileName;

                    // 🌟 تحديث الـ Session فوراً لكي تظهر الصورة في الهيدر
                    HttpContext.Session.SetString("UserProfilePicture", counselor.User.profile_picture_url);
                }

                // 3. تحديث بيانات المرشد الأخرى
                counselor.hourly_price = updatedCounselor.hourly_price;
                counselor.available_days = updatedCounselor.available_days;
                counselor.bio = updatedCounselor.bio;
                counselor.ccp_number = ccp;
                counselor.rib_number = rib;

                

                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث البيانات والصورة بنجاح.";

                // تحديث اسم المستخدم في الـ Session أيضاً لضمان مطابقة الهيدر لأي تعديل
                HttpContext.Session.SetString("UserName", counselor.User.first_name + " " + counselor.User.last_name);
            }

            return RedirectToAction(nameof(MyProfile));
        }






        [HttpGet]
        public async Task<IActionResult> WithdrawalHistory()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // جلب بيانات المرشد
            var counselor = await _context.Counselors
                .FirstOrDefaultAsync(c => c.user_id == (long)userId);

            if (counselor == null) return NotFound();

            // جلب السجلات باستخدام الحقل الجديد CounselorId
            var history = await _context.WithdrawalRequests
                .Where(w => w.CounselorId == counselor.id) // التعديل هنا ليتطابق مع الموديل الجديد
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(history);
        }

    }
}