using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduPsych_Web.Models;



[Table("counselor")]
public class Counselor

{

    [Key]

    public long id { get; set; }



    public long user_id { get; set; }

    [ForeignKey("user_id")]

    public virtual User? User { get; set; }



    // أضف هذا الحقل ليربط الكود بقاعدة البيانات

    public long? specialization_id { get; set; }



    // أضف هذه الخاصية لتمكين الـ Include في الـ Controller

    [ForeignKey("specialization_id")]




    public string? bio { get; set; }

    public string? available_days { get; set; }

    public string? available_hours { get; set; }

    public decimal? hourly_price { get; set; } // أضف علامة الاستفهام هذه

    // تأكد من وجود هذا السطر بالأسفل

    [ForeignKey("specialization_id")]

    public virtual PsychSpecialization? PsychSpecialization { get; set; }





    // داخل ملف Counselor.cs أضف هذه الخصائص:

    [Column("ccp_number")]

    public string? ccp_number { get; set; }



    [Column("rib_number")]

    public string? rib_number { get; set; }

}