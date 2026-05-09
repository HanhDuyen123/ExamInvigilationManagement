using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamPeriod")]
public partial class ExamPeriod
{
    [Key]
    public int PeriodId { get; set; }

    public int SemesterId { get; set; }

    [StringLength(10)]
    public string PeriodName { get; set; } = null!;

    [InverseProperty("Period")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();

    [InverseProperty("Period")]
    public virtual ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();

    [ForeignKey("SemesterId")]
    [InverseProperty("ExamPeriods")]
    public virtual Semester Semester { get; set; } = null!;
}
