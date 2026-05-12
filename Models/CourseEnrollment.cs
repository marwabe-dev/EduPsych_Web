using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("course_enrollments")] // التأكد من مطابقة اسم الجدول في SQL
    public class CourseEnrollment
    {
        public long id { get; set; }
        public long student_id { get; set; }
        public long course_id { get; set; }
        public decimal amount_paid { get; set; }
        public decimal platform_commission { get; set; }
        public decimal teacher_commission { get; set; }
        public DateTime enrolled_at { get; set; } // تأكد من هذه الكلمة

        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
    }
}