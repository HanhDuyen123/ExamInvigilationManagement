using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("InvigilatorResponse")]
public partial class InvigilatorResponse
{
    [Key]
    public int ResponseId { get; set; }

    public int UserId { get; set; }

    public int ExamInvigilatorId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "nvarchar(max)")]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResponseAt { get; set; }

    [ForeignKey("ExamInvigilatorId")]
    [InverseProperty("InvigilatorResponses")]
    public virtual ExamInvigilator ExamInvigilator { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("InvigilatorResponses")]
    public virtual User User { get; set; } = null!;
}
