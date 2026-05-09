using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("InvigilatorSubstitution")]
public partial class InvigilatorSubstitution
{
    [Key]
    public int SubstitutionId { get; set; }

    public int ExamInvigilatorId { get; set; }

    public int UserId { get; set; }

    public int SubstituteUserId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreateAt { get; set; }

    [ForeignKey("ExamInvigilatorId")]
    [InverseProperty("InvigilatorSubstitutions")]
    public virtual ExamInvigilator ExamInvigilator { get; set; } = null!;

    [ForeignKey("SubstituteUserId")]
    [InverseProperty("InvigilatorSubstitutionSubstituteUsers")]
    public virtual User SubstituteUser { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("InvigilatorSubstitutionUsers")]
    public virtual User User { get; set; } = null!;
}
