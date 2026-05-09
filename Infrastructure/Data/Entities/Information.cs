using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

public partial class Information
{
    [Key]
    public int InformationId { get; set; }

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [Column("DOB", TypeName = "datetime")]
    public DateTime? Dob { get; set; }

    [StringLength(10)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string? Avt { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    public byte PositionId { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("Information")]
    public virtual Position Position { get; set; } = null!;

    [InverseProperty("Information")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
