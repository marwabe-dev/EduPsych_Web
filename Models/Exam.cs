using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("exam")] // لربطه بجدول exam في PostgreSQL
    public class Exam
    {
        [Key]
        public long id { get; set; }

        public string title { get; set; }

        public string file_url { get; set; }

        public string exam_type { get; set; } // مثل: 'فرض'، 'اختبار'

        public long subject_id { get; set; }

        [ForeignKey("subject_id")]
        public virtual Subject Subject { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;
    }
}