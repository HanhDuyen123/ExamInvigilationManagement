using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class AcademyYearRepository : IAcademyYearRepository
    {
        private readonly ApplicationDbContext _context;

        public AcademyYearRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AcademyYear>> GetAllAsync()
        {
            return await _context.AcademyYears
                .Include(x => x.Semesters)
                .ThenInclude(s => s.ExamPeriods)
                .ThenInclude(p => p.ExamSessions)
                .ThenInclude(s => s.ExamSlots)
                .Select(x => new AcademyYear
                {
                    Id = x.AcademyYearId,
                    Name = x.AcademyYearName,
                    Semesters = x.Semesters.Select(s => new ExamInvigilationManagement.Domain.Entities.Semester
                    {
                        Id = s.SemesterId,
                        AcademyYearId = s.AcademyYearId,
                        Name = s.SemesterName,
                        ExamPeriods = s.ExamPeriods.Select(p => new ExamInvigilationManagement.Domain.Entities.ExamPeriod
                        {
                            Id = p.PeriodId,
                            SemesterId = p.SemesterId,
                            Name = p.PeriodName,
                            ExamSessions = p.ExamSessions.Select(se => new ExamInvigilationManagement.Domain.Entities.ExamSession
                            {
                                Id = se.SessionId,
                                PeriodId = se.PeriodId,
                                Name = se.SessionName,
                                ExamSlots = se.ExamSlots.Select(sl => new ExamInvigilationManagement.Domain.Entities.ExamSlot
                                {
                                    Id = sl.SlotId,
                                    SessionId = sl.SessionId,
                                    Name = sl.SlotName,
                                    TimeStart = sl.TimeStart
                                }).ToList()
                            }).ToList()
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<AcademyYear?> GetByIdAsync(int id)
        {
            var entity = await _context.AcademyYears.FindAsync(id);
            return entity?.ToDomain();
        }
        public async Task<AcademyYear?> GetByNameAsync(string name)
        {
            var entity = await _context.AcademyYears
                .FirstOrDefaultAsync(x => x.AcademyYearName == name);

            return entity?.ToDomain();
        }

        public async Task AddAsync(AcademyYear entity)
        {
            var data = entity.ToEntity();
            _context.AcademyYears.Add(data);
            await _context.SaveChangesAsync();
        }
        public async Task<AcademyYearDetailDto?> GetDetailAsync(int id)
        {
            var data = await _context.AcademyYears
                .Include(x => x.Semesters)
                    .ThenInclude(s => s.ExamPeriods)
                        .ThenInclude(p => p.ExamSessions)
                            .ThenInclude(s => s.ExamSlots)
                .FirstOrDefaultAsync(x => x.AcademyYearId == id);

            if (data == null) return null;

            return new AcademyYearDetailDto
            {
                Id = data.AcademyYearId,
                Name = data.AcademyYearName,
                Semesters = data.Semesters.OrderBy(s => s.SemesterName).Select(s => new SemesterDto
                {
                    Id = s.SemesterId,
                    Name = s.SemesterName,
                    Periods = s.ExamPeriods.OrderBy(p => p.PeriodName).Select(p => new PeriodDto
                    {
                        Id = p.PeriodId,
                        Name = p.PeriodName,
                        Sessions = p.ExamSessions.OrderBy(se => se.SessionName).Select(se => new SessionDto
                        {
                            Id = se.SessionId,
                            Name = se.SessionName,
                            Slots = se.ExamSlots.OrderBy(sl => sl.TimeStart).Select(sl => new SlotDto
                            {
                                Id = sl.SlotId,
                                Name = sl.SlotName,
                                TimeStart = sl.TimeStart
                            }).ToList()
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }
        public async Task UpdateAsync(AcademyYear entity)
        {
            var data = await _context.AcademyYears.FindAsync(entity.Id);

            if (data == null) return;

            data.AcademyYearName = entity.Name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var data = await _context.AcademyYears.FindAsync(id);

            if (data != null)
            {
                _context.AcademyYears.Remove(data);
                await _context.SaveChangesAsync();
            }
        }
    }
}
