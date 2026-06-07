using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AutoAssignmentPolicy")]
public partial class AutoAssignmentPolicy
{
    [Key]
    public int PolicyId { get; set; }

    public int FacultyId { get; set; }

    public int? SemesterId { get; set; }

    public int? PeriodId { get; set; }

    [StringLength(100)]
    public string PolicyName { get; set; } = null!;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public byte RequiredInvigilatorsPerSchedule { get; set; }

    public bool AllowCrossFaculty { get; set; }

    public bool RequirePeriodAvailabilityIfExists { get; set; }

    public bool AllowFacultyMemberAsFallback { get; set; }

    public int? MaxAssignmentsPerDay { get; set; }

    public int? MaxAssignmentsPerPeriod { get; set; }

    public int MaxAssignmentsPerSlot { get; set; }

    public int SolverTimeLimitSeconds { get; set; }

    public int CreatedById { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    public int? UpdatedById { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("FacultyId")]
    [InverseProperty("AutoAssignmentPolicies")]
    public virtual Faculty Faculty { get; set; } = null!;

    [InverseProperty("Policy")]
    public virtual ICollection<AutoAssignmentRule> Rules { get; set; } = new List<AutoAssignmentRule>();

    [InverseProperty("Policy")]
    public virtual ICollection<AutoAssignmentExamFormatRule> ExamFormatRules { get; set; } = new List<AutoAssignmentExamFormatRule>();

    [InverseProperty("Policy")]
    public virtual ICollection<AutoAssignmentRoleRule> RoleRules { get; set; } = new List<AutoAssignmentRoleRule>();

    [InverseProperty("Policy")]
    public virtual ICollection<AutoAssignmentRun> Runs { get; set; } = new List<AutoAssignmentRun>();
}
