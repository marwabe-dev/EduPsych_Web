using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("lesson")]
    public class Lesson
    {
        public Lesson()
        {
            Exercises = new HashSet<Exercise>();
            created_at = DateTime.UtcNow;
            updated_at = DateTime.UtcNow;
        }

        [Key]
        [Column("id")]
        public long id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string title { get; set; } = string.Empty;

        [Column("description")]
        public string? description { get; set; }

        [Column("pdf_url")]
        public string? pdf_url { get; set; }

        [Column("video_url")] // إضافة الماركر ليتوافق مع قاعدة البيانات
        public string? video_url { get; set; }

        [Required]
        [Column("teacher_id")]
        public long teacher_id { get; set; }

        [ForeignKey("teacher_id")]
        public virtual Teacher? Teacher { get; set; }

        [Required]
        [Column("subject_id")]
        public long subject_id { get; set; }

        [ForeignKey("subject_id")]
        public virtual Subject? Subject { get; set; }

        [Required]
        [Column("class_id")]
        public long class_id { get; set; }

        [ForeignKey("class_id")]
        public virtual Class? Class { get; set; }

        // --- الإضافة الجديدة هنا لحل الخطأ CS1061 ---
        [Column("course_id")]
        public long? course_id { get; set; }

        [ForeignKey("course_id")]
        public virtual Course? Course { get; set; }
        // ------------------------------------------
        // أضفه إذا لم يكن موجوداً
        // ابحثي عن هذا الجزء في ملف Lesson.cs وقومي بتحديثه هكذا:
        [Column("pdf_summary_url")] // 🛑 إضافة الماركر ليرتبط بالعمود الفعلي في PostgreSQL
        public string? pdf_summary_url { get; set; }

        [Column("is_free")] // 🛑 تأمين حقل الفرز المجاني ليرتبط بالعمود الفعلي
        public bool is_free { get; set; } = true;
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime updated_at { get; set; }
      
        public virtual ICollection<Exercise> Exercises { get; set; }
    }
}