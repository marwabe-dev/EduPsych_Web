using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;


    [Table("roles")]
    public class Role
    {
        [Key] public long id { get; set; }
        [Required] public string name { get; set; } = string.Empty;
    }
