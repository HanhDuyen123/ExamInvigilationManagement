using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AutoAssignmentRule")]
public partial class AutoAssignmentRule
{
    [Key]
    public int RuleId { get; set; }

    public int PolicyId { get; set; }

    [StringLength(50)]
    public string RuleCode { get; set; } = null!;

    [StringLength(100)]
    public string RuleName { get; set; } = null!;

    [StringLength(20)]
    public string RuleType { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public bool IsRequired { get; set; }

    public int PriorityOrder { get; set; }

    public int Weight { get; set; }

    public string? ParametersJson { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("Rules")]
    public virtual AutoAssignmentPolicy Policy { get; set; } = null!;
}
