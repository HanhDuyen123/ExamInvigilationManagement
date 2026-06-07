using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AutoAssignmentRoleRule")]
public partial class AutoAssignmentRoleRule
{
    [Key]
    public int Id { get; set; }

    public int PolicyId { get; set; }

    public byte RoleId { get; set; }

    public bool IsEligible { get; set; }

    [StringLength(30)]
    public string CandidateTier { get; set; } = null!;

    public int Weight { get; set; }

    [ForeignKey("PolicyId")]
    [InverseProperty("RoleRules")]
    public virtual AutoAssignmentPolicy Policy { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("AutoAssignmentRoleRules")]
    public virtual Role Role { get; set; } = null!;
}
