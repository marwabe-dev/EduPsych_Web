using ClosedXML.Excel;
using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduPsych_Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. الصفحة الرئيسية
        public async Task<IActionResult> Index()
        {
            var monthlyEduData = new List<int>();
            var monthlyPsychData = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                // ملاحظة: تأكد من تسمية الحقل في قاعدة البيانات، إذا كان CreatedAt في الموديل استخدمه هنا
                var eduCount = await _context.EducationalSessions
                    .CountAsync(s => s.created_at.Month == targetDate.Month && s.created_at.Year == targetDate.Year);
                var psychCount = await _context.CounselSessions
                    .CountAsync(s => s.created_at.Month == targetDate.Month && s.created_at.Year == targetDate.Year);

                monthlyEduData.Add(eduCount);
                monthlyPsychData.Add(psychCount);
            }

            var model = new AdminDashboardViewModel
            {
                PendingWithdrawalsCount = await _context.WithdrawalRequests.CountAsync(r => r.Status == "Pending"),
                TotalStudents = await _context.Users.CountAsync(u => u.role_id == 2),
                TotalTeachers = await _context.Users.CountAsync(u => u.role_id == 3),
                TotalCounselors = await _context.Users.CountAsync(u => u.role_id == 4),
                TotalParents = await _context.Users.CountAsync(u => u.role_id == 5),
                TotalRevenue = await _context.Payments.Where(p => p.status == "Completed").SumAsync(p => p.amount),
                PaidSessionsCount = await _context.Payments.CountAsync(p => p.status == "Completed"),
                MonthlyEduSessions = monthlyEduData,
                MonthlyPsychSessions = monthlyPsychData,
                TotalEducationalSessions = await _context.EducationalSessions.CountAsync(), // إضافة الحقول الناقصة للـ ViewModel
                TotalCounselSessions = await _context.CounselSessions.CountAsync(),
                RecentEduSessions = await _context.EducationalSessions.Include(s => s.Lesson).OrderByDescending(s => s.created_at).Take(5).ToListAsync(),
                RecentCounselSessions = await _context.CounselSessions.Include(s => s.Counselor).ThenInclude(c => c.User).OrderByDescending(s => s.created_at).Take(5).ToListAsync(),
                RecentPayments = await _context.Payments.OrderByDescending(p => p.id).Take(5).ToListAsync()
            };

            return View(model);
        }

        // 2. إدارة التلاميذ مع البحث
        public async Task<IActionResult> Students(string searchTerm)
        {
            var query = _context.Students.Include(s => s.User).Include(s => s.Class).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.User.first_name.Contains(searchTerm) ||
                                         s.User.last_name.Contains(searchTerm) ||
                                         s.User.email.Contains(searchTerm));
            }

            var students = await query.ToListAsync();
            return View(students);
        }

        // 3. عرض تفاصيل التلميذ
        public async Task<IActionResult> StudentDetails(long id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(m => m.id == id);

            if (student == null) return NotFound();

            var payments = await _context.Payments
                .Where(p => p.student_id == id && p.status == "Completed")
                .OrderByDescending(p => p.created_at)
                .ToListAsync();

            var eduSessions = await _context.EducationalSessions
                .Include(s => s.Lesson)
                .Where(s => s.student_id == id)
                .OrderByDescending(s => s.scheduled_at)
                .ToListAsync();

            var psychSessions = await _context.CounselSessions
                .Include(s => s.Counselor).ThenInclude(c => c.User)
                .Where(s => s.student_id == id)
                .OrderByDescending(s => s.scheduled_at)
                .ToListAsync();

            ViewBag.TotalRevenue = payments.Sum(p => p.amount);
            ViewBag.Payments = payments;
            ViewBag.EduSessions = eduSessions;
            ViewBag.PsychSessions = psychSessions;

            return View(student);
        }

        // 4. البحث الشامل
        [HttpGet]
        public async Task<JsonResult> GlobalSearch(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var searchTerm = term.ToLower().Trim();

            // 1. جلب المستخدمين الذين يطابقون الاسم أو البريد
            var users = await _context.Users
                .Where(u => u.first_name.ToLower().Contains(searchTerm) ||
                            u.last_name.ToLower().Contains(searchTerm) ||
                            u.email.ToLower().Contains(searchTerm))
                .Take(10)
                .ToListAsync();

            var finalResults = new List<object>();

            foreach (var u in users)
            {
                string roleName = "";
                string url = "";

                // ملاحظة: تأكد من أرقام الـ Role IDs في قاعدة بياناتك
                if (u.role_id == 2) // تلميذ
                {
                    roleName = "تلميذ";
                    // هنا استخدمنا id (صغيرة) لأن جدول student لا يزال يستخدم snake_case في الـ SQL
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.user_id == u.id);
                    if (student != null) url = "/Admin/StudentDetails/" + student.id;
                }
                else if (u.role_id == 3) // أستاذ
                {
                    roleName = "أستاذ";
                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == u.id);
                    if (teacher != null) url = "/Admin/StaffDetails/" + teacher.id;
                }
                else if (u.role_id == 4 || u.role_id == 5) // مستشار أو مرشد
                {
                    roleName = "مستشار / مرشد";
                    var counselor = await _context.Counselors.FirstOrDefaultAsync(c => c.user_id == u.id);
                    if (counselor != null) url = "/Admin/StaffDetails/" + counselor.id;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    finalResults.Add(new
                    {
                        name = u.first_name + " " + u.last_name,
                        role = roleName,
                        url = url
                    });
                }
            }

            return Json(finalResults);
        }

        // 5. تفاصيل الطاقم
        public async Task<IActionResult> StaffDetails(long id)
        {
            var teacher = await _context.Teachers.Include(t => t.User).FirstOrDefaultAsync(t => t.id == id);
            var counselor = teacher == null ? await _context.Counselors.Include(c => c.User).FirstOrDefaultAsync(c => c.id == id) : null;
            var user = teacher?.User ?? counselor?.User;
            if (user == null) return NotFound();

            var eduSessions = new List<EducationalSession>();
            var psychSessions = new List<CounselSession>();

            if (teacher != null)
            {
                eduSessions = await _context.EducationalSessions.Include(s => s.Lesson)
                    .Include(s => s.Student).ThenInclude(st => st.User)
                    .Where(s => s.Lesson.teacher_id == id).OrderByDescending(s => s.scheduled_at).ToListAsync();
            }
            else if (counselor != null)
            {
                psychSessions = await _context.CounselSessions.Include(s => s.Student).ThenInclude(st => st.User)
                    .Where(s => s.counselor_id == id).OrderByDescending(s => s.scheduled_at).ToListAsync();
            }

            decimal totalPaidAmount = 0;
            if (teacher != null)
            {
                var eduIds = eduSessions.Select(es => (long?)es.id).ToList();
                totalPaidAmount = await _context.Payments.Where(p => eduIds.Contains(p.educational_session_id) && p.status == "Completed").SumAsync(p => p.amount);
            }
            else
            {
                var psychIds = psychSessions.Select(ps => (long?)ps.id).ToList();
                totalPaidAmount = await _context.Payments.Where(p => psychIds.Contains(p.counsel_session_id) && p.status == "Completed").SumAsync(p => p.amount);
            }

            ViewBag.TotalSales = totalPaidAmount;
            ViewBag.NetProfit = totalPaidAmount * 0.30m;
            ViewBag.StaffEarnings = totalPaidAmount - (decimal)ViewBag.NetProfit;
            ViewBag.EduSessions = eduSessions;
            ViewBag.PsychSessions = psychSessions;
            ViewBag.UserType = teacher != null ? "Teacher" : "Counselor";

            return View(user);
        }

        public IActionResult Staff() => View();
        public IActionResult PsychSupport() => View();
        public IActionResult EduSupport() => View();
        public IActionResult CriticalCases() => View();
        public IActionResult Finance() => View();
        public IActionResult Reports() => View();

        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects.Include(s => s.Stream).ToListAsync();
            return View(subjects);
        }

        public async Task<IActionResult> StaffList()
        {
            var allStaff = new List<object>();
            var teachers = await _context.Teachers.Include(t => t.User).Select(t => new { Id = t.id, FullName = t.User.first_name + " " + t.User.last_name, Email = t.User.email, Type = "Teacher" }).ToListAsync();
            allStaff.AddRange(teachers);
            var counselors = await _context.Counselors.Include(c => c.User).Select(c => new { Id = c.id, FullName = c.User.first_name + " " + c.User.last_name, Email = c.User.email, Type = "Counselor" }).ToListAsync();
            allStaff.AddRange(counselors);
            ViewBag.AllStaff = allStaff;
            return View("Staff");
        }

        public async Task<IActionResult> PsychSupportManagement()
        {
            var counselorsData = await _context.Counselors.Include(c => c.User).Select(c => new
            {
                FullName = c.User.first_name + " " + c.User.last_name,
                SessionCount = _context.CounselSessions.Count(s => s.counselor_id == c.id),
                Revenue = _context.Payments.Where(p => p.counsel_session_id != null && _context.CounselSessions.Any(cs => cs.id == p.counsel_session_id && cs.counselor_id == c.id)).Sum(p => (decimal?)p.amount) ?? 0,
                UserRating = 4.2
            }).ToListAsync();

            var rankedStaff = counselorsData.Select(c => new
            {
                c.FullName,
                c.SessionCount,
                c.Revenue,
                FinalScore = (c.Revenue > 0) ? (double)(c.Revenue / 1000) + (c.SessionCount * 0.5) : (c.SessionCount * 0.2),
                Status = (c.Revenue > 5000) ? "Top Seller" : (c.SessionCount > 10 ? "نشط جداً" : "مبتدئ")
            }).OrderByDescending(x => x.FinalScore).ToList();

            ViewBag.CounselorsStats = rankedStaff;
            return View("PsychSupport");
        }

        public async Task<IActionResult> DownloadReport()
        {
            var data = await _context.Counselors.Include(c => c.User).Select(c => new
            {
                FullName = c.User.first_name + " " + c.User.last_name,
                SessionCount = _context.CounselSessions.Count(s => s.counselor_id == c.id),
                Revenue = _context.Payments.Where(p => p.counsel_session_id != null && _context.CounselSessions.Any(cs => cs.id == p.counsel_session_id && cs.counselor_id == c.id)).Sum(p => (decimal?)p.amount) ?? 0
            }).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("تقرير أداء الدعم النفسي");
                worksheet.RightToLeft = true;
                worksheet.Cell(1, 1).Value = "اسم المرشد"; worksheet.Cell(1, 2).Value = "عدد الجلسات";
                worksheet.Cell(1, 3).Value = "الأرباح"; worksheet.Cell(1, 4).Value = "التصنيف";
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.FullName; worksheet.Cell(row, 2).Value = item.SessionCount;
                    worksheet.Cell(row, 3).Value = item.Revenue;
                    worksheet.Cell(row, 4).Value = (item.Revenue > 5000) ? "أداء مرتفع" : "نشط";
                    row++;
                }
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report.xlsx");
                }
            }
        }

        public async Task<IActionResult> EduAcademicPerformance()
        {
            var teachers = await _context.Teachers.Include(t => t.User).ToListAsync();
            var teacherStats = teachers.Select(t =>
            {
                var ids = _context.Lessons.Where(l => l.teacher_id == t.id).Select(l => l.id).ToList();
                int count = _context.EducationalSessions.Count(s => ids.Contains(s.lesson_id));
                decimal rev = _context.Payments.Where(p => p.educational_session_id != null && _context.EducationalSessions.Any(s => s.id == p.educational_session_id && ids.Contains(s.lesson_id))).Sum(p => (decimal?)p.amount) ?? 0;
                return new { FullName = t.User.first_name + " " + t.User.last_name, LessonCount = count, Revenue = rev, Subject = "دعم بيداغوجي" };
            }).OrderByDescending(x => x.Revenue).ToList();
            ViewBag.TeacherStats = teacherStats;
            return View("EduSupport");
        }

        [HttpGet]
        public async Task<IActionResult> PendingDeposits()
        {
            // جلب طلبات الشحن المعلقة مع بيانات الطالب
            var requests = await _context.DepositRequests
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Where(r => r.status == "Pending")
                .ToListAsync();

            // جلب الدورات وأسعارها (التي أضافها الأساتذة)
            // نفترض أن جدول الدورات اسمه Courses وبه حقل title و price
            ViewBag.CoursePrices = await _context.Courses
                .Select(c => new
                {
                    title = c.title,
                    price = c.price
                })
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeposit(long requestId)
        {
            var request = await _context.DepositRequests.FindAsync(requestId);
            if (request == null || request.status != "Pending") return NotFound();
            request.status = "Approved";
            var wallet = await _context.StudentWallets.FirstOrDefaultAsync(w => w.student_id == request.student_id) ?? new StudentWallet { student_id = request.student_id, balance = 0 };
            if (wallet.id == 0) _context.StudentWallets.Add(wallet);
            wallet.balance += request.amount;
            wallet.updated_at = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PendingDeposits));
        }

        public async Task<IActionResult> FinancialReports()
        {
            var reports = await _context.CourseEnrollments.Include(e => e.Student).ThenInclude(s => s.User).Include(e => e.Course).ThenInclude(c => c.Teacher).ThenInclude(t => t.User).OrderByDescending(e => e.enrolled_at).ToListAsync();
            return View(reports);
        }

        public async Task<IActionResult> WithdrawalRequests()
        {
            return View(await _context.WithdrawalRequests.Include(r => r.Teacher).ThenInclude(t => t.User).OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithdrawal(long requestId, IFormFile receiptFile)
        {
            var request = await _context.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || receiptFile == null) return RedirectToAction(nameof(WithdrawalRequests));
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/receipts");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(receiptFile.FileName);
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create)) { await receiptFile.CopyToAsync(stream); }
            request.Status = "Approved";
            request.ProcessedAt = DateTime.UtcNow;
            request.TransferReceiptUrl = "/uploads/receipts/" + fileName;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(WithdrawalRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWithdrawal(long requestId)
        {
            var request = await _context.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound();
            var wallet = await _context.TeacherWallets.FirstOrDefaultAsync(w => w.teacher_id == request.TeacherId);
            if (wallet != null) { wallet.withdrawn_amount -= request.Amount; wallet.updated_at = DateTime.UtcNow; }
            request.Status = "Rejected";
            request.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(WithdrawalRequests));
        }


        [HttpPost]
        public async Task<IActionResult> ApprovePayment(long paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.CounselSession)
                .FirstOrDefaultAsync(p => p.id == paymentId);

            if (payment == null) return NotFound();

            payment.status = "Approved";

            if (payment.counsel_session_id != null && payment.CounselSession != null)
            {
                var session = payment.CounselSession;
                session.status = "Paid";

                // جلب المحفظة وتحديثها
                var wallet = await _context.CounselorWallets
                    .FirstOrDefaultAsync(w => w.counselor_id == session.counselor_id);

                if (wallet != null)
                {
                    // تقسيم المبلغ: حصة المرشد = المبلغ الكامل / 2
                    decimal share = payment.amount / 2;
                    wallet.total_earned += share;

                    // تحديث بيانات الجلسة للتوثيق
                    session.amount_paid = payment.amount;
                    session.counselor_commission = share;
                    session.platform_commission = share;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("PendingDeposits");
        }




        // عرض قائمة طلبات التوظيف (الأساتذة والمرشدين بانتظار التفعيل)
        public async Task<IActionResult> RecruitmentRequests()
        {
            // جلب المستخدمين الذين سجلوا كأستاذ (3) أو مرشد (5) ولم يتم تفعيلهم بعد
            var pendingExperts = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.is_verified == false && (u.role_id == 3 || u.role_id == 5))
                .OrderByDescending(u => u.created_at)
                .ToListAsync();

            return View(pendingExperts);
        }

        // أكشن "قبول طلب التوظيف"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveExpert(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.is_verified = true; // تفعيل الحساب
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تفعيل حساب الخبير بنجاح، يمكنه الآن الدخول للمنصة.";
            }
            return RedirectToAction(nameof(RecruitmentRequests));
        }
    }
}