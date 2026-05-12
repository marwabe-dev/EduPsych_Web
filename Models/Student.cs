using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("student")]
public class Student
{
    [Key] public long id { get; set; }
    public long user_id { get; set; }
    [ForeignKey("user_id")] public virtual User? User { get; set; }
    public long class_id { get; set; }
    [ForeignKey("class_id")] public virtual Class? Class { get; set; }

}