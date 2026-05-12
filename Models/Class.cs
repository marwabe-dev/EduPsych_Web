using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models;

[Table("class")]
public class Class
{
    [Key] public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public virtual ICollection<Stream> Streams { get; set; } = new List<Stream>();
}