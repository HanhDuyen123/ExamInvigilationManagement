using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AcademyYear")]
public partial class AcademyYear
{
    [Key]
    [Column("AcademyYearID")]
    public int AcademyYearId { get; set; }

    [StringLength(15)]
    public string AcademyYearName { get; set; } = null!;

    [InverseProperty("AcademyYear")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();

    [InverseProperty("AcademyYear")]
    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}
