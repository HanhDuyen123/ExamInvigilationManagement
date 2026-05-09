using ExamInvigilationManagement.Domain.Entities;

namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface ISlotRepository
    {
        Task<List<ExamSlot>> GetAllBySessionAsync(int sessionId);
        Task AddAsync(int sessionId, ExamSlot entity);
        Task<ExamSlot?> GetByIdAsync(int id);
        Task UpdateAsync(ExamSlot slot);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, string name, TimeOnly timeStart);
    }
}
