using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamSlot")]
public partial class ExamSlot
{
    [Key]
    public int SlotId { get; set; }

    public int SessionId { get; set; }

    [StringLength(10)]
    public string SlotName { get; set; } = null!;

    public TimeOnly TimeStart { get; set; }

    [InverseProperty("Slot")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();

    [InverseProperty("Slot")]
    public virtual ICollection<LecturerBusySlot> LecturerBusySlots { get; set; } = new List<LecturerBusySlot>();

    [ForeignKey("SessionId")]
    [InverseProperty("ExamSlots")]
    public virtual ExamSession Session { get; set; } = null!;
}
