using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("exercise")]
    public class Exercise
    {
        [Key]
        [Column("id")] // تأكيد اسم العمود الصغير
        public long id { get; set; }

        [Required]
        [Column("lesson_id")] // تأكيد الربط مع lesson_id الصغير
        public long lesson_id { get; set; }

        [ForeignKey("lesson_id")]
        public virtual Lesson? Lesson { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string title { get; set; } = string.Empty;

        [Column("description")]
        public string? description { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime updated_at { get; set; } = DateTime.Now;

        [Column("pdf_url")]
        public string? pdf_url { get; set; }

        [Column("file_url")]
        public string? file_url { get; set; }

        // --- هذا الجزء لحل مشكلة e.Teacherid ---
        [NotMapped]
        public long? Teacherid { get; set; }
        // وضعنا هذا الحقل بـ NotMapped لإخبار EF: 
        // "إذا كنت تبحث عن Teacherid، فهو موجود برمجياً فقط ولا تبحث عنه في SQL"
    }
}