using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamSession")]
public partial class ExamSession
{
    [Key]
    public int SessionId { get; set; }

    public int PeriodId { get; set; }

    [StringLength(20)]
    public string SessionName { get; set; } = null!;

    [InverseProperty("Session")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();

    [InverseProperty("Session")]
    public virtual ICollection<ExamSlot> ExamSlots { get; set; } = new List<ExamSlot>();

    [ForeignKey("PeriodId")]
    [InverseProperty("ExamSessions")]
    public virtual ExamPeriod Period { get; set; } = null!;
}
