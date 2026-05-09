using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface ISlotService
    {
        Task<List<SlotDto>> GetAllBySessionAsync(int sessionId);

        Task<SlotDto?> GetByIdAsync(int id);
        Task AddAsync(int sessionId, string name, TimeOnly timeStart);
        Task UpdateAsync(SlotDto dto);
        Task DeleteAsync(int id);
    }
}
