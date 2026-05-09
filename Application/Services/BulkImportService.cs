using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using E = ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Application.Services
{
    public class BulkImportService : IBulkImportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _passwordService;

        private static readonly string[] ValidScheduleStatuses = ["Chờ phân công", "Thiếu giám thị", "Chờ duyệt", "Đã duyệt", "Từ chối duyệt"];
        private static readonly string[] ValidInvigilatorStatuses = ["Chờ xác nhận", "Xác nhận", "Từ chối"];

        public BulkImportService(ApplicationDbContext db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        public IReadOnlyList<string> SupportedModules { get; } =
        [
            "subject", "information-user", "course-offering", "exam-schedule", "lecturer-busy-slot", "exam-invigilator"
        ];

        public string GetModuleTitle(string module) => NormalizeModule(module) switch
        {
            "subject" => "Import môn học",
            "information-user" => "Import hồ sơ và tài khoản",
            "course-offering" => "Import học phần mở",
            "exam-schedule" => "Import lịch thi",
            "lecturer-busy-slot" => "Import lịch bận giảng viên",
            "exam-invigilator" => "Import phân công giám thị",
            _ => "Import dữ liệu"
        };

        public string GetBackUrl(string module) => NormalizeModule(module) switch
        {
            "subject" => "/Admin/Subject",
            "information-user" => "/Admin/User",
            "course-offering" => "/Admin/CourseOffering",
            "exam-schedule" => "/ExamSchedule",
            "lecturer-busy-slot" => "/BusySlot",
            "exam-invigilator" => "/ExamSchedule",
            _ => "/"
        };

        public List<ImportColumnDto> GetTemplateColumns(string module) => NormalizeModule(module) switch
        {
            "subject" =>
            [
                Col("SubjectId", "Mã môn", true, "Tối đa 10 ký tự, không trùng dữ liệu đã có.", "IT001"),
                Col("SubjectName", "Tên môn", true, "Tối đa 100 ký tự.", "Nhập môn lập trình"),
                Col("Credit", "Số tín chỉ", true, "Số nguyên 1-20.", "3"),
                Col("FacultyName", "Tên khoa", true, "Tên khoa phải khớp chính xác với dữ liệu hệ thống.", "Công nghệ thông tin")
            ],
            "information-user" =>
            [
                Col("UserName", "Tên đăng nhập", true, "Tối đa 8 ký tự, không trùng.", "gv001"),
                Col("Password", "Mật khẩu", true, "Mật khẩu ban đầu cho tài khoản.", "123456"),
                Col("RoleName", "Vai trò", true, "Tên vai trò phải khớp chính xác, ví dụ Admin/Giảng viên/Thư ký khoa.", "Giảng viên"),
                Col("FacultyName", "Tên khoa", false, "Bắt buộc với tài khoản thuộc khoa; để trống nếu không áp dụng.", "Công nghệ thông tin"),
                Col("LastName", "Họ và tên đệm", true, "Tối đa 50 ký tự.", "Nguyễn Văn"),
                Col("FirstName", "Tên", true, "Tối đa 50 ký tự.", "An"),
                Col("Email", "Email", true, "Không trùng hồ sơ đã có.", "an@example.com"),
                Col("Phone", "Số điện thoại", false, "Tối đa 10 ký tự.", "0900000000"),
                Col("Gender", "Giới tính", false, "Nam/Nữ/Male/Female hoặc để trống.", "Nam"),
                Col("Dob", "Ngày sinh", false, "Định dạng yyyy-MM-dd hoặc dd/MM/yyyy.", "1990-01-15"),
                Col("Address", "Địa chỉ", false, "Tối đa 255 ký tự.", "Hà Nội"),
                Col("PositionName", "Chức vụ", true, "Tên chức vụ phải khớp chính xác với dữ liệu hệ thống.", "Giảng viên"),
                Col("IsActive", "Hoạt động", false, "TRUE/FALSE, 1/0, Có/Không. Mặc định TRUE.", "TRUE")
            ],
            "course-offering" =>
            [
                Col("UserName", "Tên đăng nhập giảng viên", true, "Tài khoản giảng viên đã tồn tại.", "gv001"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("SubjectId", "Mã môn", true, "SubjectId đã tồn tại.", "IT001"),
                Col("ClassName", "Lớp học phần", true, "Tối đa 10 ký tự.", "D21CQCN01"),
                Col("GroupNumber", "Nhóm", true, "Tối đa 2 ký tự.", "01")
            ],
            "exam-schedule" =>
            [
                Col("SubjectId", "Mã môn", true, "Mã môn dạng string, ví dụ IT001.", "IT001"),
                Col("UserName", "Tên đăng nhập giảng viên", true, "Giảng viên phụ trách học phần mở.", "gv001"),
                Col("ClassName", "Lớp học phần", true, "Dùng để xác định học phần mở.", "D21CQCN01"),
                Col("GroupNumber", "Nhóm", true, "Dùng để xác định học phần mở.", "01"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("PeriodName", "Đợt thi", true, "Tên đợt thi trong học kỳ.", "Đợt 1"),
                Col("SessionName", "Buổi thi", true, "Tên buổi thi trong đợt thi.", "Sáng"),
                Col("SlotName", "Ca thi", true, "Tên ca thi trong buổi thi.", "Ca 1"),
                Col("BuildingId", "Mã giảng đường", true, "Mã giảng đường dạng string, ví dụ A1.", "A1"),
                Col("RoomName", "Tên phòng", true, "Tên phòng trong giảng đường đã chọn.", "101"),
                Col("ExamDate", "Ngày thi", true, "Định dạng yyyy-MM-dd hoặc dd/MM/yyyy.", "2026-06-01"),
                Col("Status", "Trạng thái", false, "Mặc định Chờ phân công.", "Chờ phân công")
            ],
            "lecturer-busy-slot" =>
            [
                Col("UserName", "Tên đăng nhập giảng viên", true, "Tài khoản giảng viên đã tồn tại.", "gv001"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("PeriodName", "Đợt thi", true, "Tên đợt thi trong học kỳ.", "Đợt 1"),
                Col("SessionName", "Buổi thi", true, "Tên buổi thi trong đợt thi.", "Sáng"),
                Col("SlotName", "Ca thi", true, "Tên ca thi trong buổi thi.", "Ca 1"),
                Col("BusyDate", "Ngày bận", true, "Định dạng yyyy-MM-dd hoặc dd/MM/yyyy.", "2026-06-01"),
                Col("Note", "Ghi chú", false, "Lý do bận.", "Đi công tác")
            ],
            "exam-invigilator" =>
            [
                Col("ExamScheduleId", "Mã lịch thi", true, "ExamScheduleId đã tồn tại.", "1"),
                Col("AssigneeUserName", "Tên đăng nhập giám thị", true, "Tài khoản giảng viên cùng khoa.", "gv001"),
                Col("PositionNo", "Vị trí", true, "Chỉ nhận 1 hoặc 2.", "1"),
                Col("Status", "Trạng thái", false, "Mặc định Chờ xác nhận.", "Chờ xác nhận")
            ],
            _ => []
        };

        public byte[] BuildTemplate(string module)
        {
            var columns = GetTemplateColumns(module);
            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Import" });

                sheetData.Append(BuildRow(1, columns.Select(x => x.Header)));
                sheetData.Append(BuildRow(2, columns.Select(x => x.Example)));
                sheetData.Append(BuildRow(3, columns.Select(x => x.Description)));
                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        public async Task<ImportResultDto> ImportAsync(string module, IFormFile file, int currentUserId, string currentRole, CancellationToken cancellationToken = default)
        {
            module = NormalizeModule(module);
            var result = new ImportResultDto { Module = module, ModuleTitle = GetModuleTitle(module) };

            if (!SupportedModules.Contains(module))
            {
                result.Errors.Add(Error(0, "Module", module, "Module import không hợp lệ."));
                return result;
            }

            if (file == null || file.Length == 0)
            {
                result.Errors.Add(Error(0, "File", string.Empty, "Vui lòng chọn file .xlsx để import."));
                return result;
            }

            var rows = ReadRows(file, result);
            result.TotalRows = rows.Count;
            if (result.Errors.Any() || rows.Count == 0)
            {
                if (rows.Count == 0) result.Errors.Add(Error(0, "File", file.FileName, "File không có dòng dữ liệu. Dòng 1 là header, dữ liệu bắt đầu từ dòng 2."));
                return result;
            }

            var entities = await ValidateAndMapAsync(module, rows, result, currentUserId, currentRole, cancellationToken);
            if (result.Errors.Any()) return result;

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            AddEntities(module, entities);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            result.InsertedRows = rows.Count;
            return result;
        }

        private async Task<List<object>> ValidateAndMapAsync(string module, List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, string currentRole, CancellationToken ct)
        {
            return module switch
            {
                "subject" => (await MapSubjects(rows, result, ct)).Cast<object>().ToList(),
                "information-user" => (await MapInformationUsers(rows, result, ct)).Cast<object>().ToList(),
                "course-offering" => (await MapCourseOfferings(rows, result, ct)).Cast<object>().ToList(),
                "exam-schedule" => (await MapExamSchedules(rows, result, ct)).Cast<object>().ToList(),
                "lecturer-busy-slot" => (await MapBusySlots(rows, result, currentUserId, currentRole, ct)).Cast<object>().ToList(),
                "exam-invigilator" => (await MapInvigilators(rows, result, currentUserId, ct)).Cast<object>().ToList(),
                _ => []
            };
        }

        private async Task<List<E.Subject>> MapSubjects(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var faculties = await _db.Faculties.Select(x => new { x.FacultyId, x.FacultyName }).ToListAsync(ct);
            var existing = (await _db.Subjects.Select(x => x.SubjectId).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<E.Subject>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var id = Val(row, "Mã môn").Trim();
                var name = Val(row, "Tên môn").Trim();
                if (Required(result, r, "Mã môn", id) && id.Length > 10) result.Errors.Add(Error(r, "Mã môn", id, "Tối đa 10 ký tự."));
                if (Required(result, r, "Tên môn", name) && name.Length > 100) result.Errors.Add(Error(r, "Tên môn", name, "Tối đa 100 ký tự."));
                if (!seen.Add(id)) result.Errors.Add(Error(r, "Mã môn", id, "Bị trùng trong file import."));
                if (existing.Contains(id)) result.Errors.Add(Error(r, "Mã môn", id, "Đã tồn tại trong hệ thống."));
                if (!TryByte(row, "Số tín chỉ", result, r, out var credit) || credit is < 1 or > 20) result.Errors.Add(Error(r, "Số tín chỉ", Val(row, "Số tín chỉ"), "Phải là số từ 1 đến 20."));
                var faculty = ResolveOne(faculties, x => x.FacultyName, Val(row, "Tên khoa"), result, r, "Tên khoa");
                var facultyId = faculty?.FacultyId ?? 0;
                list.Add(new E.Subject { SubjectId = id, SubjectName = name, Credit = credit, FacultyId = facultyId });
            }
            return list;
        }

        private async Task<List<E.User>> MapInformationUsers(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var roles = await _db.Roles.Select(x => new { x.RoleId, x.RoleName }).ToListAsync(ct);
            var positions = await _db.Positions.Select(x => new { x.PositionId, x.PositionName }).ToListAsync(ct);
            var faculties = await _db.Faculties.Select(x => new { x.FacultyId, x.FacultyName }).ToListAsync(ct);
            var existingUsers = (await _db.Users.Select(x => x.UserName).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingEmails = (await _db.Information.Select(x => x.Email).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<E.User>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var userName = Val(row, "Tên đăng nhập").Trim();
                var password = Val(row, "Mật khẩu");
                var lastName = Val(row, "Họ và tên đệm").Trim();
                var firstName = Val(row, "Tên").Trim();
                var email = Val(row, "Email").Trim();
                var phone = Val(row, "Số điện thoại").Trim();
                var gender = NormalizeGender(Val(row, "Giới tính").Trim());
                var address = Val(row, "Địa chỉ").Trim();
                if (Required(result, r, "Tên đăng nhập", userName) && userName.Length > 8) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Tối đa 8 ký tự."));
                if (!seenUsers.Add(userName)) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Bị trùng trong file import."));
                if (existingUsers.Contains(userName)) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Đã tồn tại."));
                Required(result, r, "Mật khẩu", password);
                if (Required(result, r, "Email", email) && !email.Contains('@')) result.Errors.Add(Error(r, "Email", email, "Email không hợp lệ."));
                if (!seenEmails.Add(email)) result.Errors.Add(Error(r, "Email", email, "Bị trùng trong file import."));
                if (existingEmails.Contains(email)) result.Errors.Add(Error(r, "Email", email, "Đã tồn tại trong hồ sơ."));
                if (Required(result, r, "Họ và tên đệm", lastName) && lastName.Length > 50) result.Errors.Add(Error(r, "Họ và tên đệm", lastName, "Tối đa 50 ký tự."));
                if (Required(result, r, "Tên", firstName) && firstName.Length > 50) result.Errors.Add(Error(r, "Tên", firstName, "Tối đa 50 ký tự."));
                if (phone.Length > 10) result.Errors.Add(Error(r, "Số điện thoại", phone, "Tối đa 10 ký tự."));
                if (address.Length > 255) result.Errors.Add(Error(r, "Địa chỉ", address, "Tối đa 255 ký tự."));
                var role = ResolveOne(roles, x => x.RoleName, Val(row, "Vai trò"), result, r, "Vai trò");
                var roleId = role?.RoleId ?? 0;
                var position = ResolveOne(positions, x => x.PositionName, Val(row, "Chức vụ"), result, r, "Chức vụ");
                var positionId = position?.PositionId ?? 0;
                int? facultyId = null;
                if (!string.IsNullOrWhiteSpace(Val(row, "Tên khoa")))
                {
                    var faculty = ResolveOne(faculties, x => x.FacultyName, Val(row, "Tên khoa"), result, r, "Tên khoa");
                    facultyId = faculty?.FacultyId;
                }
                DateTime? dob = null;
                if (!string.IsNullOrWhiteSpace(Val(row, "Ngày sinh")) && !TryDateTime(row, "Ngày sinh", result, r, out dob)) { }
                var isActive = TryBool(Val(row, "Hoạt động"), true);
                list.Add(new E.User
                {
                    UserName = userName,
                    PasswordHash = _passwordService.HashPassword(password),
                    RoleId = roleId,
                    FacultyId = facultyId,
                    IsActive = isActive,
                    FailedLoginAttempts = 0,
                    Information = new E.Information { LastName = lastName, FirstName = firstName, Email = email, Phone = EmptyToNull(phone), Gender = EmptyToNull(gender), Dob = dob, Address = EmptyToNull(address), PositionId = positionId }
                });
            }
            return list;
        }

        private async Task<List<E.CourseOffering>> MapCourseOfferings(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var users = (await _db.Users.Include(x => x.Role).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId }).ToListAsync(ct);
            var subjects = (await _db.Subjects.Select(x => x.SubjectId).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var list = new List<E.CourseOffering>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var userName = Val(row, "Tên đăng nhập giảng viên").Trim();
                var subjectId = Val(row, "Mã môn").Trim();
                var className = Val(row, "Lớp học phần").Trim();
                var group = Val(row, "Nhóm").Trim();
                if (!users.TryGetValue(userName, out var user) || user.Role.RoleName != "Giảng viên") result.Errors.Add(Error(r, "Tên đăng nhập giảng viên", userName, "Không tồn tại hoặc không phải giảng viên."));
                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var semester = ResolveOne(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var semesterId = semester?.SemesterId ?? 0;
                if (Required(result, r, "Mã môn", subjectId) && !subjects.Contains(subjectId)) result.Errors.Add(Error(r, "Mã môn", subjectId, "Không tồn tại."));
                if (Required(result, r, "Lớp học phần", className) && className.Length > 10) result.Errors.Add(Error(r, "Lớp học phần", className, "Tối đa 10 ký tự."));
                if (Required(result, r, "Nhóm", group) && group.Length > 2) result.Errors.Add(Error(r, "Nhóm", group, "Tối đa 2 ký tự."));
                var key = $"{userName}|{semesterId}|{subjectId}|{className}|{group}";
                if (!seen.Add(key)) result.Errors.Add(Error(r, "Dòng", key, "Bị trùng học phần mở trong file."));
                list.Add(new E.CourseOffering { UserId = user?.UserId ?? 0, SemesterId = semesterId, SubjectId = subjectId, ClassName = className, GroupNumber = group });
            }
            return list;
        }

        private async Task<List<E.ExamSchedule>> MapExamSchedules(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId }).ToListAsync(ct);
            var periods = await _db.ExamPeriods.Select(x => new { x.PeriodId, x.PeriodName, x.SemesterId }).ToListAsync(ct);
            var sessions = await _db.ExamSessions.Select(x => new { x.SessionId, x.SessionName, x.PeriodId }).ToListAsync(ct);
            var slots = await _db.ExamSlots.Select(x => new { x.SlotId, x.SlotName, x.SessionId }).ToListAsync(ct);
            var rooms = await _db.Rooms.Select(x => new { x.RoomId, x.RoomName, x.BuildingId }).ToListAsync(ct);
            var offerings = await _db.CourseOfferings.Include(x => x.User).Select(x => new { x.OfferingId, x.User.UserName, x.SemesterId, x.SubjectId, x.ClassName, x.GroupNumber }).ToListAsync(ct);
            var list = new List<E.ExamSchedule>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var yearId = year?.AcademyYearId ?? 0;
                var semester = ResolveOne(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var semesterId = semester?.SemesterId ?? 0;
                var period = ResolveOne(periods.Where(x => semester == null || x.SemesterId == semester.SemesterId), x => x.PeriodName, Val(row, "Đợt thi"), result, r, "Đợt thi");
                var periodId = period?.PeriodId ?? 0;
                var session = ResolveOne(sessions.Where(x => period == null || x.PeriodId == period.PeriodId), x => x.SessionName, Val(row, "Buổi thi"), result, r, "Buổi thi");
                var sessionId = session?.SessionId ?? 0;
                var slot = ResolveOne(slots.Where(x => session == null || x.SessionId == session.SessionId), x => x.SlotName, Val(row, "Ca thi"), result, r, "Ca thi");
                var slotId = slot?.SlotId ?? 0;
                var buildingId = Val(row, "Mã giảng đường").Trim();
                Required(result, r, "Mã giảng đường", buildingId);
                var room = ResolveOne(rooms.Where(x => string.Equals(x.BuildingId, buildingId, StringComparison.OrdinalIgnoreCase)), x => x.RoomName, Val(row, "Tên phòng"), result, r, "Tên phòng");
                var roomId = room?.RoomId ?? 0;
                var offering = offerings.FirstOrDefault(x => x.SemesterId == semesterId && string.Equals(x.SubjectId, Val(row, "Mã môn"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.UserName, Val(row, "Tên đăng nhập giảng viên"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.ClassName, Val(row, "Lớp học phần"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.GroupNumber, Val(row, "Nhóm"), StringComparison.OrdinalIgnoreCase));
                if (offering == null) result.Errors.Add(Error(r, "Học phần mở", "", "Không tìm thấy học phần mở theo Mã môn + Giảng viên + Lớp + Nhóm + Học kỳ."));
                var offeringId = offering?.OfferingId ?? 0;
                if (!TryDateTime(row, "Ngày thi", result, r, out var examDate)) examDate = default;
                var status = Val(row, "Trạng thái").Trim();
                if (string.IsNullOrWhiteSpace(status)) status = "Chờ phân công";
                if (!ValidScheduleStatuses.Contains(status)) result.Errors.Add(Error(r, "Trạng thái", status, "Không hợp lệ."));
                list.Add(new E.ExamSchedule { OfferingId = offeringId, AcademyYearId = yearId, SemesterId = semesterId, PeriodId = periodId, SessionId = sessionId, SlotId = slotId, RoomId = roomId, ExamDate = examDate!.Value, Status = status });
            }
            return list;
        }

        private async Task<List<E.LecturerBusySlot>> MapBusySlots(List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, string currentRole, CancellationToken ct)
        {
            var users = (await _db.Users.Include(x => x.Role).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var currentFacultyId = await _db.Users.Where(x => x.UserId == currentUserId).Select(x => x.FacultyId).FirstOrDefaultAsync(ct);
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId }).ToListAsync(ct);
            var periods = await _db.ExamPeriods.Select(x => new { x.PeriodId, x.PeriodName, x.SemesterId }).ToListAsync(ct);
            var sessions = await _db.ExamSessions.Select(x => new { x.SessionId, x.SessionName, x.PeriodId }).ToListAsync(ct);
            var slots = await _db.ExamSlots.Select(x => new { x.SlotId, x.SlotName, x.SessionId }).ToListAsync(ct);
            var existing = await _db.LecturerBusySlots.Select(x => new { x.UserId, x.SlotId, x.BusyDate }).ToListAsync(ct);
            var existingSet = existing.Select(x => (x.UserId, x.SlotId, x.BusyDate)).ToHashSet();
            var seen = new HashSet<(int UserId, int SlotId, DateOnly Date)>();
            var list = new List<E.LecturerBusySlot>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var userName = Val(row, "Tên đăng nhập giảng viên").Trim();
                if (!users.TryGetValue(userName, out var user) || user.Role.RoleName != "Giảng viên") result.Errors.Add(Error(r, "Tên đăng nhập giảng viên", userName, "Không tồn tại hoặc không phải giảng viên."));
                if (currentRole == "Thư ký khoa" && user != null && user.FacultyId != currentFacultyId) result.Errors.Add(Error(r, "Tên đăng nhập giảng viên", userName, "Không thuộc khoa của thư ký hiện tại."));
                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var semester = ResolveOne(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var period = ResolveOne(periods.Where(x => semester == null || x.SemesterId == semester.SemesterId), x => x.PeriodName, Val(row, "Đợt thi"), result, r, "Đợt thi");
                var session = ResolveOne(sessions.Where(x => period == null || x.PeriodId == period.PeriodId), x => x.SessionName, Val(row, "Buổi thi"), result, r, "Buổi thi");
                var slot = ResolveOne(slots.Where(x => session == null || x.SessionId == session.SessionId), x => x.SlotName, Val(row, "Ca thi"), result, r, "Ca thi");
                var slotId = slot?.SlotId ?? 0;
                if (!TryDateOnly(row, "Ngày bận", result, r, out var busyDate)) busyDate = default;
                var key = (user?.UserId ?? 0, slotId, busyDate);
                if (!seen.Add(key) || existingSet.Contains(key)) result.Errors.Add(Error(r, "Dòng", $"{userName}-{slotId}-{busyDate:yyyy-MM-dd}", "Lịch bận bị trùng."));
                list.Add(new E.LecturerBusySlot { UserId = user?.UserId ?? 0, SlotId = slotId, BusyDate = busyDate, Note = EmptyToNull(Val(row, "Ghi chú")), CreateAt = DateTime.Now });
            }
            return list;
        }

        private async Task<List<E.ExamInvigilator>> MapInvigilators(List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, CancellationToken ct)
        {
            var currentFacultyId = await _db.Users.Where(x => x.UserId == currentUserId).Select(x => x.FacultyId).FirstOrDefaultAsync(ct);
            var users = (await _db.Users.Include(x => x.Role).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var schedules = await _db.ExamSchedules.Include(x => x.Offering).ToDictionaryAsync(x => x.ExamScheduleId, ct);
            var existing = await _db.ExamInvigilators.Select(x => new { x.ExamScheduleId, x.PositionNo, x.AssigneeId }).ToListAsync(ct);
            var occupiedPositions = existing.Select(x => (x.ExamScheduleId, x.PositionNo)).ToHashSet();
            var assignedUsers = existing.Select(x => (x.ExamScheduleId, x.AssigneeId)).ToHashSet();
            var seenPos = new HashSet<(int ScheduleId, byte Position)>();
            var list = new List<E.ExamInvigilator>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                if (!TryInt(row, "Mã lịch thi", result, r, out var scheduleId) || !schedules.TryGetValue(scheduleId, out var schedule)) result.Errors.Add(Error(r, "Mã lịch thi", Val(row, "Mã lịch thi"), "Không tồn tại."));
                else if (schedule.Offering.UserId == currentUserId) { }
                var userName = Val(row, "Tên đăng nhập giám thị").Trim();
                if (!users.TryGetValue(userName, out var user) || user.Role.RoleName != "Giảng viên") result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Không tồn tại hoặc không phải giảng viên."));
                if (user != null && user.FacultyId != currentFacultyId) result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Không thuộc khoa của thư ký hiện tại."));
                if (!TryByte(row, "Vị trí", result, r, out var pos) || pos is < 1 or > 2) result.Errors.Add(Error(r, "Vị trí", Val(row, "Vị trí"), "Chỉ nhận 1 hoặc 2."));
                if (!seenPos.Add((scheduleId, pos)) || occupiedPositions.Contains((scheduleId, pos))) result.Errors.Add(Error(r, "Vị trí", pos.ToString(), "Vị trí giám thị của lịch thi đã có người hoặc bị trùng trong file."));
                if (user != null && assignedUsers.Contains((scheduleId, user.UserId))) result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Giảng viên đã được phân công ở lịch này."));
                var status = Val(row, "Trạng thái").Trim();
                if (string.IsNullOrWhiteSpace(status)) status = "Chờ xác nhận";
                if (!ValidInvigilatorStatuses.Contains(status)) result.Errors.Add(Error(r, "Trạng thái", status, "Không hợp lệ."));
                list.Add(new E.ExamInvigilator { ExamScheduleId = scheduleId, AssigneeId = user?.UserId ?? 0, AssignerId = currentUserId, PositionNo = pos, Status = status, CreateAt = DateTime.Now });
            }
            return list;
        }

        private void AddEntities(string module, List<object> entities)
        {
            switch (module)
            {
                case "subject": _db.Subjects.AddRange(entities.Cast<E.Subject>()); break;
                case "information-user": _db.Users.AddRange(entities.Cast<E.User>()); break;
                case "course-offering": _db.CourseOfferings.AddRange(entities.Cast<E.CourseOffering>()); break;
                case "exam-schedule": _db.ExamSchedules.AddRange(entities.Cast<E.ExamSchedule>()); break;
                case "lecturer-busy-slot": _db.LecturerBusySlots.AddRange(entities.Cast<E.LecturerBusySlot>()); break;
                case "exam-invigilator": _db.ExamInvigilators.AddRange(entities.Cast<E.ExamInvigilator>()); break;
            }
        }

        private List<Dictionary<string, string>> ReadRows(IFormFile file, ImportResultDto result)
        {
            var rows = new List<Dictionary<string, string>>();
            try
            {
                using var stream = file.OpenReadStream();
                using var document = SpreadsheetDocument.Open(stream, false);
                var workbookPart = document.WorkbookPart!;
                var sheet = workbookPart.Workbook.Sheets!.Elements<Sheet>().First();
                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
                var excelRows = sheetData.Elements<Row>().ToList();
                if (excelRows.Count < 2) return rows;
                var headers = excelRows[0].Elements<Cell>().Select(c => GetCellValue(workbookPart, c).Trim()).ToList();
                foreach (var row in excelRows.Where(x => (x.RowIndex?.Value ?? 0) >= 4))
                {
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["__RowNumber"] = row.RowIndex?.Value.ToString() ?? "0" };
                    var cells = row.Elements<Cell>().ToList();
                    for (var i = 0; i < headers.Count; i++)
                    {
                        dict[headers[i]] = GetCellValue(workbookPart, cells.FirstOrDefault(c => GetColumnName(c.CellReference?.Value) == GetExcelColumnName(i + 1)));
                    }
                    if (dict.Where(x => x.Key != "__RowNumber").Any(x => !string.IsNullOrWhiteSpace(x.Value))) rows.Add(dict);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(Error(0, "File", file.FileName, "Không đọc được file .xlsx: " + ex.Message));
            }
            return rows;
        }

        private static Row BuildRow(uint index, IEnumerable<string> values)
        {
            var row = new Row { RowIndex = index };
            foreach (var value in values) row.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(value ?? string.Empty) });
            return row;
        }

        private static string GetCellValue(WorkbookPart workbookPart, Cell? cell)
        {
            if (cell?.CellValue == null) return string.Empty;
            var value = cell.CellValue.InnerText;
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                return workbookPart.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(value)).InnerText ?? string.Empty;
            }
            return value;
        }

        private static string? GetColumnName(string? reference) => string.IsNullOrWhiteSpace(reference) ? null : new string(reference.TakeWhile(char.IsLetter).ToArray());
        private static string GetExcelColumnName(int number) { var name = string.Empty; while (number > 0) { var mod = (number - 1) % 26; name = (char)('A' + mod) + name; number = (number - mod) / 26; } return name; }
        private static ImportColumnDto Col(string key, string header, bool required, string description, string example) => new() { Key = key, Header = header, Required = required, Description = description, Example = example };
        private static ImportErrorDto Error(int row, string column, string value, string message) => new() { RowNumber = row, Column = column, Value = value, Message = message };
        private static string NormalizeModule(string module) => (module ?? string.Empty).Trim().ToLowerInvariant();
        private static int RowNo(Dictionary<string, string> row) => int.TryParse(row.GetValueOrDefault("__RowNumber"), out var n) ? n : 0;
        private static string Val(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty;
        private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static bool Required(ImportResultDto result, int row, string column, string value) { if (!string.IsNullOrWhiteSpace(value)) return true; result.Errors.Add(Error(row, column, value, "Bắt buộc nhập.")); return false; }
        private static bool TryInt(Dictionary<string, string> row, string column, ImportResultDto result, int rowNo, out int value) { var raw = Val(row, column); if (int.TryParse(raw, out value)) return true; result.Errors.Add(Error(rowNo, column, raw, "Phải là số nguyên.")); return false; }
        private static bool TryByte(Dictionary<string, string> row, string column, ImportResultDto result, int rowNo, out byte value) { var raw = Val(row, column); if (byte.TryParse(raw, out value)) return true; result.Errors.Add(Error(rowNo, column, raw, "Phải là số nguyên nhỏ.")); return false; }
        private static bool TryDateOnly(Dictionary<string, string> row, string column, ImportResultDto result, int rowNo, out DateOnly value) { if (TryDateTime(row, column, result, rowNo, out var dt)) { value = DateOnly.FromDateTime(dt!.Value); return true; } value = default; return false; }
        private static bool TryDateTime(Dictionary<string, string> row, string column, ImportResultDto result, int rowNo, out DateTime? value)
        {
            var raw = Val(row, column);
            if (DateTime.TryParseExact(raw, ["yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) || DateTime.TryParse(raw, out dt)) { value = dt.Date; return true; }
            value = null; result.Errors.Add(Error(rowNo, column, raw, "Ngày không hợp lệ. Dùng yyyy-MM-dd hoặc dd/MM/yyyy.")); return false;
        }
        private static bool TryBool(string raw, bool defaultValue) => string.IsNullOrWhiteSpace(raw) ? defaultValue : raw.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "y" or "có" or "co" or "active" or "hoạt động";
        private static string NormalizeGender(string gender) => gender.ToLowerInvariant() switch { "male" or "nam" => "Male", "female" or "nữ" or "nu" => "Female", _ => gender };
        private static T? ResolveOne<T>(IEnumerable<T> source, Func<T, string?> selector, string raw, ImportResultDto result, int row, string column) where T : class
        {
            if (!Required(result, row, column, raw)) return null;
            var normalized = NormalizeLookup(raw);
            var matches = source.Where(x => NormalizeLookup(selector(x)) == normalized).ToList();
            if (matches.Count == 1) return matches[0];
            result.Errors.Add(Error(row, column, raw, matches.Count == 0 ? "Không tìm thấy dữ liệu khớp trong hệ thống." : "Tên bị trùng trong hệ thống hoặc trong phạm vi cha, cần kiểm tra lại dữ liệu."));
            return null;
        }
        private static string NormalizeLookup(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
