using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models;

[Table("psych_specialization")] // 👈 هذا السطر هو الحل، تأكد أنه مكتوب بالحروف الصغيرة تماماً كما في pgAdmin
public class PsychSpecialization
{
    [Key]
    public long id { get; set; }
    public string name { get; set; }
}