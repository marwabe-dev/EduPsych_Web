using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("users")]
public class User
{
    [Key] public long id { get; set; }
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string? phone_number { get; set; }
    public long role_id { get; set; }
    [ForeignKey("role_id")] public virtual Role? Role { get; set; }
    public DateTime created_at { get; set; } = DateTime.Now;
    public string? phone { get; set; }
    public string? profile_picture_url { get; set; }
    [Column("is_verified")] // هذا السطر يربط الكود باسم العمود في قاعدة البيانات
    public bool is_verified { get; set; } = false;
    public string? document_url { get; set; } // سيبقى فارغاً للتلاميذ والأولياء
}