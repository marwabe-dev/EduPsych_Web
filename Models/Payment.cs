using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("payment")]
public class Payment
{
    [Key] public long id { get; set; }
    public long student_id { get; set; }
    public long? educational_session_id { get; set; }
    public long? counsel_session_id { get; set; }
    public decimal amount { get; set; }
    public string status { get; set; } = "Pending";
    public DateTime created_at { get; set; } = DateTime.Now;
    public DateTime updated_at { get; set; } = DateTime.Now;



    [ForeignKey("counsel_session_id")]
    public virtual CounselSession CounselSession { get; set; }
}