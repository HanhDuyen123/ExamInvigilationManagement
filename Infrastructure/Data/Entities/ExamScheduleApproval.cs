using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamScheduleApproval")]
public partial class ExamScheduleApproval
{
    [Key]
    public int ApprovalId { get; set; }

    public int ExamScheduleId { get; set; }

    public int ApproverId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateAt { get; set; }

    [ForeignKey("ApproverId")]
    [InverseProperty("ExamScheduleApprovals")]
    public virtual User Approver { get; set; } = null!;

    [ForeignKey("ExamScheduleId")]
    [InverseProperty("ExamScheduleApprovals")]
    public virtual ExamSchedule ExamSchedule { get; set; } = null!;
}
