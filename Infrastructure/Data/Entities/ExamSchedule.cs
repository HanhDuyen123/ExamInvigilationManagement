using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamSchedule")]
public partial class ExamSchedule
{
    [Key]
    public int ExamScheduleId { get; set; }

    public int SlotId { get; set; }

    [Column("AcademyYearID")]
    public int AcademyYearId { get; set; }

    public int SemesterId { get; set; }

    public int PeriodId { get; set; }

    public int SessionId { get; set; }

    public int RoomId { get; set; }

    public int OfferingId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ExamDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [ForeignKey("AcademyYearId")]
    [InverseProperty("ExamSchedules")]
    public virtual AcademyYear AcademyYear { get; set; } = null!;

    [InverseProperty("ExamSchedule")]
    public virtual ICollection<ExamInvigilator> ExamInvigilators { get; set; } = new List<ExamInvigilator>();

    [InverseProperty("ExamSchedule")]
    public virtual ICollection<ExamScheduleApproval> ExamScheduleApprovals { get; set; } = new List<ExamScheduleApproval>();

    [ForeignKey("OfferingId")]
    [InverseProperty("ExamSchedules")]
    public virtual CourseOffering Offering { get; set; } = null!;

    [ForeignKey("PeriodId")]
    [InverseProperty("ExamSchedules")]
    public virtual ExamPeriod Period { get; set; } = null!;

    [ForeignKey("RoomId")]
    [InverseProperty("ExamSchedules")]
    public virtual Room Room { get; set; } = null!;

    [ForeignKey("SemesterId")]
    [InverseProperty("ExamSchedules")]
    public virtual Semester Semester { get; set; } = null!;

    [ForeignKey("SessionId")]
    [InverseProperty("ExamSchedules")]
    public virtual ExamSession Session { get; set; } = null!;

    [ForeignKey("SlotId")]
    [InverseProperty("ExamSchedules")]
    public virtual ExamSlot Slot { get; set; } = null!;
}
