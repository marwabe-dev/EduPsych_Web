using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduPsych_Web.Data;
using EduPsych_Web.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
            var counselor = await _context.Counselors
                .FirstOrDefaultAsync(c => c.user_id == (long)userId);

            // جلب المحفظة من الجدول الجديد counselor_wallet
            var wallet = await _context.CounselorWallets
                .FirstOrDefaultAsync(w => w.counselor_id == counselor.id);

            // حساب الرصيد المتاح (الإجمالي - المسحوب)
            ViewBag.WalletBalance = (wallet?.total_earned ?? 0) - (wallet?.withdrawn_amount ?? 0);

            // جلب الجلسات (بناءً على scheduled_at كما في SQL)
            var allSessions = await _context.CounselSessions
                .Include(s => s.Student).ThenInclude(st => st.User)
                .Where(s => s.counselor_id == counselor.id)
                .OrderByDescending(s => s.scheduled_at)
                .ToListAsync();

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptSession(long sessionId)
        {
            var session = await _context.CounselSessions
                .Include(s => s.Counselor)
                .FirstOrDefaultAsync(s => s.id == sessionId);

            if (session == null) return NotFound();

            session.status = "Accepted";
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
                    status = "Pending",
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

        // --- 6. وظيفة طلب سحب الأرباح (جديد) ---
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
                    CounselorId = counselor.id, // تأكد من الاسم الصغير حسب SQL
                    Amount = amount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                // تحديث مبلغ المسحوبات في المحفظة فوراً لتعليق الرصيد
                wallet.withdrawn_amount += amount;

                _context.WithdrawalRequests.Add(request);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "تم إرسال طلب السحب بنجاح" });
            }
            return Json(new { success = false, message = "الرصيد غير كافٍ أو المبلغ غير مسموح به" });
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
    }
}