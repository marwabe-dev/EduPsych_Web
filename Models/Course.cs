using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("courses")]
    public class Course
    {
        [Key]
        public long id { get; set; }

        [Required]
        public string title { get; set; }

        public string? description { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal price { get; set; }

        // --- الحقل الجديد المضاف لتوزيع الأرباح ---
        [Column("teacher_percentage", TypeName = "numeric(5,2)")]
        public decimal teacher_percentage { get; set; } = 50.00m;

        public string? thumbnail_url { get; set; }

        public string? promo_video_url { get; set; }

        public bool is_published { get; set; } = false;

        public DateTime created_at { get; set; } = DateTime.Now;

        // --- المعرفات الخارجية (Foreign Keys) ---

        public long teacher_id { get; set; }

        public long subject_id { get; set; }

        [Column("class_id")]
        public long? class_id { get; set; }

        // --- علاقات الملاحة (Navigation Properties) ---

        [ForeignKey("teacher_id")]
        public virtual Teacher? Teacher { get; set; }

        [ForeignKey("subject_id")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("class_id")]
        public virtual Class? Class { get; set; }

        public virtual ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    }
}