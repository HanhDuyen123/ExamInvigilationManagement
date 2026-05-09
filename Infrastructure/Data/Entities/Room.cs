using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("Room")]
public partial class Room
{
    [Key]
    public int RoomId { get; set; }

    [StringLength(10)]
    public string BuildingId { get; set; } = null!;

    [StringLength(5)]
    public string RoomName { get; set; } = null!;

    public int? Capacity { get; set; }

    [ForeignKey("BuildingId")]
    [InverseProperty("Rooms")]
    public virtual Building Building { get; set; } = null!;

    [InverseProperty("Room")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();
}
