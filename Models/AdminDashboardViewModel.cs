namespace EduPsych_Web.Models
{
    public class AdminDashboardViewModel
    {
        // --- إحصائيات المستخدمين ---
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCounselors { get; set; }
        public int TotalParents { get; set; }

        // --- الإحصائيات المالية ---
        public decimal TotalRevenue { get; set; }
        public int PaidSessionsCount { get; set; }

        // --- إحصائيات الجلسات ---
        public int TotalEducationalSessions { get; set; }
        public int TotalCounselSessions { get; set; }

        // --- بيانات الرسوم البيانية (هذا الجزء الذي كان ينقصك) ---
        // قوائم لتخزين عدد الجلسات لكل شهر (مثلاً: [5, 12, 8...])
        public List<int> MonthlyEduSessions { get; set; } = new List<int>();
        public List<int> MonthlyPsychSessions { get; set; } = new List<int>();

        // --- القوائم لعرض الجداول في الصفحة الرئيسية ---
        public List<EducationalSession> RecentEduSessions { get; set; } = new List<EducationalSession>();
        public List<CounselSession> RecentCounselSessions { get; set; } = new List<CounselSession>();
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        public int PendingWithdrawalsCount { get; set; }
    }
}