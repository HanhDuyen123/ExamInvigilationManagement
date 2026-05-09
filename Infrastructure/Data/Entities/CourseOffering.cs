using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("CourseOffering")]
public partial class CourseOffering
{
    [Key]
    public int OfferingId { get; set; }

    public int UserId { get; set; }

    public int SemesterId { get; set; }

    [StringLength(10)]
    public string SubjectId { get; set; } = null!;

    [StringLength(10)]
    public string ClassName { get; set; } = null!;

    [StringLength(2)]
    public string GroupNumber { get; set; } = null!;

    [InverseProperty("Offering")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();

    [ForeignKey("SemesterId")]
    [InverseProperty("CourseOfferings")]
    public virtual Semester Semester { get; set; } = null!;

    [ForeignKey("SubjectId")]
    [InverseProperty("CourseOfferings")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CourseOfferings")]
    public virtual User User { get; set; } = null!;
}
