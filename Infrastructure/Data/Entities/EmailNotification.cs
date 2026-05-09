using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("EmailNotification")]
public partial class EmailNotification
{
    [Key]
    public int EmailId { get; set; }

    public int UserId { get; set; }

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? SentAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    [StringLength(50)]
    public string? Type { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmailNotifications")]
    public virtual User User { get; set; } = null!;
}
