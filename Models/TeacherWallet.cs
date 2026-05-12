using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("teacher_wallet")] // يجب أن يطابق الاسم في SQL تماماً
    public class TeacherWallet
    {
        [Key] public long id { get; set; }
        public long teacher_id { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal total_earned { get; set; } = 0.00m;
        public decimal pending_clearance { get; set; } = 0.00m;
        public decimal withdrawn_amount { get; set; } = 0.00m;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [ForeignKey("teacher_id")]
        public virtual Teacher? Teacher { get; set; }
    }
}