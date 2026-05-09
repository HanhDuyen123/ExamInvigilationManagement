using ExamInvigilationManagement.Application.DTOs.Admin.CourseOffering;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Domain.Enums;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class CourseOfferingService : ICourseOfferingService
    {
        private readonly ICourseOfferingRepository _repo;

        public CourseOfferingService(ICourseOfferingRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<CourseOfferingDto>> GetPagedAsync(
            string? subjectId,
            int? userId,
            int? semesterType,
            string? className,
            string? groupNumber,
            int page,
            int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                var kw = NormalizeText(subjectId);
                data = data.Where(x =>
                    NormalizeText(x.SubjectId).Contains(kw) ||
                    NormalizeText(x.Subject?.Name).Contains(kw))
                    .ToList();
            }

            if (userId.HasValue)
            {
                data = data.Where(x => x.UserId == userId.Value).ToList();
            }

            if (semesterType.HasValue)
            {
                var type = (SemesterType)semesterType.Value;
                data = data.Where(x => x.Semester != null &&
                                       SemesterHelper.ToType(x.Semester.Name) == type)
                           .ToList();
            }

            if (!string.IsNullOrWhiteSpace(className))
            {
                var kw = NormalizeText(className);
                data = data.Where(x => NormalizeText(x.ClassName).Contains(kw)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(groupNumber))
            {
                var kw = NormalizeText(groupNumber);
                data = data.Where(x => NormalizeText(x.GroupNumber).Contains(kw)).ToList();
            }

            var total = data.Count;

            var items = data
                .OrderBy(x => x.SubjectId)
                .ThenBy(x => x.ClassName)
                .ThenBy(x => x.GroupNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CourseOfferingDto
                {
                    Id = x.Id,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject?.Name,
                    UserId = x.UserId,
                    UserName = x.User?.Information != null
                        ? $"{x.User.Information.LastName} {x.User.Information.FirstName}"
                        : x.User?.UserName,
                    SemesterId = x.SemesterId,
                    SemesterName = x.Semester?.Name,
                    AcademicYearName = x.Semester?.AcademyYear?.Name,
                    ClassName = x.ClassName,
                    GroupNumber = x.GroupNumber,
                    FacultyId = x.User?.FacultyId
                })
                .ToList();

            return new PagedResult<CourseOfferingDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<CourseOfferingDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new CourseOfferingDto
            {
                Id = x.Id,
                UserId = x.UserId,
                SemesterId = x.SemesterId,
                SubjectId = x.SubjectId,
                ClassName = x.ClassName,
                GroupNumber = x.GroupNumber,
                SubjectName = x.Subject?.Name,
                UserName = x.User?.Information != null
                    ? $"{x.User.Information.LastName} {x.User.Information.FirstName}"
                    : x.User?.UserName,
                SemesterName = x.Semester?.Name,
                AcademicYearName = x.Semester?.AcademyYear?.Name,
                FacultyId = x.User?.FacultyId
            }).ToList();
        }

        public async Task<CourseOfferingDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new CourseOfferingDto
            {
                Id = x.Id,
                UserId = x.UserId,
                SemesterId = x.SemesterId,
                AcademyYearId = x.Semester?.AcademyYear?.Id,
                SubjectId = x.SubjectId,
                ClassName = x.ClassName,
                GroupNumber = x.GroupNumber,
                SubjectName = x.Subject?.Name,
                UserName = x.User?.Information != null
                    ? $"{x.User.Information.LastName} {x.User.Information.FirstName}"
                    : x.User?.UserName,
                SemesterName = x.Semester?.Name,
                AcademicYearName = x.Semester?.AcademyYear?.Name,
                FacultyId = x.User?.FacultyId,
                FacultyName = x.User?.Faculty?.Name
            };
        }

        public async Task CreateAsync(CourseOfferingDto dto)
        {
            await ValidateAndNormalizeAsync(dto, isUpdate: false);

            if (await _repo.ExistsByUniqueKeyAsync(dto.SemesterId!.Value, dto.SubjectId!, dto.ClassName!, dto.GroupNumber!))
                throw new InvalidOperationException("Học phần mở này đã tồn tại.");

            await _repo.AddAsync(new CourseOffering
            {
                UserId = dto.UserId!.Value,
                SemesterId = dto.SemesterId!.Value,
                SubjectId = dto.SubjectId!,
                ClassName = dto.ClassName!,
                GroupNumber = dto.GroupNumber!
            });
        }

        public async Task UpdateAsync(CourseOfferingDto dto)
        {
            await ValidateAndNormalizeAsync(dto, isUpdate: true);

            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy học phần mở cần cập nhật.");

            if (await _repo.ExistsByUniqueKeyAsync(dto.SemesterId!.Value, dto.SubjectId!, dto.ClassName!, dto.GroupNumber!, dto.Id))
                throw new InvalidOperationException("Học phần mở này đã tồn tại.");

            await _repo.UpdateAsync(new CourseOffering
            {
                Id = dto.Id,
                UserId = dto.UserId!.Value,
                SemesterId = dto.SemesterId!.Value,
                SubjectId = dto.SubjectId!,
                ClassName = dto.ClassName!,
                GroupNumber = dto.GroupNumber!
            });
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy học phần mở cần xóa.");

            if (await _repo.HasExamSchedulesAsync(id))
                throw new InvalidOperationException("Không thể xóa học phần mở vì đã có lịch thi sử dụng.");

            await _repo.DeleteAsync(id);
        }

        private async Task ValidateAndNormalizeAsync(CourseOfferingDto dto, bool isUpdate)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            dto.SubjectId = NormalizeId(dto.SubjectId);
            dto.ClassName = NormalizeClassName(dto.ClassName);
            dto.GroupNumber = NormalizeGroupNumber(dto.GroupNumber);

            if (!dto.UserId.HasValue || dto.UserId.Value <= 0)
                throw new InvalidOperationException("Vui lòng chọn giảng viên.");

            if (!dto.SemesterId.HasValue || dto.SemesterId.Value <= 0)
                throw new InvalidOperationException("Vui lòng chọn học kỳ.");

            if (string.IsNullOrWhiteSpace(dto.SubjectId))
                throw new InvalidOperationException("Vui lòng chọn môn học.");

            if (string.IsNullOrWhiteSpace(dto.ClassName))
                throw new InvalidOperationException("Vui lòng nhập lớp học phần.");

            if (string.IsNullOrWhiteSpace(dto.GroupNumber))
                throw new InvalidOperationException("Vui lòng nhập nhóm học phần.");

            if (dto.SubjectId.Length > 10)
                throw new InvalidOperationException("Mã môn học tối đa 10 ký tự.");

            if (dto.ClassName.Length > 10)
                throw new InvalidOperationException("Lớp học phần tối đa 10 ký tự.");

            if (dto.GroupNumber.Length > 2)
                throw new InvalidOperationException("Nhóm học phần tối đa 2 ký tự.");

            if (!Regex.IsMatch(dto.SubjectId, @"^[A-Z0-9]+$"))
                throw new InvalidOperationException("Mã môn học chỉ được chứa chữ cái và số, không có khoảng trắng.");

            if (!Regex.IsMatch(dto.ClassName, @"^[A-Z0-9.\-]+$"))
                throw new InvalidOperationException("Lớp học phần chỉ được chứa chữ cái, số, dấu chấm và dấu gạch ngang, không có khoảng trắng.");

            if (!Regex.IsMatch(dto.GroupNumber, @"^[A-Z0-9]+$"))
                throw new InvalidOperationException("Nhóm học phần chỉ được chứa chữ cái và số, không có khoảng trắng.");

            var user = await _repo.UserExistsAsync(dto.UserId.Value);
            if (!user)
                throw new InvalidOperationException("Giảng viên đã chọn không tồn tại.");

            if (!await _repo.IsUserActiveLecturerAsync(dto.UserId.Value))
                throw new InvalidOperationException("Giảng viên đã chọn không còn hoạt động hoặc không đúng vai trò.");

            var subjectExists = await _repo.SubjectExistsAsync(dto.SubjectId);
            if (!subjectExists)
                throw new InvalidOperationException("Môn học đã chọn không tồn tại.");

            var semesterExists = await _repo.SemesterExistsAsync(dto.SemesterId.Value);
            if (!semesterExists)
                throw new InvalidOperationException("Học kỳ đã chọn không tồn tại.");

            var subjectFacultyId = await _repo.GetSubjectFacultyIdAsync(dto.SubjectId);
            var userFacultyId = await _repo.GetUserFacultyIdAsync(dto.UserId.Value);

            if (subjectFacultyId.HasValue && userFacultyId.HasValue && subjectFacultyId.Value != userFacultyId.Value)
                throw new InvalidOperationException("Giảng viên và môn học phải thuộc cùng khoa.");

            if (dto.AcademyYearId.HasValue)
            {
                var semesterAcademyYearId = await _repo.GetSemesterAcademyYearIdAsync(dto.SemesterId.Value);
                if (semesterAcademyYearId.HasValue && semesterAcademyYearId.Value != dto.AcademyYearId.Value)
                    throw new InvalidOperationException("Học kỳ đã chọn không thuộc năm học đã chọn.");
            }
        }

        private static string NormalizeId(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeClassName(string? value)
        {
            var result = (value ?? string.Empty).Trim();
            result = Regex.Replace(result, @"\s+", "");
            return result.ToUpperInvariant();
        }

        private static string NormalizeGroupNumber(string? value)
        {
            var result = (value ?? string.Empty).Trim();
            result = Regex.Replace(result, @"\s+", "");
            return result.ToUpperInvariant();
        }

        private static string NormalizeText(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
