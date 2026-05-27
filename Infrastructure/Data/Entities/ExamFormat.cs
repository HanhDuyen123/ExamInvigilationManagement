using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ExamFormat")]
public partial class ExamFormat
{
    [Key]
    public int ExamFormatId { get; set; }

    [StringLength(20)]
    public string Code { get; set; } = null!;

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    [InverseProperty("ExamFormat")]
    public virtual ICollection<ExamSchedule> ExamSchedules { get; set; } = new List<ExamSchedule>();
}
