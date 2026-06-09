namespace EduPsych_Web.Models
{
    public class ChatRoom
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string RoomType { get; set; } // 'AI', 'Private', 'Group'
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 🟢 أضف هذا السطر لتعريف العلاقة (هذا ما ينقصك)
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}