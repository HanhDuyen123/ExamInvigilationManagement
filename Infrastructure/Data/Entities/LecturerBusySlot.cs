using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("LecturerBusySlot")]
[Index("UserId", "SlotId", "BusyDate", Name = "UQ_Busy", IsUnique = true)]
public partial class LecturerBusySlot
{
    [Key]
    public int BusySlotId { get; set; }

    public int UserId { get; set; }

    public int SlotId { get; set; }

    public DateOnly BusyDate { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreateAt { get; set; }

    [ForeignKey("SlotId")]
    [InverseProperty("LecturerBusySlots")]
    public virtual ExamSlot Slot { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("LecturerBusySlots")]
    public virtual User User { get; set; } = null!;
}
