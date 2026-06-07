using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AutoAssignmentExamFormatRule")]
public partial class AutoAssignmentExamFormatRule
{
    [Key]
    public int Id { get; set; }

    public int PolicyId { get; set; }

    public int ExamFormatId { get; set; }

    [StringLength(30)]
    public string PriorityGroup { get; set; } = null!;

    [StringLength(30)]
    public string AssignmentMode { get; set; } = "Full";

    public int SpecialistWeight { get; set; }

    public int ExactOwnerWeight { get; set; }

    public int SameSubjectWeight { get; set; }

    [ForeignKey("ExamFormatId")]
    [InverseProperty("AutoAssignmentExamFormatRules")]
    public virtual ExamFormat ExamFormat { get; set; } = null!;

    [ForeignKey("PolicyId")]
    [InverseProperty("ExamFormatRules")]
    public virtual AutoAssignmentPolicy Policy { get; set; } = null!;
}
