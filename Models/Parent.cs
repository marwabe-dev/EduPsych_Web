using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models;

[Table("parent")]
public class Parent
{
    [Key]
    public long id { get; set; }

    // الربط مع جدول المستخدمين (الجدول رقم 2)
    [Required]
    public long user_id { get; set; }

    [ForeignKey("user_id")]
    public virtual User? User { get; set; }

    // الربط مع التلميذ (الجدول رقم 6) - لمتابعة ابنه
    public long? student_id { get; set; }

    [ForeignKey("student_id")]
    public virtual Student? Student { get; set; }

    public DateTime created_at { get; set; } = DateTime.Now;

    public DateTime updated_at { get; set; } = DateTime.Now;
}