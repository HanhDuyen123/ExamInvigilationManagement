using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("Subject")]
public partial class Subject
{
    [Key]
    [StringLength(10)]
    public string SubjectId { get; set; } = null!;

    public int FacultyId { get; set; }

    [StringLength(100)]
    public string SubjectName { get; set; } = null!;

    public byte Credit { get; set; }

    [InverseProperty("Subject")]
    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    [ForeignKey("FacultyId")]
    [InverseProperty("Subjects")]
    public virtual Faculty Faculty { get; set; } = null!;
}
