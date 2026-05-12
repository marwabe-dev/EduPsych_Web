using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("deposit_requests")]
    public class DepositRequest
    {
        [Key]
        public long id { get; set; }
        public long student_id { get; set; }
        public decimal amount { get; set; }
        public string receipt_image_url { get; set; } = string.Empty;
        public string status { get; set; } = "Pending";
        public DateTime created_at { get; set; } = DateTime.Now;

        [ForeignKey("student_id")]
        public virtual Student? Student { get; set; }
    }
}