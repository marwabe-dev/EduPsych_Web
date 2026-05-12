using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("student_wallets")]
    public class StudentWallet
    {
        [Key]
        public long id { get; set; }
        public long student_id { get; set; }
        public decimal balance { get; set; }
        public DateTime updated_at { get; set; } = DateTime.Now;

        [ForeignKey("student_id")]
        public virtual Student? Student { get; set; }
    }
}