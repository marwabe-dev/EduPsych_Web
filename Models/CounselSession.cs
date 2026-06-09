using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("counsel_session")] // اسم الجدول في قاعدة البيانات
    public class CounselSession
    {
        [Key]
        public long id { get; set; }

        public long counselor_id { get; set; }
        [ForeignKey("counselor_id")]
        public virtual Counselor? Counselor { get; set; }

        public long student_id { get; set; }
        [ForeignKey("student_id")]
        public virtual Student? Student { get; set; }

        public string session_type { get; set; } = string.Empty;
        public DateTime scheduled_at { get; set; }
        public string status { get; set; } = "Pending";

        // تأكد من وجود هذين السطرين بنفس السبيلينج (Spelling)
        public DateTime created_at { get; set; } = DateTime.Now;
        public DateTime updated_at { get; set; } = DateTime.Now;
        public string? meeting_link { get; set; }


        // داخل Models/CounselSession.cs
        public decimal amount_paid { get; set; } // أضف هذا السطر إذا لم يكن موجوداً
        public decimal counselor_commission { get; set; }
        public decimal platform_commission { get; set; }

    }
}