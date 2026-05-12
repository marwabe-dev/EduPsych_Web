using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduPsych_Web.Models
{
    [Table("teacher")]
    public class Teacher
    {
        [Key]
        [Column("id")]
        public long id { get; set; }

        [Column("user_id")]
        public long user_id { get; set; }

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }

        [Column("specialization_id")]
        public long? specialization_id { get; set; }

        [ForeignKey("specialization_id")]
        public virtual specialization? Specialization { get; set; }

        [Column("bio")]
        public string? bio { get; set; }

        [Column("available_days")]
        public string? available_days { get; set; }

        [Column("available_hours")]
        public string? available_hours { get; set; }

        [Column("hourly_price")]
        public decimal hourly_price { get; set; }

        [Column("years_of_experience")]
        public int years_of_experience { get; set; }

        [Column("video_intro_url")]
        public string? video_intro_url { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime updated_at { get; set; } = DateTime.UtcNow;

        // علاقات صحيحة (يوجد teacher_id في الجداول المقابلة)
        public virtual ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
        public virtual ICollection<Course> Courses { get; set; } = new HashSet<Course>();

        public string? ccp_number { get; set; } // رقم الحساب الجاري
        public string? rib_number { get; set; } // رقم الحساب البنكي أو الـ RIP
    }
}