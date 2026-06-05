using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("LecturerPeriodAvailability")]
[Index("UserId", "PeriodId", Name = "UQ_LecturerPeriodAvailability_User_Period", IsUnique = true)]
public partial class LecturerPeriodAvailability
{
    [Key]
    public int AvailabilityId { get; set; }

    public int UserId { get; set; }

    public int PeriodId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [StringLength(20)]
    public string Source { get; set; } = null!;

    public int CreatedById { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedById")]
    [InverseProperty("CreatedLecturerPeriodAvailabilities")]
    public virtual User CreatedBy { get; set; } = null!;

    [ForeignKey("PeriodId")]
    [InverseProperty("LecturerPeriodAvailabilities")]
    public virtual ExamPeriod Period { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("LecturerPeriodAvailabilities")]
    public virtual User User { get; set; } = null!;
}
