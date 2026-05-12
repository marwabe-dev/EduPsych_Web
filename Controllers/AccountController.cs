using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EduPsych_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. تسجيل الدخول (Login)
        // ==========================================
        [HttpGet]
        public IActionResult Login() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "يرجى إدخال البريد الإلكتروني وكلمة المرور";
                return View();
            }

            // تنظيف البيانات المدخلة
            var cleanEmail = email.Trim().ToLower();
            var cleanPassword = password.Trim();

            // البحث عن المستخدم باستخدام LINQ (أضمن وأسرع)
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.email.ToLower() == cleanEmail && u.password == cleanPassword);

            if (user != null)
            {
                // حفظ البيانات في الجلسة
                HttpContext.Session.SetInt32("UserId", (int)user.id);
                HttpContext.Session.SetString("UserName", $"{user.first_name} {user.last_name}");

                // توجيه المستخدم بناءً على رقم الرتبة (Role ID)
                // ملاحظة: تأكد من مطابقة هذه الأرقام مع جدول roles في قاعدة البيانات
                switch (user.role_id)
                {
                    case 1:
                    case 6: // أضفنا الرقم 6 لأن الأدمن عندك يحمل هذا الرقم
                        HttpContext.Session.SetString("UserRole", "Admin");
                        return RedirectToAction("Index", "Admin");

                    case 2: // Student
                        HttpContext.Session.SetString("UserRole", "Student");
                        return RedirectToAction("Index", "Student");

                    case 3: // Teacher
                        HttpContext.Session.SetString("UserRole", "Teacher");
                        return RedirectToAction("Index", "Teacher");

                    case 4: // Parent
                        HttpContext.Session.SetString("UserRole", "Parent");
                        return RedirectToAction("Index", "Parent");

                    case 5: // Counselor
                        HttpContext.Session.SetString("UserRole", "Counselor");
                        return RedirectToAction("Index", "Counselor");

                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "البريد الإلكتروني أو كلمة المرور غير صحيحة";
            return View();
        }

        // ==========================================
        // 2. إنشاء حساب جديد (Register)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // 1. جلب الصفوف (للطالب)
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();

            // 2. جلب تخصصات الأساتذة (من جدول specialization)
            ViewBag.TeacherSpecs = await _context.specialization.AsNoTracking().ToListAsync();

            // 3. جلب تخصصات المرشدين (من جدول psych_specialization)
            ViewBag.CounselorSpecs = await _context.PsychSpecializations.AsNoTracking().ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string FullName, string Email, string Phone, string Password, int RoleId, long? ClassId)
        {
            try
            {
                // 1. التحقق من وجود البريد الإلكتروني
                if (await _context.Users.AnyAsync(u => u.email.ToLower() == Email.Trim().ToLower()))
                {
                    ViewBag.Error = "هذا البريد الإلكتروني مسجل بالفعل";
                    ViewBag.Classes = await _context.Classes.ToListAsync();
                    return View();
                }

                // 2. تقسيم الاسم
                var names = FullName.Trim().Split(' ');
                string firstName = names[0];
                string lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : " ";

                // 3. إنشاء كائن المستخدم (تعديل phone إلى phone_number)
                var newUser = new User
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = Email.Trim().ToLower(),
                    password = Password.Trim(),

                    // التعديل هنا: نستخدم phone_number ليطابق العمود الجديد في PostgreSQL والموديل
                    phone_number = Phone,

                    role_id = RoleId,
                    created_at = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 4. إنشاء السجل الفرعي بناءً على الدور (Student, Teacher, etc.)
                if (RoleId == 2) // Student
                {
                    var student = new Student { user_id = newUser.id, class_id = ClassId ?? 0 };
                    _context.Students.Add(student);
                }
                else if (RoleId == 3) // Teacher
                {
                    var teacher = new Teacher { user_id = newUser.id, hourly_price = 0 };
                    _context.Teachers.Add(teacher);

                    // ملاحظة: لا حاجة لإضافة phone_number هنا لأننا وضعناه في كائن newUser (جدول users)
                }
                else if (RoleId == 4) // Parent
                {
                    var parent = new Parent { user_id = newUser.id, created_at = DateTime.Now };
                    _context.Parents.Add(parent);
                }
                else if (RoleId == 5) // Counselor
                {
                    var counselor = new Counselor { user_id = newUser.id, hourly_price = 0 };
                    _context.Counselors.Add(counselor);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "حدث خطأ أثناء التسجيل: " + ex.Message;
                ViewBag.Classes = await _context.Classes.ToListAsync();
                return View();
            }
        }

        // ==========================================
        // 3. تسجيل الخروج (Logout)
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}