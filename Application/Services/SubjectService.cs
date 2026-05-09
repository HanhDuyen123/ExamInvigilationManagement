using ExamInvigilationManagement.Application.DTOs.Admin.Subject;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _repo;

        public SubjectService(ISubjectRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SubjectDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new SubjectDto
            {
                Id = x.Id,
                Name = x.Name,
                Credit = x.Credit,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty?.Name
            }).ToList();
        }

        public async Task<PagedResult<SubjectDto>> GetPagedAsync(
            string? id,
            string? name,
            byte? credit,
            int? facultyId,
            int page,
            int pageSize)
        {
            var paged = await _repo.GetPagedAsync(id, name, credit, facultyId, page, pageSize);

            var items = paged.Items.Select(x => new SubjectDto
            {
                Id = x.Id,
                Name = x.Name,
                Credit = x.Credit,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty?.Name
            }).ToList();

            return new PagedResult<SubjectDto>
            {
                Items = items,
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task<SubjectDto?> GetByIdAsync(string id)
        {
            var normalizedId = NormalizeId(id);
            if (string.IsNullOrWhiteSpace(normalizedId))
                return null;

            var entity = await _repo.GetByIdAsync(normalizedId);
            if (entity == null)
                return null;

            return new SubjectDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Credit = entity.Credit,
                FacultyId = entity.FacultyId,
                FacultyName = entity.Faculty?.Name
            };
        }

        public async Task CreateAsync(SubjectDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            NormalizeAndValidate(dto, isCreate: true);

            if (!await _repo.FacultyExistsAsync(dto.FacultyId!.Value))
                throw new InvalidOperationException("Khoa đã chọn không tồn tại.");

            if (await _repo.ExistsByIdAsync(dto.Id))
                throw new InvalidOperationException("Mã môn học đã tồn tại.");

            await _repo.AddAsync(new Subject
            {
                Id = dto.Id,
                Name = dto.Name,
                Credit = dto.Credit!.Value,
                FacultyId = dto.FacultyId.Value
            });
        }

        public async Task UpdateAsync(SubjectDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            NormalizeAndValidate(dto, isCreate: false);

            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy môn học cần cập nhật.");

            if (!await _repo.FacultyExistsAsync(dto.FacultyId!.Value))
                throw new InvalidOperationException("Khoa đã chọn không tồn tại.");

            await _repo.UpdateAsync(new Subject
            {
                Id = dto.Id,
                Name = dto.Name,
                Credit = dto.Credit!.Value,
                FacultyId = dto.FacultyId.Value
            });
        }

        public async Task DeleteAsync(string id)
        {
            var normalizedId = NormalizeId(id);

            if (string.IsNullOrWhiteSpace(normalizedId))
                throw new InvalidOperationException("Mã môn học không hợp lệ.");

            var existing = await _repo.GetByIdAsync(normalizedId);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy môn học cần xóa.");

            if (await _repo.HasCourseOfferingsAsync(normalizedId))
                throw new InvalidOperationException("Không thể xóa môn học vì đã có học phần mở sử dụng môn này.");

            await _repo.DeleteAsync(normalizedId);
        }

        private static void NormalizeAndValidate(SubjectDto dto, bool isCreate)
        {
            dto.Id = NormalizeId(dto.Id);
            dto.Name = NormalizeName(dto.Name);

            if (string.IsNullOrWhiteSpace(dto.Id))
                throw new InvalidOperationException("Vui lòng nhập mã môn học.");

            if (dto.Id.Length > 10)
                throw new InvalidOperationException("Mã môn học tối đa 10 ký tự.");

            if (!Regex.IsMatch(dto.Id, @"^[A-Z0-9]+$"))
                throw new InvalidOperationException("Mã môn học chỉ được chứa chữ cái và số, không có khoảng trắng.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Vui lòng nhập tên môn học.");

            if (dto.Name.Length > 100)
                throw new InvalidOperationException("Tên môn học tối đa 100 ký tự.");

            if (!dto.FacultyId.HasValue || dto.FacultyId.Value <= 0)
                throw new InvalidOperationException("Vui lòng chọn khoa.");

            if (!dto.Credit.HasValue)
                throw new InvalidOperationException("Vui lòng nhập số tín chỉ.");

            if (dto.Credit.Value < 1 || dto.Credit.Value > 20)
                throw new InvalidOperationException("Số tín chỉ phải từ 1 đến 20.");
        }

        private static string NormalizeId(string? id)
        {
            return (id ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeName(string? name)
        {
            var value = (name ?? string.Empty).Trim();
            value = Regex.Replace(value, @"\s+", " ");
            return value;
        }
    }
}