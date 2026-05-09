using ExamInvigilationManagement.Application.DTOs.Admin.Room;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using System.Text.RegularExpressions;

namespace ExamInvigilationManagement.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _repo;

        public RoomService(IRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<RoomDto>> GetPagedAsync(string? keyword, string? buildingId, int page, int pageSize)
        {
            var data = await _repo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.Building?.Name ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    x.BuildingId.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(buildingId))
            {
                var bid = NormalizeBuildingId(buildingId);
                data = data.Where(x => x.BuildingId == bid).ToList();
            }

            var total = data.Count;

            var items = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoomDto
                {
                    RoomId = x.Id,
                    RoomName = x.Name,
                    Capacity = x.Capacity,
                    BuildingId = x.BuildingId,
                    BuildingName = x.Building?.Name
                }).ToList();

            return new PagedResult<RoomDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RoomDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new RoomDto
            {
                RoomId = x.Id,
                RoomName = x.Name,
                Capacity = x.Capacity,
                BuildingId = x.BuildingId,
                BuildingName = x.Building?.Name
            };
        }

        public async Task<List<RoomDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return data.Select(x => new RoomDto
            {
                RoomId = x.Id,
                RoomName = x.Name,
                BuildingId = x.BuildingId,
                BuildingName = x.Building?.Name,
                Capacity = x.Capacity
            }).ToList();
        }

        public async Task CreateAsync(RoomDto dto)
        {
            var buildingId = NormalizeBuildingId(dto.BuildingId);
            var roomName = NormalizeRoomName(dto.RoomName);

            ValidateBuildingId(buildingId);
            ValidateRoomName(roomName);
            ValidateCapacity(dto.Capacity);

            if (!await _repo.BuildingExistsAsync(buildingId))
                throw new InvalidOperationException("Giảng đường đã chọn không tồn tại.");

            if (await _repo.ExistsByBuildingAndRoomNameAsync(buildingId, roomName))
                throw new InvalidOperationException("Phòng đã tồn tại trong giảng đường này.");

            await _repo.AddAsync(new Domain.Entities.Room
            {
                BuildingId = buildingId,
                Name = roomName,
                Capacity = dto.Capacity
            });
        }

        public async Task UpdateAsync(RoomDto dto)
        {
            var buildingId = NormalizeBuildingId(dto.BuildingId);
            var roomName = NormalizeRoomName(dto.RoomName);

            ValidateBuildingId(buildingId);
            ValidateRoomName(roomName);
            ValidateCapacity(dto.Capacity);

            var existing = await _repo.GetByIdAsync(dto.RoomId);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy phòng cần cập nhật.");

            if (!await _repo.BuildingExistsAsync(buildingId))
                throw new InvalidOperationException("Giảng đường đã chọn không tồn tại.");

            if (await _repo.ExistsByBuildingAndRoomNameAsync(buildingId, roomName, dto.RoomId))
                throw new InvalidOperationException("Phòng đã tồn tại trong giảng đường này.");

            await _repo.UpdateAsync(new Domain.Entities.Room
            {
                Id = dto.RoomId,
                BuildingId = buildingId,
                Name = roomName,
                Capacity = dto.Capacity
            });
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new InvalidOperationException("Không tìm thấy phòng cần xóa.");

            if (await _repo.HasExamSchedulesAsync(id))
                throw new InvalidOperationException("Không thể xóa phòng vì đã có lịch thi sử dụng phòng này.");

            await _repo.DeleteAsync(id);
        }

        private static string NormalizeBuildingId(string? buildingId)
        {
            return (buildingId ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeRoomName(string? roomName)
        {
            var value = (roomName ?? string.Empty).Trim();
            value = Regex.Replace(value, @"\s+", "");
            return value.ToUpperInvariant();
        }

        private static void ValidateBuildingId(string buildingId)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
                throw new InvalidOperationException("Vui lòng chọn giảng đường.");
        }

        private static void ValidateRoomName(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                throw new InvalidOperationException("Vui lòng nhập tên phòng.");

            if (roomName.Length > 5)
                throw new InvalidOperationException("Tên phòng tối đa 5 ký tự.");

            if (!Regex.IsMatch(roomName, @"^[A-Z0-9]+$"))
                throw new InvalidOperationException("Tên phòng chỉ được chứa chữ cái và số, không có khoảng trắng.");
        }

        private static void ValidateCapacity(int? capacity)
        {
            if (capacity.HasValue && (capacity < 1 || capacity > 500))
                throw new InvalidOperationException("Sức chứa phải từ 1 đến 500.");
        }
    }
}