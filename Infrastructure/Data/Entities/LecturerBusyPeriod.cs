using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("LecturerBusyPeriod")]
[Index("UserId", "PeriodId", Name = "UQ_LecturerBusyPeriod_User_Period", IsUnique = true)]
public partial class LecturerBusyPeriod
{
    [Key]
    public int BusyPeriodId { get; set; }

    public int UserId { get; set; }

    public int PeriodId { get; set; }

    [StringLength(500)]
    public string Note { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreateAt { get; set; }

    [StringLength(20)]
    public string ApprovalStatus { get; set; } = null!;

    public int? ApprovedById { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    [ForeignKey("ApprovedById")]
    [InverseProperty("ApprovedLecturerBusyPeriods")]
    public virtual User? ApprovedBy { get; set; }

    [ForeignKey("PeriodId")]
    [InverseProperty("LecturerBusyPeriods")]
    public virtual ExamPeriod Period { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("LecturerBusyPeriods")]
    public virtual User User { get; set; } = null!;
}
