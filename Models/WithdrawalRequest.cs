using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("withdrawal_requests")]
    public class WithdrawalRequest
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("teacher_id")]
        public long? TeacherId { get; set; }

        [Column("counselor_id")] // تم توحيد الاسم هنا
        public long? CounselorId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("transfer_receipt_url")]
        public string? TransferReceiptUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        // العلاقات - تأكدي أن الأسماء تطابق الخصائص أعلاه
        [ForeignKey("TeacherId")]
        public virtual Teacher? Teacher { get; set; }

        [ForeignKey("CounselorId")]
        public virtual Counselor? Counselor { get; set; }

        // ملاحظة: قمنا بحذف السطر المكرر (public long? counselor_id) لأنه يسبب التعارض
    }
}