using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models;

[Table("review")]
public class Review
{
    [Key]
    public long id { get; set; }

    // الشخص الذي قام بالتقييم (التلميذ) - الجدول رقم 6
    [Required]
    public long student_id { get; set; }

    [ForeignKey("student_id")]
    public virtual Student? Student { get; set; }

    // إذا كان التقييم لحصة تعليمية - الجدول رقم 13
    public long? educational_session_id { get; set; }

    [ForeignKey("educational_session_id")]
    public virtual EducationalSession? EducationalSession { get; set; }

    // إذا كان التقييم لمرشد نفسي - الجدول رقم 14
    public long? counsel_session_id { get; set; }

    [ForeignKey("counsel_session_id")]
    public virtual CounselSession? CounselSession { get; set; }

    // التقييم من 1 إلى 5 نجوم
    [Required]
    [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5")]
    public int rating { get; set; }

    // التعليق الكتابي
    public string? comment { get; set; }

    public DateTime created_at { get; set; } = DateTime.Now;
}
