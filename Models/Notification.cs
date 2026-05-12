using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("notification")]
public class Notification
{
    [Key] public long id { get; set; }
    public long? student_id { get; set; }
    public long? teacher_id { get; set; }
    public long? counselor_id { get; set; }
    public long? parent_id { get; set; }
    public string type { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public bool is_read { get; set; } = false;
    public DateTime created_at { get; set; } = DateTime.Now;
}
