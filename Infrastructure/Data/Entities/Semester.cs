using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("Semester")]
public partial class Semester
{
    [Key]
    public int SemesterId { get; set; }

    [Column("AcademyYearID")]
    public int AcademyYearId { get; set; }

    [StringLength(10)]
    public string SemesterName { get; set; } = null!;

    [ForeignKey("AcademyYearId")]
    [InverseProperty("Semesters")]
    public virtual AcademyYear AcademyYear { get; set; } = null!;

    [InverseProperty("Semester")]
    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    [InverseProperty("Semester")]
    public virtual ICollection<ExamPeriod> ExamPeriods { get; set; } = new List<ExamPeriod>();

    [InverseProperty("Semester")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();
}
