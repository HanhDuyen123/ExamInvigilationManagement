using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Domain.Enums;
using ExamInvigilationManagement.Infrastructure.Services;
using ExamInvigilationManagement.Infrastructure.Mapping;
using ExamInvigilationManagement.Application.Interfaces.Common;

namespace ExamInvigilationManagement.Application.Services
{
    public class AcademyYearService : IAcademyYearService
    {
        private readonly IAcademyYearRepository _repo;
        private readonly IAcademyYearGeneratorService _generator;

        public AcademyYearService(
            IAcademyYearRepository repo,
            IAcademyYearGeneratorService generator)
        {
            _repo = repo;
            _generator = generator;
        }
        public async Task<PagedResult<AcademyYearDto>> GetPagedAsync(
            string? keyword,
            int? semesterType,
            int page,
            int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrEmpty(keyword))
            {
                data = data.Where(x =>
                    x.Name.ToLower().Contains(keyword.Trim().ToLower())
                ).ToList();
            }

            if (semesterType != null)
            {
                var type = (SemesterType)semesterType.Value;

                data = data.Where(x =>
                    x.Semesters.Any(s => SemesterHelper.ToType(s.Name) == type)
                ).ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AcademyYearDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    SemesterCount = x.Semesters.Count,
                    PeriodCount = x.Semesters.Sum(s => s.ExamPeriods.Count),
                    SessionCount = x.Semesters.Sum(s => s.ExamPeriods.Sum(p => p.ExamSessions.Count)),
                    SlotCount = x.Semesters.Sum(s => s.ExamPeriods.Sum(p => p.ExamSessions.Sum(se => se.ExamSlots.Count))),
                    IsComplete = IsCompleteStructure(x)
                })
                .ToList();

            return new PagedResult<AcademyYearDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
        //public async Task<PagedResult<AcademyYearDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        //{
        //    var data = await _repo.GetAllAsync();

        //    if (!string.IsNullOrEmpty(keyword))
        //    {
        //        data = data
        //            .Where(x => x.Name.ToLower().Contains(keyword.ToLower()))
        //            .ToList();
        //    }

        //    var total = data.Count;

        //    var items = data
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .Select(x => new AcademyYearDto
        //        {
        //            Id = x.Id,
        //            Name = x.Name
        //        })
        //        .ToList();

        //    return new PagedResult<AcademyYearDto>
        //    {
        //        Items = items,
        //        TotalCount = total,
        //        Page = page,
        //        PageSize = pageSize
        //    };
        //}

        public async Task<List<AcademyYearDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new AcademyYearDto
            {
                Id = x.Id,
                Name = x.Name,
                SemesterCount = x.Semesters.Count,
                PeriodCount = x.Semesters.Sum(s => s.ExamPeriods.Count),
                SessionCount = x.Semesters.Sum(s => s.ExamPeriods.Sum(p => p.ExamSessions.Count)),
                SlotCount = x.Semesters.Sum(s => s.ExamPeriods.Sum(p => p.ExamSessions.Sum(se => se.ExamSlots.Count))),
                IsComplete = IsCompleteStructure(x)
            }).ToList();
        }

        public async Task<AcademyYearDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new AcademyYearDto
            {
                Id = x.Id,
                Name = x.Name
            };
        }

        public async Task CreateAsync(AcademyYearDto dto)
        {
            dto.Name = NormalizeName(dto.Name);
            await EnsureUniqueNameAsync(dto.Name);

            var entity = new AcademyYear
            {
                Name = dto.Name
            };

            await _repo.AddAsync(entity);
        }
        public async Task<AcademyYearDetailDto?> GetDetailAsync(int id)
        {
            return await _repo.GetDetailAsync(id);
        }

        public async Task<string> GetAcademyYearNameAsync(int academyYearId)
        {
            var academyYear = await _repo.GetByIdAsync(academyYearId);
            return academyYear?.Name ?? string.Empty;
        }

        public async Task CreateAsync(CreateAcademyYearDto dto)
        {
            dto.Name = NormalizeName(dto.Name);
            await EnsureUniqueNameAsync(dto.Name);

            var entity = new AcademyYear
            {
                Name = dto.Name
            };

            if (!dto.AutoGenerate || dto.Semesters.Count == 0)
            {
                await _repo.AddAsync(entity);
                return;
            }
            dto.Semesters = NormalizeAndValidateOptions(dto.Semesters);
            var dataEntity = entity.ToEntity();

            await _generator.GenerateAsync(dataEntity, dto.Semesters);

            //await _generator.GenerateAsync(entity, dto.Semesters);
        }

        public async Task UpdateAsync(AcademyYearDto dto)
        {
            dto.Name = NormalizeName(dto.Name);
            await EnsureUniqueNameAsync(dto.Name, dto.Id);

            var entity = new AcademyYear
            {
                Id = dto.Id,
                Name = dto.Name
            };

            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        private static string NormalizeName(string? name)
        {
            var normalized = (name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Vui lòng nhập năm học.");

            return normalized;
        }

        private async Task EnsureUniqueNameAsync(string name, int? currentId = null)
        {
            var all = await _repo.GetAllAsync();
            var duplicated = all.Any(x =>
                (!currentId.HasValue || x.Id != currentId.Value) &&
                string.Equals(x.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));

            if (duplicated)
                throw new InvalidOperationException("Năm học đã tồn tại.");
        }

        private static List<SemesterOptionDto> NormalizeAndValidateOptions(List<SemesterOptionDto> posted)
        {
            var expected = DefaultDataBuilder.Build().Semesters;

            foreach (var expectedSemester in expected)
            {
                var postedSemester = posted.FirstOrDefault(x => x.Type == expectedSemester.Type);
                expectedSemester.Selected = postedSemester?.Selected == true;

                foreach (var expectedPeriod in expectedSemester.Periods)
                {
                    var postedPeriod = postedSemester?.Periods.FirstOrDefault(x => SameName(x.Name, expectedPeriod.Name));
                    expectedPeriod.Selected = postedPeriod?.Selected == true;

                    if (expectedPeriod.Selected && !expectedSemester.Selected)
                        throw new InvalidOperationException("Đợt thi không hợp lệ vì học kỳ cha chưa được chọn.");

                    foreach (var expectedSession in expectedPeriod.Sessions)
                    {
                        var postedSession = postedPeriod?.Sessions.FirstOrDefault(x => SameName(x.Name, expectedSession.Name));
                        expectedSession.Selected = postedSession?.Selected == true;

                        if (expectedSession.Selected && !expectedPeriod.Selected)
                            throw new InvalidOperationException("Buổi thi không hợp lệ vì đợt thi cha chưa được chọn.");

                        foreach (var expectedSlot in expectedSession.Slots)
                        {
                            var postedSlot = postedSession?.Slots.FirstOrDefault(x => SameName(x.Name, expectedSlot.Name) && x.TimeStart == expectedSlot.TimeStart);
                            expectedSlot.Selected = postedSlot?.Selected == true;

                            if (expectedSlot.Selected && !expectedSession.Selected)
                                throw new InvalidOperationException("Ca thi không hợp lệ vì buổi thi cha chưa được chọn.");
                        }
                    }
                }
            }

            if (!expected.Any(x => x.Selected))
                throw new InvalidOperationException("Vui lòng chọn ít nhất một học kỳ.");

            return expected;
        }

        private static bool IsCompleteStructure(AcademyYear year)
        {
            var expected = DefaultDataBuilder.Build().Semesters;

            foreach (var expectedSemester in expected)
            {
                var semester = year.Semesters.FirstOrDefault(x => SemesterHelper.ToType(x.Name) == expectedSemester.Type);
                if (semester == null) return false;

                foreach (var expectedPeriod in expectedSemester.Periods)
                {
                    var period = semester.ExamPeriods.FirstOrDefault(x => SameName(x.Name, expectedPeriod.Name));
                    if (period == null) return false;

                    foreach (var expectedSession in expectedPeriod.Sessions)
                    {
                        var session = period.ExamSessions.FirstOrDefault(x => SameName(x.Name, expectedSession.Name));
                        if (session == null) return false;

                        foreach (var expectedSlot in expectedSession.Slots)
                        {
                            if (!session.ExamSlots.Any(x => SameName(x.Name, expectedSlot.Name) && x.TimeStart == expectedSlot.TimeStart))
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool SameName(string? current, string? expected)
        {
            return string.Equals(current?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
