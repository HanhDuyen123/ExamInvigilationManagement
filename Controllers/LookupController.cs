using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize(Roles = "Admin,Trưởng khoa,Thư ký khoa,Giảng viên")]
    public class LookupController : BaseRoleController
    {
        private readonly IAcademyYearService _academyYearService;
        private readonly ISemesterService _semesterService;
        private readonly IPeriodService _periodService;
        private readonly ISessionService _sessionService;
        private readonly ISlotService _slotService;
        private readonly ISubjectService _subjectService;
        private readonly IBuildingService _buildingService;
        private readonly IRoomService _roomService;

        public LookupController(
            IAcademyYearService academyYearService,
            ISemesterService semesterService,
            IPeriodService periodService,
            ISessionService sessionService,
            ISlotService slotService,
            ISubjectService subjectService,
            IBuildingService buildingService,
            IRoomService roomService,
            IAdminUserService userService) : base(userService)
        {
            _academyYearService = academyYearService;
            _semesterService = semesterService;
            _periodService = periodService;
            _sessionService = sessionService;
            _slotService = slotService;
            _subjectService = subjectService;
            _buildingService = buildingService;
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> AcademyYears(string? keyword)
        {
            var data = await _academyYearService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                data = data.Where(x =>
                    (x.Name ?? string.Empty).Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> Semesters(int academyYearId, string? keyword)
        {
            var data = await _semesterService.GetAllAsync();
            var result = data.Where(x => x.AcademyYearId == academyYearId);

            if (!string.IsNullOrWhiteSpace(keyword))
                result = result.Where(x => (x.Name ?? string.Empty).Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase));

            return Json(result.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> Periods(int semesterId)
        {
            var data = await _periodService.GetAllBySemesterAsync(semesterId);
            return Json(data.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> Sessions(int periodId)
        {
            var data = await _sessionService.GetAllByPeriodAsync(periodId);
            return Json(data.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> Slots(int sessionId)
        {
            var data = await _slotService.GetAllBySessionAsync(sessionId);
            return Json(data.Select(x => new { id = x.Id, name = x.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> Subjects(string? keyword, int? facultyId)
        {
            var subjects = (await _subjectService.GetAllAsync()).AsEnumerable();

            if (IsAdmin())
            {
                if (facultyId.HasValue)
                    subjects = subjects.Where(x => x.FacultyId == facultyId.Value);
            }
            else
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                subjects = currentFacultyId.HasValue
                    ? subjects.Where(x => x.FacultyId == currentFacultyId.Value)
                    : subjects.Where(x => false);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                subjects = subjects.Where(x =>
                    (x.Id ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.Name ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            return Json(subjects.Select(x => new { id = x.Id, name = $"{x.Id} - {x.Name}" }));
        }

        [HttpGet]
        public async Task<IActionResult> Buildings(string? keyword)
        {
            var data = await _buildingService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    (x.BuildingName ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.BuildingId ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new { id = x.BuildingId, name = x.BuildingName }));
        }

        [HttpGet]
        public async Task<IActionResult> Rooms(string? buildingId, string? keyword)
        {
            var data = await _roomService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(buildingId))
                data = data.Where(x => x.BuildingId == buildingId).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                data = data.Where(x =>
                    (x.RoomName ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (x.BuildingName ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(data.Select(x => new { id = x.RoomId, name = $"{x.BuildingId}.{x.RoomName}" }));
        }
    }
}
