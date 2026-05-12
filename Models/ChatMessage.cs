namespace EduPsych_Web.Models
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public long RoomId { get; set; }
        public long? SenderId { get; set; }
        public string Content { get; set; }
        public bool IsAiGenerated { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;

        // 🟢 المراجع للملاحة (Navigation Properties)
        public virtual ChatRoom Room { get; set; }
        public virtual User Sender { get; set; }
    }
}