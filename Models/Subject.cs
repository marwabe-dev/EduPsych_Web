using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;

[Table("subject")]
public class Subject
{
    [Key] public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public long stream_id { get; set; }
    [ForeignKey("stream_id")] public virtual Stream? Stream { get; set; }
}