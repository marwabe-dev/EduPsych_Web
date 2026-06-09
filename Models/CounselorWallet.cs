using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("counselor_wallet")]
    public class CounselorWallet
    {
        [Key]
        [Column("id")]
        public long id { get; set; }

        [Column("counselor_id")]
        public long counselor_id { get; set; } // تأكد أن الاسم هنا صغير بالكامل

        [Column("total_earned")]
        public decimal total_earned { get; set; }

        [Column("withdrawn_amount")]
        public decimal withdrawn_amount { get; set; }

        [Column("updated_at")]
        public DateTime updated_at { get; set; } = DateTime.Now;

        [ForeignKey("counselor_id")]
        public virtual Counselor Counselor { get; set; }
    }
}