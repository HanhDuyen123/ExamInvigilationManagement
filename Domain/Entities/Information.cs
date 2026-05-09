using ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Domain.Entities;

public class Information
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    public DateTime? Dob { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Email { get; set; } = null!;

    public string? Avt { get; set; }
    public string? Gender { get; set; }

    public byte PositionId { get; set; }

    public Position? Position { get; set; }
}