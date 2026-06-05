using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual DbSet<AcademyYear> AcademyYears { get; set; }

    public virtual DbSet<ApprovalHistory> ApprovalHistories { get; set; }

    public virtual DbSet<ApprovalRequest> ApprovalRequests { get; set; }

    public virtual DbSet<AssignmentChangeHistory> AssignmentChangeHistories { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<CourseOffering> CourseOfferings { get; set; }

    public virtual DbSet<EmailNotification> EmailNotifications { get; set; }

    public virtual DbSet<ExamInvigilator> ExamInvigilators { get; set; }

    public virtual DbSet<ExamFormat> ExamFormats { get; set; }

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

    public virtual DbSet<LecturerBusyPeriod> LecturerBusyPeriods { get; set; }

    public virtual DbSet<LecturerPeriodAvailability> LecturerPeriodAvailabilities { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=DESKTOP-N3MBUQD\\HANHDUYEN;Database=ExamInvigilationManagement;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademyYear>(entity =>
        {
            entity.HasKey(e => e.AcademyYearId).HasName("PK__AcademyY__6E9A375C180825D0");
        });

        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.ApprovalHistoryId).HasName("PK_ApprovalHistory");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.HasIndex(e => e.ExamScheduleId).HasDatabaseName("IX_ApprovalHistory_ExamScheduleId");
            entity.HasIndex(e => e.ApprovalRequestId).HasDatabaseName("IX_ApprovalHistory_ApprovalRequestId");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_ApprovalHistory_CorrelationId");
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(e => e.ApprovalRequestId).HasName("PK_ApprovalRequest");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.HasIndex(e => e.RequestedById).HasDatabaseName("IX_ApprovalRequest_RequestedById");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_ApprovalRequest_CorrelationId");
        });

        modelBuilder.Entity<AssignmentChangeHistory>(entity =>
        {
            entity.HasKey(e => e.AssignmentChangeHistoryId).HasName("PK_AssignmentChangeHistory");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.HasIndex(e => e.ExamScheduleId).HasDatabaseName("IX_AssignmentChangeHistory_ExamScheduleId");
            entity.HasIndex(e => e.ExamInvigilatorId).HasDatabaseName("IX_AssignmentChangeHistory_ExamInvigilatorId");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_AssignmentChangeHistory_CorrelationId");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK_AuditLog");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.HasIndex(e => new { e.EntityName, e.EntityId }).HasDatabaseName("IX_AuditLog_Entity");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_AuditLog_CorrelationId");
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

        modelBuilder.Entity<ExamFormat>(entity =>
        {
            entity.HasKey(e => e.ExamFormatId).HasName("PK_ExamFormat");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UX_ExamFormat_Code");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
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

            entity.HasOne(d => d.ExamFormat).WithMany(p => p.ExamSchedules)
                .HasConstraintName("FK_ExamSchedule_ExamFormat");

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
            entity.Property(e => e.ApprovalStatus).HasDefaultValue("Chờ duyệt");

            entity.HasIndex(e => e.ApprovalStatus).HasDatabaseName("IX_LecturerBusySlot_ApprovalStatus");

            entity.HasOne(d => d.ApprovedBy).WithMany(p => p.ApprovedLecturerBusySlots).HasConstraintName("FK_LecturerBusySlot_ApprovedBy");

            entity.HasOne(d => d.Slot).WithMany(p => p.LecturerBusySlots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusySlot_Slot");

            entity.HasOne(d => d.User).WithMany(p => p.LecturerBusySlots)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusySlot_User");
        });

        modelBuilder.Entity<LecturerBusyPeriod>(entity =>
        {
            entity.HasKey(e => e.BusyPeriodId).HasName("PK_LecturerBusyPeriod");
            entity.Property(e => e.CreateAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ApprovalStatus).HasDefaultValue("Chờ duyệt");
            entity.HasIndex(e => e.ApprovalStatus).HasDatabaseName("IX_LecturerBusyPeriod_ApprovalStatus");

            entity.HasOne(d => d.ApprovedBy).WithMany(p => p.ApprovedLecturerBusyPeriods).HasConstraintName("FK_LecturerBusyPeriod_ApprovedBy");
            entity.HasOne(d => d.Period).WithMany(p => p.LecturerBusyPeriods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturerBusyPeriod_Period");
            entity.HasOne(d => d.User).WithMany(p => p.LecturerBusyPeriods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturerBusyPeriod_User");
        });

        modelBuilder.Entity<LecturerPeriodAvailability>(entity =>
        {
            entity.HasKey(e => e.AvailabilityId).HasName("PK_LecturerPeriodAvailability");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Source).HasDefaultValue("Manual");
            entity.HasIndex(e => e.PeriodId).HasDatabaseName("IX_LecturerPeriodAvailability_PeriodId");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.CreatedLecturerPeriodAvailabilities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturerPeriodAvailability_CreatedBy");
            entity.HasOne(d => d.Period).WithMany(p => p.LecturerPeriodAvailabilities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturerPeriodAvailability_Period");
            entity.HasOne(d => d.User).WithMany(p => p.LecturerPeriodAvailabilities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturerPeriodAvailability_User");
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

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.OutboxMessageId).HasName("PK_OutboxMessage");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_OutboxMessage_Status");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_OutboxMessage_CorrelationId");
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAutomaticAuditLogs();
        NormalizeAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AddAutomaticAuditLogs();
        NormalizeAuditLogs();
        return base.SaveChanges();
    }

    private void NormalizeAuditLogs()
    {
        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var audit = entry.Entity;
            audit.EventType = Truncate(audit.EventType, 100) ?? string.Empty;
            audit.EntityName = Truncate(audit.EntityName, 100) ?? string.Empty;
            audit.EntityId = Truncate(audit.EntityId, 100);
            audit.Action = Truncate(audit.Action, 50) ?? string.Empty;
            audit.Source = Truncate(audit.Source, 100);
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength];
    }

    private void AddAutomaticAuditLogs()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => ShouldAudit(e))
            .ToList();

        if (entries.Count == 0) return;

        var actorUserId = GetCurrentUserId();
        var now = DateTime.Now;
        var correlationId = Guid.NewGuid();

        foreach (var entry in entries)
        {
            AuditLogs.Add(new AuditLog
            {
                EventType = "CrudChange",
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                Action = entry.State.ToString(),
                ActorUserId = actorUserId,
                OldValues = entry.State == EntityState.Added ? null : SerializeValues(entry, original: true),
                NewValues = entry.State == EntityState.Deleted ? null : SerializeValues(entry, original: false),
                CreatedAt = now,
                CorrelationId = correlationId,
                Source = "ApplicationDbContext"
            });
        }
    }

    private static bool ShouldAudit(EntityEntry entry)
    {
        if (entry.Entity is AuditLog or OutboxMessage) return false;

        return entry.Entity is AcademyYear
            or Building
            or CourseOffering
            or ExamFormat
            or ExamInvigilator
            or ExamPeriod
            or ExamSchedule
            or ExamScheduleApproval
            or ExamSession
            or ExamSlot
            or Faculty
            or Entities.Information
            or InvigilatorResponse
            or InvigilatorSubstitution
            or LecturerBusySlot
            or Position
            or Role
            or Room
            or Semester
            or Subject
            or User;
    }

    private int? GetCurrentUserId()
    {
        var value = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null) return null;

        var values = key.Properties.Select(p =>
            entry.State == EntityState.Deleted
                ? entry.Property(p.Name).OriginalValue?.ToString()
                : entry.Property(p.Name).CurrentValue?.ToString());

        return string.Join(",", values.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string SerializeValues(EntityEntry entry, bool original)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey()) continue;
            if (property.Metadata.IsForeignKey() || property.Metadata.ClrType.IsPrimitive || property.Metadata.ClrType == typeof(string) || property.Metadata.ClrType == typeof(DateTime) || property.Metadata.ClrType == typeof(DateOnly) || property.Metadata.ClrType == typeof(TimeOnly) || Nullable.GetUnderlyingType(property.Metadata.ClrType) != null)
            {
                if (entry.State == EntityState.Modified && !property.IsModified && !original) continue;
                values[property.Metadata.Name] = original ? property.OriginalValue : property.CurrentValue;
            }
        }

        return JsonSerializer.Serialize(values);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
