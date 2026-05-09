using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("User")]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    public byte RoleId { get; set; }

    public int InformationId { get; set; }

    public int? FacultyId { get; set; }

    [StringLength(8)]
    public string UserName { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public int? FailedLoginAttempts { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastLogin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LockoutEnd { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    [InverseProperty("User")]
    public virtual ICollection<EmailNotification> EmailNotifications { get; set; } = new List<EmailNotification>();

    [InverseProperty("Assignee")]
    public virtual ICollection<ExamInvigilator> ExamInvigilatorAssignees { get; set; } = new List<ExamInvigilator>();

    [InverseProperty("Assigner")]
    public virtual ICollection<ExamInvigilator> ExamInvigilatorAssigners { get; set; } = new List<ExamInvigilator>();

    [InverseProperty("NewAssignee")]
    public virtual ICollection<ExamInvigilator> ExamInvigilatorNewAssignees { get; set; } = new List<ExamInvigilator>();

    [InverseProperty("Approver")]
    public virtual ICollection<ExamScheduleApproval> ExamScheduleApprovals { get; set; } = new List<ExamScheduleApproval>();

    [ForeignKey("FacultyId")]
    [InverseProperty("Users")]
    public virtual Faculty? Faculty { get; set; }

    [ForeignKey("InformationId")]
    [InverseProperty("Users")]
    public virtual Information Information { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<InvigilatorResponse> InvigilatorResponses { get; set; } = new List<InvigilatorResponse>();

    [InverseProperty("SubstituteUser")]
    public virtual ICollection<InvigilatorSubstitution> InvigilatorSubstitutionSubstituteUsers { get; set; } = new List<InvigilatorSubstitution>();

    [InverseProperty("User")]
    public virtual ICollection<InvigilatorSubstitution> InvigilatorSubstitutionUsers { get; set; } = new List<InvigilatorSubstitution>();

    [InverseProperty("User")]
    public virtual ICollection<LecturerBusySlot> LecturerBusySlots { get; set; } = new List<LecturerBusySlot>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;
}
