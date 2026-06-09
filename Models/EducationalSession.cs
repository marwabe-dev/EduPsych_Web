using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models;

[Table("educational_session")]
public class EducationalSession
{
    [Key] public long id { get; set; }
    public long lesson_id { get; set; } 
    [ForeignKey("lesson_id")] public virtual Lesson? Lesson { get; set; }
    // داخل ملف EducationalSession.cs قم بتعديل هذا السطر فقط:
    public long? student_id { get; set; } // 👈 تحويله إلى اختياري بالـ (?) لكي تقبل قاعدة البيانات حجز الحصة لاحقاً
    [ForeignKey("student_id")] public virtual Student? Student { get; set; }
    public string session_type { get; set; } = "Online"; // Online or In-Person
    public string? location { get; set; }
    public DateTime scheduled_at { get; set; }
    public string status { get; set; } = "Pending";
    public DateTime created_at { get; set; } = DateTime.Now;
    public string? meeting_link { get; set; } // أضيفي هذا السطر
}