using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AutoAssignmentRun")]
public partial class AutoAssignmentRun
{
    [Key]
    public long RunId { get; set; }

    public int PolicyId { get; set; }

    public int FacultyId { get; set; }

    public int SemesterId { get; set; }

    public int PeriodId { get; set; }

    public int AssignerId { get; set; }

    public bool IsPreview { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    public int TotalSchedules { get; set; }

    public int AssignedInvigilators { get; set; }

    public int MissingSchedules { get; set; }

    public string PolicySnapshotJson { get; set; } = null!;

    public string? ResultSummaryJson { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("Runs")]
    public virtual AutoAssignmentPolicy Policy { get; set; } = null!;
}
