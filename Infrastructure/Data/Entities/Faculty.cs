using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("Faculty")]
public partial class Faculty
{
    [Key]
    public int FacultyId { get; set; }

    [StringLength(50)]
    public string FacultyName { get; set; } = null!;

    [InverseProperty("Faculty")]
    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    [InverseProperty("Faculty")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
