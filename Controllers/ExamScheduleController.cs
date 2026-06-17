using System.Globalization;
using System.Security.Claims;
using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.DTOs.InvigilatorResponse;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamInvigilationManagement.Controllers
{
    [Authorize]
    public class ExamScheduleController : BaseRoleController
    {
        private readonly IExamScheduleService _service;
        private readonly ICourseOfferingService _courseOfferingService;
        private readonly INotificationService _notificationService;
        private readonly IInvigilatorResponseService _invigilatorResponseService;
        private readonly IEmailService _emailService;
        private readonly IEmailLogService _emailLogService;
        private readonly IEmailConfigurationService _emailConfigurationService;
        private readonly IExamScheduleDocumentService _documentService;
        private readonly ITemplatePathService _templatePathService;
        private readonly IRequestContextService _requestContextService;
        private readonly ICurrentAcademicContextService _currentAcademicContextService;

        public ExamScheduleController(
            IExamScheduleService service,
            ICourseOfferingService courseOfferingService,
            IAdminUserService userService,
            INotificationService notificationService,
            IInvigilatorResponseService invigilatorResponseService,
            IEmailService emailService,
            IEmailLogService emailLogService,
            IEmailConfigurationService emailConfigurationService,
            IExamScheduleDocumentService documentService,
            ITemplatePathService templatePathService,
            IRequestContextService requestContextService,
            ICurrentAcademicContextService currentAcademicContextService)
            : base(userService)
        {
            _service = service;
            _courseOfferingService = courseOfferingService;
            _notificationService = notificationService;
            _invigilatorResponseService = invigilatorResponseService;
            _emailService = emailService;
            _emailLogService = emailLogService;
            _emailConfigurationService = emailConfigurationService;
            _documentService = documentService;
            _templatePathService = templatePathService;
            _requestContextService = requestContextService;
            _currentAcademicContextService = currentAcademicContextService;
        }

        public async Task<IActionResult> Index(string? status = null)
        {
            var scope = await BuildScopeAsync();
            await SetInitialAcademicContextAsync(scope);
            ViewBag.ShowFacultyFilter = User.IsInRole("Admin");
            ViewBag.ShowUserFilter = User.IsInRole("Admin") || User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa");
            ViewBag.ShowStatusFilter = !User.IsInRole("Giảng viên");
            ViewBag.InitialStatus = status;
            //ViewBag.ShowActionColumn = User.IsInRole("Admin");
            ViewBag.ShowCreateButton = User.IsInRole("Admin");
            ViewBag.CanSendApprovalRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendConfirmationRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendSupportRequest = User.IsInRole("Thư ký khoa");
            ViewBag.ScheduleActionMode = User.IsInRole("Admin")
                ? "admin"
                : User.IsInRole("Thư ký khoa")
                    ? "secretary"
                    : "view";
            ViewBag.CanSendApprovalRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendConfirmationRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendSupportRequest = User.IsInRole("Thư ký khoa");
            ViewBag.AvailabilityUrl = User.IsInRole("Thư ký khoa")
                ? Url.Action("Availability", "LecturerManagement", new { area = "" })
                : null;

            var vm = new CrudIndexViewModel
            {
                Title = "Lịch thi",
                Subtitle = "Theo dõi thời gian, phòng thi và giám thị cho từng buổi thi.",
                CreateUrl = Url.Action("Create", "ExamSchedule") ?? "#",
                SearchPartialView = "_ExamScheduleSearch",
                TableClass = "full-width",
                ShowCreateButton = User.IsInRole("Admin"),
                ImportUrl = User.IsInRole("Admin")
                    ? Url.Action("Index", "BulkImport", new { area = "", module = "exam-schedule" })
                    : User.IsInRole("Thư ký khoa")
                        ? Url.Action("Index", "BulkImport", new { area = "", module = "exam-invigilator" })
                        : null,
                ExportUrl = Url.Action("Export", "ExamSchedule")
            };

            
            return View(vm);
        }

        public async Task<IActionResult> GetList(
            string? keyword,
            int? facultyId,
            int? userId,
            int? academyYearId,
            int? semesterId,
            int? periodId,
            int? sessionId,
            int? slotId,
            string? subjectId,
            string? className,
            string? groupNumber,
            string? buildingId,
            int? roomId,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page = 1,
            int pageSize = 5)
        {
            var scope = await BuildScopeAsync();

            var filter = new ExamScheduleSearchDto
            {
                Keyword = keyword,
                FacultyId = facultyId,
                UserId = userId,
                AcademyYearId = academyYearId,
                SemesterId = semesterId,
                PeriodId = periodId,
                SessionId = sessionId,
                SlotId = slotId,
                SubjectId = subjectId,
                ClassName = className,
                GroupNumber = groupNumber,
                BuildingId = buildingId,
                RoomId = roomId,
                Status = User.IsInRole("Giảng viên") ? null : status,
                FromDate = fromDate,
                ToDate = toDate,
                CurrentRole = scope.Role,
                CurrentUserId = scope.UserId,
                CurrentFacultyId = scope.FacultyId
            };

            ViewBag.ScheduleActionMode = User.IsInRole("Admin")
                ? "admin"
                : User.IsInRole("Thư ký khoa")
                    ? "secretary"
                    : "view";

            ViewBag.CanSendApprovalRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendConfirmationRequest = User.IsInRole("Thư ký khoa");
            ViewBag.CanSendSupportRequest = User.IsInRole("Thư ký khoa");

            var result = await _service.GetPagedAsync(filter, page, pageSize);
            return PartialView("_ExamScheduleTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Export(
            string? keyword,
            int? facultyId,
            int? userId,
            int? academyYearId,
            int? semesterId,
            int? periodId,
            int? sessionId,
            int? slotId,
            string? subjectId,
            string? className,
            string? groupNumber,
            int? examFormatId,
            string? buildingId,
            int? roomId,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            var scope = await BuildScopeAsync();
            var filter = new ExamScheduleSearchDto
            {
                Keyword = keyword,
                FacultyId = facultyId,
                UserId = userId,
                AcademyYearId = academyYearId,
                SemesterId = semesterId,
                PeriodId = periodId,
                SessionId = sessionId,
                SlotId = slotId,
                SubjectId = subjectId,
                ClassName = className,
                GroupNumber = groupNumber,
                ExamFormatId = examFormatId,
                BuildingId = buildingId,
                RoomId = roomId,
                Status = User.IsInRole("Giảng viên") ? null : status,
                FromDate = fromDate,
                ToDate = toDate,
                CurrentRole = scope.Role,
                CurrentUserId = scope.UserId,
                CurrentFacultyId = scope.FacultyId
            };

            var result = await _service.GetPagedAsync(filter, 1, int.MaxValue);
            var templatePath = _templatePathService.GetTemplatePath("MAU EXPORT LICH PHAN CONG COI THI.xlsx");
            var fileBytes = _documentService.BuildExamScheduleExportExcel(result.Items.ToList(), templatePath);
            var fileName = $"lich-thi-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> SearchLecturer(string? keyword, int? facultyId)
        {
            var paged = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
            var users = paged.Items.AsEnumerable();

            // Chỉ lấy giảng viên
            users = users.Where(x => x.RoleName == "Giảng viên").ToList();

            if (User.IsInRole("Giảng viên"))
            {
                var currentUserId = GetCurrentUserId();
                users = users.Where(x => x.Id == currentUserId).ToList();
            }
            else if (User.IsInRole("Thư ký khoa") || User.IsInRole("Trưởng khoa"))
            {
                var currentFacultyId = await GetCurrentFacultyIdAsync();
                users = users.Where(x => x.FacultyId == currentFacultyId).ToList();
            }
            else if (User.IsInRole("Admin") && facultyId.HasValue)
            {
                users = users.Where(x => x.FacultyId == facultyId.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(x =>
                    (x.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(users.Select(x => new
            {
                id = x.Id,
                name = string.IsNullOrWhiteSpace(x.FullName)
                    ? x.UserName
                    : $"{x.UserName} - {x.FullName}"
            }));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new ExamScheduleDto
            {
                ExamDate = DateTime.Today,
                Status = ExamScheduleStatusHelper.WaitingAssign
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ExamScheduleDto dto)
        {
            foreach (var key in ModelState.Keys.Where(x => x == nameof(dto.RoomIds) || x.StartsWith($"{nameof(dto.RoomIds)}[", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", "Vui lòng kiểm tra lại dữ liệu nhập vào.");
                dto.RoomCount = Math.Max(dto.RoomCount, dto.RoomIds?.Count(x => x > 0) ?? 0);
                await HydrateCreateLookupsAsync(dto);
                return View(dto);
            }

            try
            {
                await _service.CreateAsync(dto);
                TempData.SetNotification("success", "Tạo lịch thi thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData.SetNotification("error", ex.Message);
                dto.RoomCount = Math.Max(dto.RoomCount, dto.RoomIds?.Count(x => x > 0) ?? 0);
                await HydrateCreateLookupsAsync(dto);
                return View(dto);
            }
        }

        private async Task HydrateCreateLookupsAsync(ExamScheduleDto dto)
        {
            if (dto.OfferingId.HasValue)
            {
                var offering = await _courseOfferingService.GetByIdAsync(dto.OfferingId.Value);
                if (offering != null)
                {
                    dto.SubjectId = offering.SubjectId;
                    dto.SubjectName = offering.SubjectName;
                    dto.UserName = offering.UserName;
                    dto.AcademyYearName = offering.AcademicYearName;
                    dto.SemesterName = offering.SemesterName;
                    dto.ClassName = offering.ClassName;
                    dto.GroupNumber = offering.GroupNumber;
                    dto.OfferingAcademyYearId = offering.AcademyYearId;
                    dto.OfferingSemesterId = offering.SemesterId;
                }
            }

            if (dto.ExamFormatId.HasValue)
            {
                var format = (await _service.GetExamFormatsAsync()).FirstOrDefault(x => x.Id == dto.ExamFormatId.Value);
                if (format != null)
                {
                    dto.ExamFormatCode = format.Code;
                    dto.ExamFormatName = format.Name;
                }
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();

            if (data.IsLockedForScheduleEdit)
            {
                TempData.SetNotification("warning", "Lịch thi đã được phân công giám thị nên không thể chỉnh sửa.");
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(data);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(ExamScheduleDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.SetNotification("error", "Vui lòng kiểm tra lại dữ liệu nhập vào.");
                return View(dto);
            }

            try
            {
                await _service.UpdateAsync(dto);
                TempData.SetNotification("success", "Cập nhật lịch thi thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
                return View(dto);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData.SetNotification("success", "Xóa lịch thi thành công.");
            }
            catch (Exception ex)
            {
                TempData.SetNotification("error", ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewAsync(data)) return Forbid();

            return View(data);
        }

        [HttpPost]
        [Authorize(Roles = "Thư ký khoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewInvigilatorSupportRequest([FromBody] ExamScheduleApprovalRequestDto request)
        {
            var result = await BuildSupportRequestAsync(request.ScheduleIds);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new
            {
                success = true,
                title = result.Title,
                academyYear = result.AcademyYearName,
                semester = result.SemesterName,
                scheduleCount = result.Schedules.Count,
                missingInvigilatorCount = result.Schedules.Sum(CountMissingInvigilators),
                rows = result.Schedules.Select((x, index) => new
                {
                    stt = index + 1,
                    subjectId = x.SubjectId,
                    subjectName = x.SubjectName,
                    credit = x.Credit,
                    groupNumber = x.GroupNumber,
                    className = x.ClassName,
                    examDate = x.ExamDate?.ToString("dd-MM-yyyy"),
                    sessionName = x.SessionName,
                    slotName = GetSlotNumber(x),
                    slotTime = FormatTime(x.SlotTimeStart),
                    roomName = x.RoomName,
                    buildingName = x.BuildingName ?? x.BuildingId,
                    capacity = x.RoomCapacity,
                    lecturer = x.UserName,
                    faculty = x.FacultyName,
                    lecturer1Code = x.Lecturer1Code,
                    lecturer1Name = x.Lecturer1Name,
                    lecturer1Faculty = x.Lecturer1FacultyName,
                    lecturer2Code = x.Lecturer2Code,
                    lecturer2Name = x.Lecturer2Name,
                    lecturer2Faculty = x.Lecturer2FacultyName,
                    missing = CountMissingInvigilators(x)
                })
            });
        }

        [HttpPost]
        [Authorize(Roles = "Thư ký khoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadInvigilatorSupportRequest([FromForm] SupportRequestFormDto request)
        {
            var result = await BuildSupportRequestAsync(ParseScheduleIds(request.ScheduleIds));
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            byte[] fileBytes;
            try
            {
                var importFile = request.TemplateFile == null
                    ? null
                    : new ImportFileDto
                    {
                        FileName = request.TemplateFile.FileName,
                        Length = request.TemplateFile.Length,
                        OpenReadStream = request.TemplateFile.OpenReadStream
                    };
                var templatePath = _templatePathService.GetTemplatePath("MAU DE NGHI HO TRO CBCT.xlsx");
                var templateBytes = await _documentService.GetSupportTemplateBytesAsync(importFile, templatePath);
                fileBytes = _documentService.BuildSupportRequestExcel(ToSupportRequestDocument(result), templateBytes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Không thể tạo file đề nghị hỗ trợ CBCT từ mẫu Excel.", errors = new[] { ex.Message } });
            }

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _documentService.BuildSupportRequestFileName(ToSupportRequestDocument(result)));
        }

        [HttpPost]
        [Authorize(Roles = "Thư ký khoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvigilatorSupportRequest([FromForm] SupportRequestFormDto request)
        {
            var result = await BuildSupportRequestAsync(ParseScheduleIds(request.ScheduleIds));
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            var recipient = _emailConfigurationService.GetSupportRequestRecipientEmail();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chưa cấu hình email nhận đề nghị hỗ trợ CBCT. Vui lòng thêm EmailSettings:SupportRequestRecipientEmail trong appsettings.json."
                });
            }

            var currentUser = result.CurrentUserId.HasValue
                ? await _userService.GetByIdAsync(result.CurrentUserId.Value)
                : null;
            var replyTo = currentUser?.Email;
            byte[] fileBytes;
            try
            {
                var importFile = request.TemplateFile == null
                    ? null
                    : new ImportFileDto
                    {
                        FileName = request.TemplateFile.FileName,
                        Length = request.TemplateFile.Length,
                        OpenReadStream = request.TemplateFile.OpenReadStream
                    };
                var templatePath = _templatePathService.GetTemplatePath("MAU DE NGHI HO TRO CBCT.xlsx");
                var templateBytes = await _documentService.GetSupportTemplateBytesAsync(importFile, templatePath);
                fileBytes = _documentService.BuildSupportRequestExcel(ToSupportRequestDocument(result), templateBytes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Không thể tạo file đề nghị hỗ trợ CBCT từ mẫu Excel.", errors = new[] { ex.Message } });
            }

            var subject = $"Đề nghị hỗ trợ CBCT - {result.FacultyName} - {result.SemesterName} năm học {result.AcademyYearName}";
            var supportRequestDocument = ToSupportRequestDocument(result);
            var body = _documentService.BuildSupportRequestEmailBody(supportRequestDocument, replyTo);

            try
            {
                await _emailService.SendEmailWithAttachmentAsync(
                    recipient.Trim(),
                    subject,
                    body,
                    _documentService.BuildSupportRequestFileName(supportRequestDocument),
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    replyTo);

                await _emailLogService.LogAsync(result.CurrentUserId!.Value, recipient.Trim(), "Sent", null, "InvigilatorSupportRequest");
                await _service.MarkSupportRequestedAsync(result.Schedules.Select(x => x.Id), result.CurrentUserId.Value);
            }
            catch (Exception ex)
            {
                await _emailLogService.LogAsync(result.CurrentUserId!.Value, recipient.Trim(), "Failed", ex.Message, "InvigilatorSupportRequest");
                return BadRequest(new { success = false, message = "Không thể gửi email đề nghị hỗ trợ CBCT.", errors = new[] { ex.Message } });
            }

            return Ok(new
            {
                success = true,
                message = $"Đã gửi email đề nghị hỗ trợ CBCT cho {recipient.Trim()} với {result.Schedules.Count} lịch thi.",
                scheduleCount = result.Schedules.Count,
                recipient = recipient.Trim()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Thư ký khoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestApproval([FromBody] ExamScheduleApprovalRequestDto request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var currentFacultyId = await GetCurrentFacultyIdAsync();

            if (!currentUserId.HasValue)
                return Unauthorized(new { success = false, message = "Không xác định được người dùng hiện tại." });

            if (!currentFacultyId.HasValue)
                return BadRequest(new { success = false, message = "Không xác định được khoa của tài khoản hiện tại." });

            var selectedIds = request.ScheduleIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (!selectedIds.Any())
                return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất một lịch thi để gửi duyệt." });

            var schedules = new List<ExamScheduleDto>();
            var errors = new List<string>();

            foreach (var scheduleId in selectedIds)
            {
                var schedule = await _service.GetByIdAsync(scheduleId);
                if (schedule == null)
                {
                    errors.Add($"Lịch thi #{scheduleId} không tồn tại.");
                    continue;
                }

                if (schedule.FacultyId != currentFacultyId.Value)
                {
                    errors.Add($"Lịch thi {BuildScheduleLabel(schedule)} không thuộc khoa của bạn.");
                    continue;
                }

                if (!string.Equals(ExamScheduleStatusHelper.Normalize(schedule.Status), ExamScheduleStatusHelper.Pending, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Lịch thi {BuildScheduleLabel(schedule)} đang ở trạng thái '{schedule.Status}', không thể gửi duyệt.");
                    continue;
                }

                if (schedule.ApprovalCount > 0)
                {
                    errors.Add($"Lịch thi {BuildScheduleLabel(schedule)} đã được gửi duyệt.");
                    continue;
                }

                schedules.Add(schedule);
            }

            if (errors.Any())
            {
                var message = errors.All(x => x.Contains("đã được gửi duyệt", StringComparison.OrdinalIgnoreCase))
                    ? "Lịch thi đã được gửi duyệt."
                    : "Chưa thể gửi yêu cầu duyệt. Vui lòng kiểm tra lại các lịch đã chọn.";

                return BadRequest(new
                {
                    success = false,
                    message,
                    errors
                });
            }

            var deans = (await _userService.GetPagedAsync(null, null, null, currentFacultyId.Value, true, 1, 1000))
                .Items
                .Where(x => string.Equals(x.RoleName, "Trưởng khoa", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!deans.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không tìm thấy tài khoản Trưởng khoa đang hoạt động trong khoa để nhận yêu cầu duyệt."
                });
            }

            var relatedId = schedules.Count == 1 ? schedules[0].Id : (int?)null;
            var title = $"Thư ký khoa gửi {schedules.Count} lịch thi chờ duyệt";
            var content = BuildApprovalRequestContent(schedules);

            foreach (var dean in deans)
            {
                await _notificationService.CreateAsync(new NotificationWriteDto
                {
                    UserId = dean.Id,
                    Title = title,
                    Content = content,
                    Type = NotificationTypes.ExamScheduleApproval,
                    RelatedId = relatedId,
                    CreatedBy = currentUserId.Value,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }, cancellationToken);
            }

            await _service.MarkApprovalRequestedAsync(
                schedules.Select(x => x.Id),
                deans.Select(x => x.Id),
                currentUserId.Value,
                currentFacultyId.Value,
                "Thư ký khoa gửi yêu cầu duyệt lịch thi.",
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = $"Đã gửi yêu cầu duyệt {schedules.Count} lịch thi đến Trưởng khoa.",
                sentCount = schedules.Count,
                recipientCount = deans.Count
            });
        }

        [HttpPost]
        [Authorize(Roles = "Thư ký khoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvigilatorConfirmation([FromBody] ExamScheduleApprovalRequestDto request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var currentFacultyId = await GetCurrentFacultyIdAsync();

            if (!currentUserId.HasValue)
                return Unauthorized(new { success = false, message = "Không xác định được người dùng hiện tại." });

            if (!currentFacultyId.HasValue)
                return BadRequest(new { success = false, message = "Không xác định được khoa của tài khoản hiện tại." });

            var confirmationPath = Url.Action(
                "Index",
                "InvigilatorResponse",
                new { area = "Lecturer", status = "Chưa phản hồi" }) ?? "/Lecturer/InvigilatorResponse?status=Ch%C6%B0a%20ph%E1%BA%A3n%20h%E1%BB%93i";
            var confirmationUrl = _requestContextService.BuildAbsoluteUrl(confirmationPath);

            var result = await _invigilatorResponseService.SendConfirmationAsync(new InvigilatorConfirmationRequestDto
            {
                ScheduleIds = request.ScheduleIds,
                SecretaryId = currentUserId.Value,
                FacultyId = currentFacultyId.Value
            }, confirmationUrl, cancellationToken);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new
            {
                success = true,
                message = result.Message,
                lecturerCount = result.LecturerCount,
                scheduleCount = result.ScheduleCount
            });
        }

        private async Task<bool> CanViewAsync(ExamScheduleDto dto)
        {
            if (User.IsInRole("Admin")) return true;

            var facultyId = await GetCurrentFacultyIdAsync();
            return facultyId.HasValue && dto.FacultyId == facultyId.Value;
        }

        private static string BuildScheduleLabel(ExamScheduleDto schedule)
        {
            var subject = string.IsNullOrWhiteSpace(schedule.SubjectId) ? $"#{schedule.Id}" : schedule.SubjectId;
            var classInfo = string.IsNullOrWhiteSpace(schedule.ClassName) ? string.Empty : $" - {schedule.ClassName}";
            var groupInfo = string.IsNullOrWhiteSpace(schedule.GroupNumber) ? string.Empty : $" - Nhóm {schedule.GroupNumber}";
            return $"{subject}{classInfo}{groupInfo}";
        }

        private static string BuildApprovalRequestContent(IReadOnlyList<ExamScheduleDto> schedules)
        {
            var sampleRows = schedules
                .Take(5)
                .Select(x => $"- {BuildScheduleLabel(x)} | {x.ExamDate?.ToString("dd/MM/yyyy") ?? "Chưa có ngày thi"} | {x.RoomName ?? "Chưa có phòng"}")
                .ToList();

            var extraCount = Math.Max(0, schedules.Count - sampleRows.Count);
            var extraText = extraCount > 0 ? $"\n... và {extraCount} lịch thi khác." : string.Empty;

            return "Thư ký khoa đã gửi yêu cầu duyệt lịch thi.\n" +
                   string.Join("\n", sampleRows) +
                   extraText +
                   "\nVui lòng vào màn Duyệt lịch thi để xem và xử lý.";
        }

        private async Task<(string Role, int? UserId, int? FacultyId)> BuildScopeAsync()
        {
            var role =
                User.IsInRole("Admin") ? "Admin" :
                User.IsInRole("Giảng viên") ? "Giảng viên" :
                User.IsInRole("Thư ký khoa") ? "Thư ký khoa" :
                User.IsInRole("Trưởng khoa") ? "Trưởng khoa" :
                "";

            var userId = GetCurrentUserId();

            int? facultyId = null;
            if (userId.HasValue)
            {
                var paged = await _userService.GetPagedAsync(null, null, null, null, null, 1, 1000);
                facultyId = paged.Items.FirstOrDefault(x => x.Id == userId.Value)?.FacultyId;
            }

            return (role, userId, facultyId);
        }

        private async Task ApplyDefaultAcademicContextAsync(ExamScheduleSearchDto filter, (string Role, int? UserId, int? FacultyId) scope)
        {
            if (filter.AcademyYearId.HasValue || filter.SemesterId.HasValue || filter.PeriodId.HasValue || filter.FromDate.HasValue || filter.ToDate.HasValue || !scope.UserId.HasValue)
                return;

            var context = await _currentAcademicContextService.GetCurrentContextAsync(scope.UserId.Value, scope.Role, filter.FacultyId ?? scope.FacultyId);
            if (context is null)
                return;

            filter.AcademyYearId = context.AcademyYearId;
            filter.SemesterId = context.SemesterId;
            filter.PeriodId = context.PeriodId;
        }

        private async Task SetInitialAcademicContextAsync((string Role, int? UserId, int? FacultyId) scope)
        {
            if (!scope.UserId.HasValue)
                return;

            var context = await _currentAcademicContextService.GetCurrentContextAsync(scope.UserId.Value, scope.Role, scope.FacultyId);
            ViewBag.InitialAcademyYearId = context?.AcademyYearId;
            ViewBag.InitialAcademyYearName = context?.AcademyYearName;
            ViewBag.InitialSemesterId = context?.SemesterId;
            ViewBag.InitialSemesterName = context?.SemesterName;
            ViewBag.InitialPeriodId = context?.PeriodId;
            ViewBag.InitialPeriodName = context?.PeriodName;
        }

        public class ExamScheduleApprovalRequestDto
        {
            public List<int> ScheduleIds { get; set; } = new();
        }

        private async Task<SupportRequestBuildResult> BuildSupportRequestAsync(List<int>? scheduleIds)
        {
            var currentUserId = GetCurrentUserId();
            var currentFacultyId = await GetCurrentFacultyIdAsync();

            if (!currentUserId.HasValue)
                return SupportRequestBuildResult.Fail("Không xác định được người dùng hiện tại.");

            if (!currentFacultyId.HasValue)
                return SupportRequestBuildResult.Fail("Không xác định được khoa của tài khoản hiện tại.");

            var selectedIds = scheduleIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (!selectedIds.Any())
                return SupportRequestBuildResult.Fail("Vui lòng chọn ít nhất một lịch thi cần hỗ trợ CBCT.");

            var schedules = new List<ExamScheduleDto>();
            var errors = new List<string>();

            foreach (var scheduleId in selectedIds)
            {
                var schedule = await _service.GetByIdAsync(scheduleId);
                if (schedule == null)
                {
                    errors.Add($"Lịch thi #{scheduleId} không tồn tại.");
                    continue;
                }

                if (schedule.FacultyId != currentFacultyId.Value)
                {
                    errors.Add($"Lịch thi {BuildScheduleLabel(schedule)} không thuộc khoa của bạn.");
                    continue;
                }

                var status = ExamScheduleStatusHelper.Normalize(schedule.Status);
                if (status != ExamScheduleStatusHelper.WaitingAssign && status != ExamScheduleStatusHelper.MissingInvigilator)
                {
                    errors.Add($"Lịch thi {BuildScheduleLabel(schedule)} đang ở trạng thái '{schedule.Status}', không thuộc diện đề nghị hỗ trợ CBCT.");
                    continue;
                }

                schedules.Add(schedule);
            }

            if (errors.Any())
                return SupportRequestBuildResult.Fail("Chưa thể lập file đề nghị hỗ trợ CBCT. Vui lòng kiểm tra lại các lịch đã chọn.", errors);

            var academyYearCount = schedules.Select(x => x.AcademyYearId).Distinct().Count();
            var semesterCount = schedules.Select(x => x.SemesterId).Distinct().Count();
            if (academyYearCount > 1 || semesterCount > 1)
                return SupportRequestBuildResult.Fail("Vui lòng chỉ chọn các lịch thi trong cùng một năm học và học kỳ để lập đúng mẫu đề nghị hỗ trợ CBCT.");

            schedules = schedules
                .OrderBy(x => x.ExamDate)
                .ThenBy(x => SessionOrder(x.SessionName))
                .ThenBy(x => x.SlotTimeStart ?? TimeOnly.MaxValue)
                .ThenBy(x => x.RoomName)
                .ToList();

            var academyYear = schedules.FirstOrDefault()?.AcademyYearName ?? string.Empty;
            var semester = ToSemesterDisplayName(schedules.FirstOrDefault()?.SemesterName);
            var facultyName = schedules.FirstOrDefault()?.FacultyName ?? "Khoa quản lý";

            return new SupportRequestBuildResult
            {
                Success = true,
                CurrentUserId = currentUserId,
                CurrentFacultyId = currentFacultyId,
                FacultyName = facultyName,
                AcademyYearName = academyYear,
                SemesterName = semester,
                Title = $"DANH SÁCH HỖ TRỢ CBCT - {semester.ToUpperInvariant()} NĂM HỌC {academyYear}",
                Schedules = schedules
            };
        }

        private static ExamScheduleSupportRequestDocumentDto ToSupportRequestDocument(SupportRequestBuildResult result) => new()
        {
            FacultyName = result.FacultyName,
            AcademyYearName = result.AcademyYearName,
            SemesterName = result.SemesterName,
            Title = result.Title,
            Schedules = result.Schedules
        };

        private static int CountMissingInvigilators(ExamScheduleDto schedule)
        {
            var assigned = 0;
            if (!string.IsNullOrWhiteSpace(schedule.Lecturer1Name)) assigned++;
            if (!string.IsNullOrWhiteSpace(schedule.Lecturer2Name)) assigned++;
            return Math.Max(0, 2 - assigned);
        }

        private static string FormatTime(TimeOnly? time)
        {
            return time.HasValue ? $"{time.Value.Hour}h{time.Value.Minute:00}" : string.Empty;
        }

        private static string GetSlotNumber(ExamScheduleDto schedule)
        {
            var raw = schedule.SlotName ?? string.Empty;
            if (raw.Contains("Ca 1", StringComparison.OrdinalIgnoreCase)) return "1";
            if (raw.Contains("Ca 2", StringComparison.OrdinalIgnoreCase)) return "2";
            return raw.Replace("Ca", string.Empty, StringComparison.OrdinalIgnoreCase).Split('(')[0].Trim();
        }

        private static int SessionOrder(string? sessionName)
        {
            return (sessionName ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "sáng" => 1,
                "chiều" => 2,
                "tối" => 3,
                _ => 99
            };
        }

        private static string ToSemesterDisplayName(string? semesterName)
        {
            var name = (semesterName ?? string.Empty).Trim();
            return name.ToLowerInvariant() switch
            {
                "1" or "hk1" or "học kỳ 1" => "HỌC KỲ 1",
                "2" or "hk2" or "học kỳ 2" => "HỌC KỲ 2",
                "hè" or "hk hè" or "summer" => "HỌC KỲ HÈ",
                _ => string.IsNullOrWhiteSpace(name) ? "HỌC KỲ" : name.ToUpperInvariant()
            };
        }

        private static List<int> ParseScheduleIds(string? raw)
        {
            return (raw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        public class SupportRequestFormDto
        {
            public string? ScheduleIds { get; set; }
            public IFormFile? TemplateFile { get; set; }
        }

        private class SupportRequestBuildResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<string> Errors { get; set; } = new();
            public int? CurrentUserId { get; set; }
            public int? CurrentFacultyId { get; set; }
            public string FacultyName { get; set; } = string.Empty;
            public string AcademyYearName { get; set; } = string.Empty;
            public string SemesterName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public List<ExamScheduleDto> Schedules { get; set; } = new();

            public static SupportRequestBuildResult Fail(string message, List<string>? errors = null)
            {
                return new SupportRequestBuildResult
                {
                    Success = false,
                    Message = message,
                    Errors = errors ?? new List<string>()
                };
            }
        }
    }
}
