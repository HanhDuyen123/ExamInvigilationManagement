using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("Building")]
public partial class Building
{
    [Key]
    [StringLength(10)]
    public string BuildingId { get; set; } = null!;

    [StringLength(50)]
    public string BuildingName { get; set; } = null!;

    [InverseProperty("Building")]
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
