using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("stream")]
public class Stream
{
    [Key] public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public long class_id { get; set; }
    [ForeignKey("class_id")] public virtual Class? Class { get; set; }
}