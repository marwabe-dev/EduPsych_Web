using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EduPsych_Web.Controllers
{
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض لوحة تحكم الولي
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null || userRole != "Parent")
            {
                return RedirectToAction("Login", "Account");
            }

            _context.ChangeTracker.Clear();

            // جلب بيانات الولي مع بيانات الطالب المرتبط به (إن وجد)
            var parentProfile = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.user_id == (long)userId);

            if (parentProfile == null)
            {
                parentProfile = new Parent { user_id = (long)userId };
                _context.Parents.Add(parentProfile);
                await _context.SaveChangesAsync();
            }

            return View(parentProfile);
        }

        // 2. ميثود ربط الولي بالطالب عن طريق الإيميل (إعادة التفعيل)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkStudentByEmail(string studentEmail)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(studentEmail))
            {
                TempData["Error"] = "يرجى إدخال البريد الإلكتروني للطالب.";
                return RedirectToAction("Index");
            }

            // البحث عن الطالب باستخدام الإيميل في جدول Users
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.email.Trim().ToLower() == studentEmail.Trim().ToLower());

            if (student == null)
            {
                TempData["Error"] = "عذراً، لم يتم العثور على طالب بهذا البريد الإلكتروني.";
                return RedirectToAction("Index");
            }

            // جلب سجل الولي الحالي لتحديثه
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.user_id == (long)userId);

            if (parent != null)
            {
                parent.student_id = student.id; // عملية الربط
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