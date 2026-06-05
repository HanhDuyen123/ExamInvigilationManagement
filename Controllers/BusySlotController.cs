using ExamInvigilationManagement.Application.DTOs.LecturerBusySlot;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize(Roles = "Admin,Trưởng khoa,Thư ký khoa,Giảng viên")]
    public class BusySlotController : BaseRoleController
    {
        private readonly ILecturerBusySlotService _service;

        public BusySlotController(
            ILecturerBusySlotService service,
            IAdminUserService userService
        ) : base(userService)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            ViewBag.ShowUserFilter = !User.IsInRole("Giảng viên");
            ViewBag.ShowActionColumn = User.IsInRole("Giảng viên");
            ViewBag.ShowApprovalActions = User.IsInRole("Trưởng khoa");
            ViewBag.ShowBusyBulkApprovalToolbar = User.IsInRole("Trưởng khoa");

            var vm = new CrudIndexViewModel
            {
                Title = "Lịch bận giảng viên",
                Subtitle = "Ghi nhận những khoảng thời gian giảng viên không thể tham gia coi thi.",
                CreateUrl = @Url.Action("Create", "BusySlot") ?? "#",
                SearchPartialView = "_BusySlotSearch",
                TableClass = "full-width",
                ShowCreateButton = User.IsInRole("Giảng viên"),
                ImportUrl = User.IsInRole("Admin")
                    ? null
                    : Url.Action("Index", "BulkImport", new { area = "", module = "lecturer-busy-slot" })
            };

            return View(vm);
        }



        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? keyword)
        {
            var paged = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
            var users = paged.Items.AsEnumerable();

            // Chỉ lấy 3 role được phép hiện trong BusySlot
            users = users.Where(x =>
                x.RoleName == "Giảng viên").ToList();

            if (User.IsInRole("Giảng viên"))
            {
                var currentUserId = GetCurrentUserId();
                users = users.Where(x => x.Id == currentUserId);
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                users = users.Where(x => x.FacultyId == currentFacultyId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(x =>
                    (x.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            return Json(users.Select(x => new
            {
                id = x.Id,
                name = string.IsNullOrWhiteSpace(x.FullName)
                    ? x.UserName
                    : $"{x.UserName} - {x.FullName}"
            }));
        }

        public async Task<IActionResult> GetList(
            string? keyword,
            int? userId,
            int? facultyId,
            int? academyYearId,
            int? semesterId,
            int? examPeriodId,
            int? examSessionId,
            int? examSlotId,
            string? approvalStatus,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page = 1,
            int pageSize = 20)
        {
            ViewBag.ShowActionColumn = User.IsInRole("Giảng viên");
            ViewBag.ShowApprovalActions = User.IsInRole("Trưởng khoa");
            var filter = await BuildScopeFilter(
                keyword, userId, facultyId, academyYearId, semesterId,
                examPeriodId, examSessionId, examSlotId, approvalStatus, fromDate, toDate);

            var result = await _service.GetPagedAsync(filter, page, pageSize);
            return PartialView("_BusySlotTable", result);
        }

        [Authorize(Roles = "Giảng viên")]
        public IActionResult Create()
        {
            return View(new LecturerBusySlotDto
            {
                BusyDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost]
        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Create(LecturerBusySlotDto dto)
        {
            dto.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var count = await _service.CreateManyAsync(dto);
                try
                {
                    await _service.NotifyBusyRegistrationAsync(dto, count, HttpContext.RequestAborted);
                }
                catch
                {
                    // Đăng ký đã lưu thành công; lỗi notification không được làm hỏng luồng chính.
                }
                TempData.SetNotification("success", $"Đăng ký lịch bận thành công cho {count} ca.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            if (!CanLecturerEdit(data)) return Forbid();

            return View(data);
        }

        [HttpPost]
        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Edit(LecturerBusySlotDto dto)
        {
            dto.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật lịch bận thành công!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [Authorize(Roles = "Giảng viên")]
        public async Task<IActionResult> Delete(int id)
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (!CanLecturerEdit(current)) return Forbid();

            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xoá thành công!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Trưởng khoa")]
        public async Task<IActionResult> BulkApprove(List<int> selectedIds)
        {
            var result = await HandleBulkApprovalAsync(selectedIds, true, null);
            TempData.SetNotification(result.SuccessCount > 0 ? "success" : "error", result.Message);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Trưởng khoa")]
        public async Task<IActionResult> BulkReject(List<int> selectedIds, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData.SetNotification("error", "Vui lòng nhập lý do từ chối.");
                return RedirectToAction(nameof(Index));
            }

            var result = await HandleBulkApprovalAsync(selectedIds, false, reason.Trim());
            TempData.SetNotification(result.SuccessCount > 0 ? "success" : "error", result.Message);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Trưởng khoa")]
        public async Task<IActionResult> Approve(int id)
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (!await CanViewAsync(current)) return Forbid();

            await _service.ApproveAsync(id, GetCurrentUserId() ?? 0);
            TempData.SetNotification("success", "Đã duyệt lịch bận.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Trưởng khoa")]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (!await CanViewAsync(current)) return Forbid();

            try
            {
                await _service.RejectAsync(id, GetCurrentUserId() ?? 0, reason);
                TempData.SetNotification("success", "Đã từ chối lịch bận.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<(int SuccessCount, string Message)> HandleBulkApprovalAsync(List<int>? selectedIds, bool approve, string? reason)
        {
            var ids = (selectedIds ?? new List<int>()).Distinct().ToList();
            if (ids.Count == 0) return (0, "Vui lòng chọn ít nhất một lịch bận.");

            var success = 0;
            var skipped = 0;
            var approverId = GetCurrentUserId() ?? 0;

            foreach (var id in ids)
            {
                var current = await _service.GetByIdAsync(id);
                if (current == null || !await CanViewAsync(current) || current.ApprovalStatus != "Chờ duyệt")
                {
                    skipped++;
                    continue;
                }

                if (approve) await _service.ApproveAsync(id, approverId);
                else await _service.RejectAsync(id, approverId, reason ?? string.Empty);
                success++;
            }

            if (success == 0) return (0, "Không có lịch bận hợp lệ để xử lý.");

            var action = approve ? "duyệt" : "từ chối";
            var message = $"Đã {action} {success} lịch bận.";
            if (skipped > 0) message += $" Bỏ qua {skipped} lịch không hợp lệ hoặc không còn chờ duyệt.";
            return (success, message);
        }

        public async Task<IActionResult> Details(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewAsync(data)) return Forbid();

            return View(data);
        }

        private async Task<LecturerBusySlotSearchDto> BuildScopeFilter(
            string? keyword,
            int? userId,
            int? facultyId,
            int? academyYearId,
            int? semesterId,
            int? examPeriodId,
            int? examSessionId,
            int? examSlotId,
            string? approvalStatus,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            var filter = new LecturerBusySlotSearchDto
            {
                Keyword = keyword,
                UserId = userId,
                FacultyId = facultyId,
                AcademyYearId = academyYearId,
                SemesterId = semesterId,
                ExamPeriodId = examPeriodId,
                ExamSessionId = examSessionId,
                ExamSlotId = examSlotId,
                ApprovalStatus = approvalStatus,
                FromDate = fromDate,
                ToDate = toDate
            };

            if (User.IsInRole("Giảng viên"))
            {
                filter.UserId = GetCurrentUserId();
                filter.FacultyId = null;
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                filter.UserId = null;
                filter.FacultyId = await GetCurrentFacultyIdAsync();
            }
            else if (User.IsInRole("Admin"))
            {
                filter.UserId = null;
                filter.FacultyId = null;
            }
            return filter;
        }

        private bool CanLecturerEdit(LecturerBusySlotDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return currentUserId.HasValue && dto.UserId == currentUserId.Value;
        }
        private async Task<bool> CanViewAsync(LecturerBusySlotDto dto)
        {
            if (User.IsInRole("Admin")) return true;
            if (User.IsInRole("Giảng viên")) return CanLecturerEdit(dto);

            if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var facultyId = await GetCurrentFacultyIdAsync();
                return facultyId.HasValue && dto.FacultyId == facultyId.Value;
            }

            return false;
        }
    }
}
