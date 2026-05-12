using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models; // 👈 هذا هو السطر السحري الذي ينظم كل شيء

[Table("administrator")]
public class Administrator
{
    [Key]
    public long id { get; set; }
    public long user_id { get; set; }
    [ForeignKey("user_id")]
    public virtual User? User { get; set; }
}