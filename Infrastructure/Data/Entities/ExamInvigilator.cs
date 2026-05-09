using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamInvigilator")]
public partial class ExamInvigilator
{
    [Key]
    public int ExamInvigilatorId { get; set; }

    public int AssigneeId { get; set; }

    public int AssignerId { get; set; }

    public int? NewAssigneeId { get; set; }

    public int ExamScheduleId { get; set; }

    public byte PositionNo { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreateAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateAt { get; set; }

    [ForeignKey("AssigneeId")]
    [InverseProperty("ExamInvigilatorAssignees")]
    public virtual User Assignee { get; set; } = null!;

    [ForeignKey("AssignerId")]
    [InverseProperty("ExamInvigilatorAssigners")]
    public virtual User Assigner { get; set; } = null!;

    [ForeignKey("ExamScheduleId")]
    [InverseProperty("ExamInvigilators")]
    public virtual ExamSchedule ExamSchedule { get; set; } = null!;

    [InverseProperty("ExamInvigilator")]
    public virtual ICollection<InvigilatorResponse> InvigilatorResponses { get; set; } = new List<InvigilatorResponse>();

    [InverseProperty("ExamInvigilator")]
    public virtual ICollection<InvigilatorSubstitution> InvigilatorSubstitutions { get; set; } = new List<InvigilatorSubstitution>();

    [ForeignKey("NewAssigneeId")]
    [InverseProperty("ExamInvigilatorNewAssignees")]
    public virtual User? NewAssignee { get; set; }
}
