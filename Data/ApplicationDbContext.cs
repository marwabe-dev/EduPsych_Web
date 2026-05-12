using Microsoft.EntityFrameworkCore;
using EduPsych_Web.Models;

namespace EduPsych_Web.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 1️⃣ جداول الهوية والأدوار
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<Parent> Parents { get; set; }

        // 2️⃣ جداول الهيكل التعليمي
        public DbSet<Class> Classes { get; set; }
        public DbSet<EduPsych_Web.Models.Stream> Streams { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<specialization> specialization { get; set; }

        // 3️⃣ جداول المستخدمين المتخصصين
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Counselor> Counselors { get; set; }

        // 4️⃣ جداول المحتوى التعليمي
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Course> Courses { get; set; }

        // 5️⃣ جداول الجلسات والعمليات والأنظمة المالية
        public DbSet<EducationalSession> EducationalSessions { get; set; }
        public DbSet<CounselSession> CounselSessions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PsychSpecialization> PsychSpecializations { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<TeacherWallet> TeacherWallets { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<StudentWallet> StudentWallets { get; set; }
        public DbSet<DepositRequest> DepositRequests { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<CounselorWallet> CounselorWallets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1️⃣ إعدادات أسماء الجداول لـ PostgreSQL (snake_case) ---
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Student>().ToTable("student");
            modelBuilder.Entity<Teacher>().ToTable("teacher");
            modelBuilder.Entity<Counselor>().ToTable("counselor");
            modelBuilder.Entity<Parent>().ToTable("parent");
            modelBuilder.Entity<Lesson>().ToTable("lesson");
            modelBuilder.Entity<EducationalSession>().ToTable("educational_session");
            modelBuilder.Entity<CounselSession>().ToTable("counsel_session");
            modelBuilder.Entity<Payment>().ToTable("payment");
            modelBuilder.Entity<Notification>().ToTable("notification");
            modelBuilder.Entity<ChatMessage>().ToTable("chat_message");
            modelBuilder.Entity<Subject>().ToTable("subject");
            modelBuilder.Entity<EduPsych_Web.Models.Stream>().ToTable("stream");
            modelBuilder.Entity<Class>().ToTable("class");
            modelBuilder.Entity<Exercise>().ToTable("exercise");
            modelBuilder.Entity<Exam>().ToTable("exam");
            modelBuilder.Entity<TeacherWallet>().ToTable("teacher_wallet");
            modelBuilder.Entity<Course>().ToTable("courses");
            modelBuilder.Entity<StudentWallet>().ToTable("student_wallets");
            modelBuilder.Entity<DepositRequest>().ToTable("deposit_requests");

            // --- 2️⃣ إعدادات جدول اشتراكات الدورات والتقارير المالية ---
            modelBuilder.Entity<CourseEnrollment>(entity =>
            {
                entity.ToTable("course_enrollments");
                entity.Property(e => e.student_id).HasColumnName("student_id");
                entity.Property(e => e.course_id).HasColumnName("course_id");
                entity.Property(e => e.amount_paid).HasPrecision(10, 2).HasColumnName("amount_paid");
                entity.Property(e => e.platform_commission).HasPrecision(10, 2).HasColumnName("platform_commission");
                entity.Property(e => e.teacher_commission).HasPrecision(10, 2).HasColumnName("teacher_commission");
                entity.Property(e => e.enrolled_at).HasColumnName("enrolled_at");

                entity.HasOne(d => d.Student)
                      .WithMany()
                      .HasForeignKey(d => d.student_id);

                entity.HasOne(d => d.Course)
                      .WithMany()
                      .HasForeignKey(d => d.course_id);
            });

            // --- 3️⃣ إعدادات جدول الدورات (Courses) ---
            modelBuilder.Entity<Course>(entity =>
            {
                entity.Property(c => c.price).HasPrecision(10, 2);
                entity.Property(c => c.teacher_id).HasColumnName("teacher_id");
                entity.Property(c => c.subject_id).HasColumnName("subject_id");
                entity.Property(c => c.teacher_percentage).HasPrecision(5, 2);

                entity.HasOne(d => d.Teacher)
                      .WithMany(p => p.Courses)
                      .HasForeignKey(d => d.teacher_id);

                entity.HasOne(d => d.Subject)
                      .WithMany()
                      .HasForeignKey(d => d.subject_id);
            });

            // --- 4️⃣ إعدادات الدروس والتمارين وجلسات الدعم ---
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.Property(l => l.teacher_id).HasColumnName("teacher_id");
                entity.Property(l => l.subject_id).HasColumnName("subject_id");
                entity.Property(l => l.class_id).HasColumnName("class_id");

                entity.HasOne(d => d.Teacher)
                      .WithMany(p => p.Lessons)
                      .HasForeignKey(d => d.teacher_id);
            });

            modelBuilder.Entity<Exercise>(entity =>
            {
                entity.Property(e => e.lesson_id).HasColumnName("lesson_id");
                entity.Ignore("Teacherid");
                entity.HasOne(d => d.Lesson)
                      .WithMany(p => p.Exercises)
                      .HasForeignKey(d => d.lesson_id)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EducationalSession>(entity =>
            {
                entity.Property(s => s.lesson_id).HasColumnName("lesson_id");
                entity.Property(s => s.student_id).HasColumnName("student_id");
                entity.Ignore("Teacherid");
            });

            // --- 5️⃣ إعدادات دقة الأرقام والمبالغ المالية ---
            modelBuilder.Entity<Teacher>().Property(t => t.hourly_price).HasPrecision(8, 2);
            modelBuilder.Entity<Counselor>().Property(c => c.hourly_price).HasPrecision(8, 2);
            modelBuilder.Entity<Payment>().Property(p => p.amount).HasPrecision(10, 2);
            modelBuilder.Entity<StudentWallet>().Property(w => w.balance).HasPrecision(12, 2);

            modelBuilder.Entity<TeacherWallet>(entity => {
                entity.Property(w => w.total_earned).HasPrecision(12, 2);
                entity.Property(w => w.pending_clearance).HasPrecision(12, 2);
                entity.Property(w => w.withdrawn_amount).HasPrecision(12, 2);
            });
            // أضيفي هذا الكود داخل OnModelCreating
            modelBuilder.Entity<WithdrawalRequest>(entity =>
            {
                entity.ToTable("withdrawal_requests"); // ربط الموديل بالجدول الصحيح
                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
                entity.Property(e => e.CounselorId).HasColumnName("counselor_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
                entity.Property(e => e.TransferReceiptUrl).HasColumnName("transfer_receipt_url");
                entity.Property(e => e.Amount).HasPrecision(12, 2).HasColumnName("amount");
                entity.Property(e => e.Status).HasColumnName("status");
            });
            // --- 6️⃣ إعدادات المستخدمين والدردشة ---
            modelBuilder.Entity<User>(entity => {
                entity.Property(u => u.phone_number).HasColumnName("phone_number");
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Room)
                      .WithMany(p => p.Messages)
                      .HasForeignKey(d => d.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Sender)
                      .WithMany()
                      .HasForeignKey(d => d.SenderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Notification>(entity => {
                entity.Property(n => n.teacher_id).HasColumnName("teacher_id");
            });
        }
    }
}