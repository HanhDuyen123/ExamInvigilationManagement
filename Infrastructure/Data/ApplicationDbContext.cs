using System;
using System.Collections.Generic;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademyYear> AcademyYears { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<CourseOffering> CourseOfferings { get; set; }

    public virtual DbSet<EmailNotification> EmailNotifications { get; set; }

    public virtual DbSet<ExamInvigilator> ExamInvigilators { get; set; }

    public virtual DbSet<ExamPeriod> ExamPeriods { get; set; }

    public virtual DbSet<ExamSchedule> ExamSchedules { get; set; }

    public virtual DbSet<ExamScheduleApproval> ExamScheduleApprovals { get; set; }

    public virtual DbSet<ExamSession> ExamSessions { get; set; }

    public virtual DbSet<ExamSlot> ExamSlots { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<Information> Information { get; set; }

    public virtual DbSet<InvigilatorResponse> InvigilatorResponses { get; set; }

    public virtual DbSet<InvigilatorSubstitution> InvigilatorSubstitutions { get; set; }

    public virtual DbSet<LecturerBusySlot> LecturerBusySlots { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-N3MBUQD\\HANHDUYEN;Database=ExamInvigilationManagement;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademyYear>(entity =>
        {
            entity.HasKey(e => e.AcademyYearId).HasName("PK__AcademyY__6E9A375C180825D0");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId).HasName("PK__Building__5463CDC431D73599");
        });

        modelBuilder.Entity<CourseOffering>(entity =>
        {
            entity.HasKey(e => e.OfferingId).HasName("PK__CourseOf__3500D72D8C2C085E");

            entity.HasOne(d => d.Semester).WithMany(p => p.CourseOfferings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseOffering_Semester");

            entity.HasOne(d => d.Subject).WithMany(p => p.CourseOfferings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseOffering_Subject");

            entity.HasOne(d => d.User).WithMany(p => p.CourseOfferings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseOffering_User");
        });

        modelBuilder.Entity<EmailNotification>(entity =>
        {
            entity.HasKey(e => e.EmailId).HasName("PK__EmailNot__7ED91ACF1197A34F");

            entity.HasOne(d => d.User).WithMany(p => p.EmailNotifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Email_User");
        });

        modelBuilder.Entity<ExamInvigilator>(entity =>
        {
            entity.HasKey(e => e.ExamInvigilatorId).HasName("PK__ExamInvi__97319BE3146A8BFF");

            entity.ToTable("ExamInvigilator", tb => tb.HasTrigger("trg_UpdateExamScheduleStatus"));

            entity.HasOne(d => d.Assignee).WithMany(p => p.ExamInvigilatorAssignees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invigilator_Assignee");

            entity.HasOne(d => d.Assigner).WithMany(p => p.ExamInvigilatorAssigners)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invigilator_Assigner");

            entity.HasOne(d => d.ExamSchedule).WithMany(p => p.ExamInvigilators)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invigilator_Schedule");

            entity.HasOne(d => d.NewAssignee).WithMany(p => p.ExamInvigilatorNewAssignees).HasConstraintName("FK_Invigilator_NewAssignee");
        });

        modelBuilder.Entity<ExamPeriod>(entity =>
        {
            entity.HasKey(e => e.PeriodId).HasName("PK__ExamPeri__E521BB16289BD43D");

            entity.HasOne(d => d.Semester).WithMany(p => p.ExamPeriods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamPeriod_Semester");
        });

        modelBuilder.Entity<ExamSchedule>(entity =>
        {
            entity.HasKey(e => e.ExamScheduleId).HasName("PK__ExamSche__D03AF2C250A2B4CF");

            entity.HasOne(d => d.AcademyYear).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_AcademyYear");

            entity.HasOne(d => d.Offering).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Offering");

            entity.HasOne(d => d.Period).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Period");

            entity.HasOne(d => d.Room).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Room");

            entity.HasOne(d => d.Semester).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Semester");

            entity.HasOne(d => d.Session).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Session");

            entity.HasOne(d => d.Slot).WithMany(p => p.ExamSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSchedule_Slot");
        });

        modelBuilder.Entity<ExamScheduleApproval>(entity =>
        {
            entity.HasKey(e => e.ApprovalId).HasName("PK__ExamSche__328477F4496FCD98");

            entity.HasOne(d => d.Approver).WithMany(p => p.ExamScheduleApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Approval_User");

            entity.HasOne(d => d.ExamSchedule).WithMany(p => p.ExamScheduleApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Approval_Schedule");
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__ExamSess__C9F4929030E20C1A");

            entity.HasOne(d => d.Period).WithMany(p => p.ExamSessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSession_Period");
        });

        modelBuilder.Entity<ExamSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__ExamSlot__0A124AAFF69C83CE");

            entity.HasOne(d => d.Session).WithMany(p => p.ExamSlots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSlot_Session");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.FacultyId).HasName("PK__Faculty__306F630EF22759AF");
        });

        modelBuilder.Entity<Information>(entity =>
        {
            entity.HasKey(e => e.InformationId).HasName("PK__Informat__C93C35B037A60C73");

            entity.HasOne(d => d.Position).WithMany(p => p.Information)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Information_Position");
        });

        modelBuilder.Entity<InvigilatorResponse>(entity =>
        {
            entity.HasKey(e => e.ResponseId).HasName("PK__Invigila__1AAA646C69601A4A");

            entity.HasOne(d => d.ExamInvigilator).WithMany(p => p.InvigilatorResponses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Response_Invigilator");

            entity.HasOne(d => d.User).WithMany(p => p.InvigilatorResponses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Response_User");
        });

        modelBuilder.Entity<InvigilatorSubstitution>(entity =>
        {
            entity.HasKey(e => e.SubstitutionId).HasName("PK__Invigila__95BE7D8496E219A3");

            entity.HasOne(d => d.ExamInvigilator).WithMany(p => p.InvigilatorSubstitutions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Substitution_Invigilator");

            entity.HasOne(d => d.SubstituteUser).WithMany(p => p.InvigilatorSubstitutionSubstituteUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Substitution_Substitute");

            entity.HasOne(d => d.User).WithMany(p => p.InvigilatorSubstitutionUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Substitution_User");
        });

        modelBuilder.Entity<LecturerBusySlot>(entity =>
        {
            entity.HasKey(e => e.BusySlotId).HasName("PK__Lecturer__70A1FD1C18B4EFCB");

            entity.Property(e => e.CreateAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Slot).WithMany(p => p.LecturerBusySlots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusySlot_Slot");

            entity.HasOne(d => d.User).WithMany(p => p.LecturerBusySlots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusySlot_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12C17671B6");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations).HasConstraintName("FK_Notification_CreatedBy");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A799681B982");

            entity.Property(e => e.PositionId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1AFFE46476");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863939AE4B386B");

            entity.HasOne(d => d.Building).WithMany(p => p.Rooms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Building");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__043301DD3D4C52BA");

            entity.HasOne(d => d.AcademyYear).WithMany(p => p.Semesters)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Semester_AcademyYear");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subject__AC1BA3A8EE5538E4");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Subjects)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subject_Faculty");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C32FBD3B3");

            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);

            entity.HasOne(d => d.Faculty).WithMany(p => p.Users).HasConstraintName("FK_User_Faculty");

            entity.HasOne(d => d.Information).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Information");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id); // Primary key

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ExpiredAt)
                .IsRequired();

            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false);

            // Relationship với User
            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens) // tạo ICollection trong User
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PasswordResetToken_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
