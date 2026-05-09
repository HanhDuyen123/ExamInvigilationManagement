using ExamInvigilationManagement.Application.DTOs.Admin.Information;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class InformationService : IInformationService
    {
        private static readonly HashSet<string> AllowedGenders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Male", "Female", "Other"
        };

        private readonly IInformationRepository _repo;

        public InformationService(IInformationRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<InformationDto>> GetPagedAsync(
            string? name,
            string? email,
            string? gender,
            DateTime? dob,
            byte? positionId,
            int page,
            int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var kw = NormalizeText(name);
                data = data.Where(x =>
                    NormalizeText($"{x.LastName} {x.FirstName}")
                        .Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var kw = email.Trim();
                data = data.Where(x =>
                    x.Email.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(gender))
            {
                data = data.Where(x =>
                    string.Equals(x.Gender, gender, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (dob.HasValue)
            {
                data = data.Where(x => x.Dob.HasValue && x.Dob.Value.Date == dob.Value.Date).ToList();
            }

            if (positionId.HasValue)
            {
                data = data.Where(x => x.PositionId == positionId.Value).ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InformationDto
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    Gender = x.Gender,
                    Dob = x.Dob,
                    PositionId = x.PositionId,
                    PositionName = x.Position?.Name
                })
                .ToList();

            return new PagedResult<InformationDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<InformationDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new InformationDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                Dob = x.Dob,
                Gender = x.Gender,
                PositionId = x.PositionId,
                PositionName = x.Position?.Name
            };
        }

        public async Task<List<InformationDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new InformationDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PositionId = x.PositionId,
                PositionName = x.Position?.Name
            }).ToList();
        }

        public async Task CreateAsync(InformationDto dto)
        {
            var entity = BuildEntity(dto, isUpdate: false);
            ValidateEntity(entity, isUpdate: false, dto.Id);

            if (await _repo.ExistsByEmailAsync(entity.Email))
                throw new InvalidOperationException("Email đã tồn tại.");

            await _repo.AddAsync(entity);
        }

        public async Task UpdateAsync(InformationDto dto)
        {
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ cần cập nhật.");

            var entity = BuildEntity(dto, isUpdate: true);
            ValidateEntity(entity, isUpdate: true, dto.Id);

            if (await _repo.ExistsByEmailAsync(entity.Email, excludeId: dto.Id))
                throw new InvalidOperationException("Email đã tồn tại.");

            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ cần xóa.");

            if (await _repo.HasUsersAsync(id))
                throw new InvalidOperationException("Không thể xóa hồ sơ vì đang được tài khoản hệ thống sử dụng.");

            await _repo.DeleteAsync(id);
        }

        private static Information BuildEntity(InformationDto dto, bool isUpdate)
        {
            return new Information
            {
                Id = dto.Id,
                FirstName = NormalizeName(dto.FirstName),
                LastName = NormalizeName(dto.LastName),
                Email = NormalizeEmail(dto.Email),
                Phone = NormalizePhone(dto.Phone),
                Address = NormalizeText(dto.Address),
                Dob = dto.Dob?.Date,
                Gender = NormalizeGender(dto.Gender),
                Avt = string.IsNullOrWhiteSpace(dto.Avt) ? null : dto.Avt.Trim(),
                PositionId = dto.PositionId
            };
        }

        private static void ValidateEntity(Information entity, bool isUpdate, int id)
        {
            if (string.IsNullOrWhiteSpace(entity.FirstName))
                throw new InvalidOperationException("Vui lòng nhập tên.");

            if (entity.FirstName.Length > 50)
                throw new InvalidOperationException("Tên tối đa 50 ký tự.");

            if (string.IsNullOrWhiteSpace(entity.LastName))
                throw new InvalidOperationException("Vui lòng nhập họ.");

            if (entity.LastName.Length > 50)
                throw new InvalidOperationException("Họ tối đa 50 ký tự.");

            if (string.IsNullOrWhiteSpace(entity.Email))
                throw new InvalidOperationException("Vui lòng nhập email.");

            if (entity.Email.Length > 100)
                throw new InvalidOperationException("Email tối đa 100 ký tự.");

            if (!new EmailAddressAttribute().IsValid(entity.Email))
                throw new InvalidOperationException("Email không đúng định dạng.");

            if (!string.IsNullOrWhiteSpace(entity.Phone))
            {
                if (entity.Phone.Length > 10)
                    throw new InvalidOperationException("Số điện thoại tối đa 10 ký tự.");

                if (!Regex.IsMatch(entity.Phone, @"^\d{10}$"))
                    throw new InvalidOperationException("Số điện thoại phải gồm đúng 10 chữ số.");
            }

            if (!string.IsNullOrWhiteSpace(entity.Address) && entity.Address.Length > 255)
                throw new InvalidOperationException("Địa chỉ tối đa 255 ký tự.");

            if (entity.Dob.HasValue)
            {
                var dob = entity.Dob.Value.Date;
                if (dob > DateTime.Today)
                    throw new InvalidOperationException("Ngày sinh không được lớn hơn ngày hiện tại.");

                if (dob < DateTime.Today.AddYears(-120))
                    throw new InvalidOperationException("Ngày sinh không hợp lệ.");
            }

            if (!string.IsNullOrWhiteSpace(entity.Gender) &&
                !AllowedGenders.Contains(entity.Gender))
            {
                throw new InvalidOperationException("Giới tính không hợp lệ.");
            }

            if (entity.PositionId <= 0)
                throw new InvalidOperationException("Vui lòng chọn chức vụ.");
        }

        private static string NormalizeName(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        private static string NormalizeText(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        private static string NormalizeEmail(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string? NormalizePhone(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static string? NormalizeGender(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
