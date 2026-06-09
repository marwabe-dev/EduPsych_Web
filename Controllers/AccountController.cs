using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            var cleanEmail = email.Trim().ToLower();
            var cleanPassword = password.Trim();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.email.ToLower() == cleanEmail && u.password == cleanPassword);

            if (user != null)
            {
                // حظر الأساتذة والمرشدين غير المفعلين
                if ((user.role_id == 3 || user.role_id == 5) && user.is_verified == false)
                {
                    ViewBag.Error = "عذراً، حسابك  قيد المراجعة. لا يمكنك الدخول حتى يتم دراسة ملفك وقبوله من طرف الإدارة.";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", (int)user.id);
                HttpContext.Session.SetString("UserName", $"{user.first_name} {user.last_name}");
                HttpContext.Session.SetString("UserRole", user.Role?.name ?? "");

                switch (user.role_id)
                {
                    case 1: case 6: return RedirectToAction("Index", "Admin");
                    case 2: return RedirectToAction("Index", "Student");
                    case 3: return RedirectToAction("Index", "Teacher");
                    case 4: return RedirectToAction("Index", "Parent");
                    case 5: return RedirectToAction("Index", "Counselor");
                    default: return RedirectToAction("Index", "Home");
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
            await LoadRegisterData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string FullName, string Email, string Phone, string Password, int RoleId,
            long? ClassId, long? SpecializationId, long? StudentId, long? CounselorSpecId, IFormFile? docFile)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.email.ToLower() == Email.Trim().ToLower()))
                {
                    ViewBag.Error = "هذا البريد الإلكتروني مسجل بالفعل";
                    await LoadRegisterData();
                    return View();
                }

                // معالجة رفع الملف (الشهادات/CV)
                string? documentUrl = null;
                if (docFile != null && docFile.Length > 0)
                {
                    // تأكدي من وجود المجلد: wwwroot/uploads/documents
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(docFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await docFile.CopyToAsync(fileStream);
                    }
                    documentUrl = "/uploads/documents/" + fileName;
                }

                var names = FullName.Trim().Split(' ');
                string firstName = names[0];
                string lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : " ";

                bool autoVerify = (RoleId == 2 || RoleId == 4);

                var newUser = new User
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = Email.Trim().ToLower(),
                    password = Password.Trim(),
                    phone_number = Phone,
                    role_id = RoleId,
                    is_verified = autoVerify,
                    document_url = documentUrl, // حفظ رابط الملف هنا
                    created_at = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // توزيع البيانات على الجداول الفرعية
                if (RoleId == 2)
                {
                    _context.Students.Add(new Student { user_id = newUser.id, class_id = ClassId ?? 0 });
                }
                else if (RoleId == 3)
                {
                    _context.Teachers.Add(new Teacher { user_id = newUser.id, specialization_id = SpecializationId });
                }
                else if (RoleId == 4)
                {
                    _context.Parents.Add(new Parent { user_id = newUser.id, student_id = StudentId, created_at = DateTime.Now });
                }
                else if (RoleId == 5)
                {
                    _context.Counselors.Add(new Counselor { user_id = newUser.id, specialization_id = CounselorSpecId });
                }

                await _context.SaveChangesAsync();

                if (!autoVerify)
                {
                    TempData["Warning"] = "تم استلام طلبك بنجاح. يرجى انتظار مراجعة الإدارة لشهاداتك وتفعيل حسابك.";
                }

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "حدث خطأ أثناء التسجيل: " + ex.Message;
                await LoadRegisterData();
                return View();
            }
        }

        private async Task LoadRegisterData()
        {
            ViewBag.Classes = await _context.Classes.AsNoTracking().ToListAsync();
            ViewBag.TeacherSpecs = await _context.specialization.AsNoTracking().ToListAsync();
            ViewBag.CounselorSpecs = await _context.PsychSpecializations.AsNoTracking().ToListAsync();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}