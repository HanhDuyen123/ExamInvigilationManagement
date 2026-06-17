using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories;

public class ExamFormatRepository : IExamFormatRepository
{
    private readonly ApplicationDbContext _db;

    public ExamFormatRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ExamFormatDto>> GetPagedAsync(string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.ExamFormats.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(kw) || x.Name.ToLower().Contains(kw));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExamFormatDto { Id = x.ExamFormatId, Code = x.Code, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);

        return new PagedResult<ExamFormatDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<ExamFormatDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.ExamFormats
            .AsNoTracking()
            .Where(x => x.ExamFormatId == id)
            .Select(x => new ExamFormatDto { Id = x.ExamFormatId, Code = x.Code, Name = x.Name, IsActive = x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, int? ignoredId = null, CancellationToken cancellationToken = default)
    {
        return _db.ExamFormats.AnyAsync(x => (!ignoredId.HasValue || x.ExamFormatId != ignoredId.Value) && x.Code == code, cancellationToken);
    }

    public Task<bool> IsUsedInScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.ExamSchedules.AnyAsync(x => x.ExamFormatId == id, cancellationToken);
    }

    public async Task CreateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default)
    {
        _db.ExamFormats.Add(new ExamFormat { Code = dto.Code, Name = dto.Name, IsActive = dto.IsActive });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExamFormatDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _db.ExamFormats.FirstOrDefaultAsync(x => x.ExamFormatId == dto.Id, cancellationToken);
        if (item == null) return;

        item.Code = dto.Code;
        item.Name = dto.Name;
        item.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.ExamFormats.FirstOrDefaultAsync(x => x.ExamFormatId == id, cancellationToken);
        if (item == null) return;

        _db.ExamFormats.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
