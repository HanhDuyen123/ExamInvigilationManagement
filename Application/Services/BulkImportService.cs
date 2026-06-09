using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Common.Constants;
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
        private static readonly string[] ValidInvigilatorStatuses = ["Chưa gửi xác nhận", "Chờ xác nhận", "Xác nhận", "Từ chối"];

        public BulkImportService(ApplicationDbContext db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        public IReadOnlyList<string> SupportedModules { get; } =
        [
            "subject", "information-user", "course-offering", "exam-schedule", "lecturer-busy-slot", "lecturer-period-availability", "exam-invigilator"
        ];

        public string GetModuleTitle(string module) => NormalizeModule(module) switch
        {
            "subject" => "Import môn học",
            "information-user" => "Import hồ sơ và tài khoản",
            "course-offering" => "Import học phần mở",
            "exam-schedule" => "Import lịch thi",
            "lecturer-busy-slot" => "Import lịch bận giảng viên",
            "lecturer-period-availability" => "Import giảng viên khả thi theo đợt",
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
            "lecturer-period-availability" => "/LecturerManagement/Availability",
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
                Col("ExamFormat", "Hình thức thi", true, "Code hoặc tên hình thức thi, ví dụ PM/TN-TL/TL.", "PM"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("PeriodName", "Đợt thi", true, "Tên đợt thi trong học kỳ.", "Đợt 1"),
                Col("SessionName", "Buổi thi", true, "Tên buổi thi trong đợt thi; có thể nhập Cả ngày để lấy tất cả buổi/ca trong ngày.", "Sáng"),
                Col("SlotName", "Ca thi", false, "Tên ca thi trong buổi thi; nhập Nguyên buổi để lấy tất cả ca của buổi. Nếu Buổi thi là Cả ngày thì có thể để trống hoặc nhập Nguyên buổi.", "Ca 1"),
                Col("BuildingId", "Mã giảng đường", false, "Mã giảng đường nếu có, ví dụ A1. Có thể để trống với phòng thi độc lập.", "A1"),
                Col("RoomName", "Tên phòng", true, "Tên phòng thi. Có thể nhập 101, A1.101, P.DIEN AN hoặc DIEN AN.", "101"),
                Col("ExamDate", "Ngày thi", true, "Định dạng yyyy-MM-dd hoặc dd/MM/yyyy.", "2026-06-01"),
                Col("Status", "Trạng thái", false, "Mặc định Chờ phân công.", "Chờ phân công")
            ],
            "lecturer-busy-slot" =>
            [
                Col("UserName", "Tên đăng nhập giảng viên", true, "Tài khoản hợp lệ đã tồn tại; không bắt buộc role Giảng viên.", "gv001"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("PeriodName", "Đợt thi", true, "Tên đợt thi trong học kỳ.", "Đợt 1"),
                Col("SessionName", "Buổi thi", true, "Tên buổi thi trong đợt thi.", "Sáng"),
                Col("SlotName", "Ca thi", true, "Tên ca thi trong buổi thi.", "Ca 1"),
                Col("BusyDate", "Ngày bận", true, "Định dạng yyyy-MM-dd hoặc dd/MM/yyyy.", "2026-06-01"),
                Col("Note", "Ghi chú", false, "Lý do bận.", "Đi công tác")
            ],
            "lecturer-period-availability" =>
            [
                Col("UserName", "Tên đăng nhập giảng viên", true, "Tài khoản hợp lệ có khả năng tham gia coi thi trong đợt; không bắt buộc role Giảng viên.", "gv001"),
                Col("LastName", "Họ", true, "Họ phải khớp với tên đăng nhập trong hệ thống.", "Nguyễn Văn"),
                Col("FirstName", "Tên", true, "Tên phải khớp với tên đăng nhập trong hệ thống.", "An"),
                Col("AcademyYearName", "Năm học", true, "Tên năm học phải khớp chính xác.", "2025-2026"),
                Col("SemesterName", "Học kỳ", true, "Tên học kỳ trong năm học đã chọn.", "Học kỳ 1"),
                Col("PeriodName", "Đợt thi", true, "Tên đợt thi trong học kỳ.", "Đợt 1"),
                Col("Note", "Ghi chú", false, "Thông tin bổ sung nếu có.", "Có thể coi thi trực tiếp")
            ],
            "exam-invigilator" =>
            [
                Col("ExamScheduleId", "Mã lịch thi", true, "ExamScheduleId đã tồn tại.", "1"),
                Col("AssigneeUserName", "Tên đăng nhập giám thị", true, "Tài khoản hợp lệ có thể tham gia coi thi; không bắt buộc role Giảng viên.", "gv001"),
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
                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildImportStylesheet();
                stylesPart.Stylesheet.Save();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                var columnCount = Math.Max(1, columns.Count);
                worksheetPart.Worksheet = new Worksheet(BuildImportColumns(columns), sheetData);
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Import" });

                string[] SpreadHeader(string left, string right)
                {
                    var values = Enumerable.Repeat(string.Empty, columnCount).ToArray();
                    values[0] = left;
                    values[^1] = right;
                    return values;
                }

                sheetData.Append(BuildRow(1, SpreadHeader(DocumentLetterhead.Ministry, DocumentLetterhead.Nation), 3));
                sheetData.Append(BuildRow(2, SpreadHeader(DocumentLetterhead.School, DocumentLetterhead.Motto), 3));
                sheetData.Append(BuildRow(4, [GetModuleTitle(module).ToUpperInvariant()], 2));
                sheetData.Append(BuildRow(5, [$"Nhập dữ liệu từ dòng 7. Các cột bắt buộc: {string.Join(", ", columns.Where(x => x.Required).Select(x => x.Header))}"], 4));
                sheetData.Append(BuildRow(6, columns.Select(x => x.Header), 1));

                worksheetPart.Worksheet.Append(new MergeCells(
                    new MergeCell { Reference = new StringValue($"A4:{GetExcelColumnName(columnCount)}4") },
                    new MergeCell { Reference = new StringValue($"A5:{GetExcelColumnName(columnCount)}5") }));
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

            var rows = ReadRows(module, file, result);
            result.TotalRows = rows.Count;
            if (result.Errors.Any() || rows.Count == 0)
            {
                if (rows.Count == 0) result.Errors.Add(Error(0, "File", file.FileName, "File không có dòng dữ liệu. Không tìm thấy header hoặc dòng dữ liệu hợp lệ trong file."));
                return result;
            }

            var entities = await ValidateAndMapAsync(module, rows, result, currentUserId, currentRole, cancellationToken);
            if (result.Errors.Any()) return result;

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            result.InsertedRows = await AddEntitiesAsync(module, entities, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return result;
        }

        private async Task<List<object>> ValidateAndMapAsync(string module, List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, string currentRole, CancellationToken ct)
        {
            return module switch
            {
                "subject" => (await MapSubjects(rows, result, ct)).Cast<object>().ToList(),
                "information-user" => (await MapInformationUsers(rows, result, ct)).Cast<object>().ToList(),
                "course-offering" => (await MapCourseOfferingsWithSubjects(rows, result, ct)).Cast<object>().ToList(),
                "exam-schedule" => (await MapExamSchedules(rows, result, ct)).Cast<object>().ToList(),
                "lecturer-busy-slot" => (await MapBusySlots(rows, result, currentUserId, currentRole, ct)).Cast<object>().ToList(),
                "lecturer-period-availability" => (await MapPeriodAvailabilities(rows, result, currentUserId, currentRole, ct)).Cast<object>().ToList(),
                "exam-invigilator" => (await MapInvigilators(rows, result, currentUserId, ct)).Cast<object>().ToList(),
                _ => []
            };
        }

        private async Task<List<E.Subject>> MapSubjects(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var faculties = await _db.Faculties.Select(x => new { x.FacultyId, x.FacultyName }).ToListAsync(ct);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<E.Subject>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var id = Val(row, "Mã môn").Trim();
                var name = Val(row, "Tên môn").Trim();
                if (Required(result, r, "Mã môn", id) && id.Length > 10) result.Errors.Add(Error(r, "Mã môn", id, "Tối đa 10 ký tự."));
                if (Required(result, r, "Tên môn", name) && name.Length > 255) result.Errors.Add(Error(r, "Tên môn", name, "Tối đa 255 ký tự."));
                if (!seen.Add(id)) continue;
                if (!TryByte(row, "Số tín chỉ", result, r, out var credit) || credit is < 1 or > 20) result.Errors.Add(Error(r, "Số tín chỉ", Val(row, "Số tín chỉ"), "Phải là số từ 1 đến 20."));
                var faculty = ResolveFaculty(faculties, x => x.FacultyName, Val(row, "Tên khoa"), result, r, "Tên khoa");
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
                    var faculty = ResolveFaculty(faculties, x => x.FacultyName, Val(row, "Tên khoa"), result, r, "Tên khoa");
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
            var users = (await _db.Users.Include(x => x.Role).Include(x => x.Information).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var today = DateTime.Today;
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId, x.EndDate }).ToListAsync(ct);
            var subjects = (await _db.Subjects.Select(x => x.SubjectId).ToListAsync(ct))
                .Concat(_db.Subjects.Local.Select(x => x.SubjectId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
                var semester = ResolveSemester(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var semesterId = semester?.SemesterId ?? 0;
                if (semester?.EndDate.HasValue == true && semester.EndDate.Value.Date < today)
                    result.Errors.Add(Error(r, "Học kỳ", Val(row, "Học kỳ"), "Không thể import dữ liệu vào học kỳ đã kết thúc."));
                if (Required(result, r, "Mã môn", subjectId) && !subjects.Contains(subjectId)) result.Errors.Add(Error(r, "Mã môn", subjectId, "Không tồn tại."));
                if (Required(result, r, "Lớp học phần", className) && className.Length > 30) result.Errors.Add(Error(r, "Lớp học phần", className, "Tối đa 30 ký tự."));
                if (Required(result, r, "Nhóm", group) && group.Length > 2) result.Errors.Add(Error(r, "Nhóm", group, "Tối đa 2 ký tự."));
                var key = $"{userName}|{semesterId}|{subjectId}|{className}|{group}";
                if (!seen.Add(key)) result.Errors.Add(Error(r, "Dòng", key, "Bị trùng học phần mở trong file."));
                list.Add(new E.CourseOffering { UserId = user?.UserId ?? 0, SemesterId = semesterId, SubjectId = subjectId, ClassName = className, GroupNumber = group });
            }
            return list;
        }

        private async Task<List<E.CourseOffering>> MapCourseOfferingsWithSubjects(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            if (rows.Any(x => Val(x, "__SchoolSchedule") == "1"))
                await StageSubjectsFromSchoolScheduleRowsAsync(rows, result, ct);

            return await MapCourseOfferings(rows, result, ct);
        }

        private async Task StageSubjectsFromSchoolScheduleRowsAsync(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            var faculties = await _db.Faculties.Select(x => new { x.FacultyId, x.FacultyName }).ToListAsync(ct);
            var existing = (await _db.Subjects.Select(x => x.SubjectId).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var staged = _db.Subjects.Local.Select(x => x.SubjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var r = RowNo(row);
                var subjectId = Val(row, "Mã môn").Trim();
                if (string.IsNullOrWhiteSpace(subjectId) || existing.Contains(subjectId) || staged.Contains(subjectId) || !seen.Add(subjectId))
                    continue;

                var subjectName = Val(row, "Tên môn").Trim();
                if (Required(result, r, "Tên môn", subjectName) && subjectName.Length > 255)
                    result.Errors.Add(Error(r, "Tên môn", subjectName, "Tối đa 255 ký tự."));

                if (!TryByte(row, "Số tín chỉ", result, r, out var credit) || credit is < 1 or > 20)
                    result.Errors.Add(Error(r, "Số tín chỉ", Val(row, "Số tín chỉ"), "Phải là số từ 1 đến 20."));

                var faculty = ResolveFaculty(faculties, x => x.FacultyName, Val(row, "Tên khoa"), result, r, "Tên khoa");
                var facultyId = faculty?.FacultyId ?? 0;

                if (facultyId > 0 && !string.IsNullOrWhiteSpace(subjectName) && credit is >= 1 and <= 20)
                {
                    _db.Subjects.Add(new E.Subject
                    {
                        SubjectId = subjectId,
                        SubjectName = subjectName,
                        Credit = credit,
                        FacultyId = facultyId
                    });
                    staged.Add(subjectId);
                }
            }
        }

        private async Task<List<E.ExamSchedule>> MapExamSchedules(List<Dictionary<string, string>> rows, ImportResultDto result, CancellationToken ct)
        {
            if (rows.Any(x => Val(x, "__SchoolSchedule") == "1"))
            {
                var offeringRows = rows
                    .GroupBy(x => string.Join("|", Val(x, "Tên đăng nhập giảng viên"), Val(x, "Năm học"), Val(x, "Học kỳ"), Val(x, "Mã môn"), Val(x, "Lớp học phần"), Val(x, "Nhóm")), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var stagedOfferings = await MapCourseOfferingsWithSubjects(offeringRows, result, ct);
                if (!result.Errors.Any())
                    await UpsertCourseOfferingsAsync(stagedOfferings, ct);
            }

            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId }).ToListAsync(ct);
            var periods = await _db.ExamPeriods.Select(x => new { x.PeriodId, x.PeriodName, x.SemesterId }).ToListAsync(ct);
            var sessions = await _db.ExamSessions.Select(x => new { x.SessionId, x.SessionName, x.PeriodId }).ToListAsync(ct);
            var slots = await _db.ExamSlots.Select(x => new { x.SlotId, x.SlotName, x.SessionId }).ToListAsync(ct);
            var rooms = await _db.Rooms.Select(x => new ImportRoomLookup(x.RoomId, x.RoomName, x.BuildingId)).ToListAsync(ct);
            var examFormats = await _db.ExamFormats.Where(x => x.IsActive).Select(x => new { x.ExamFormatId, x.Code, x.Name }).ToListAsync(ct);
            var usersById = await _db.Users.Select(x => new { x.UserId, x.UserName }).ToDictionaryAsync(x => x.UserId, x => x.UserName, ct);
            var offerings = (await _db.CourseOfferings
                    .Include(x => x.User)
                    .Select(x => new ImportOfferingLookup(x.OfferingId, x.User.UserName, x.SemesterId, x.SubjectId, x.ClassName, x.GroupNumber, null))
                    .ToListAsync(ct))
                .Concat(_db.CourseOfferings.Local.Select(x => new ImportOfferingLookup(
                    0,
                    usersById.TryGetValue(x.UserId, out var userName) ? userName : string.Empty,
                    x.SemesterId,
                    x.SubjectId,
                    x.ClassName,
                    x.GroupNumber,
                    x)))
                .ToList();
            var list = new List<E.ExamSchedule>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var yearId = year?.AcademyYearId ?? 0;
                var semester = ResolveSemester(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var semesterId = semester?.SemesterId ?? 0;
                var period = ResolveExamPeriod(periods.Where(x => semester == null || x.SemesterId == semester.SemesterId), x => x.PeriodName, Val(row, "Đợt thi"), result, r, "Đợt thi");
                var periodId = period?.PeriodId ?? 0;
                var sessionRaw = Val(row, "Buổi thi");
                var slotRaw = Val(row, "Ca thi");
                var periodSessions = sessions.Where(x => period == null || x.PeriodId == period.PeriodId).ToList();
                var targetSlots = ResolveScheduleTargets(periodSessions, slots, sessionRaw, slotRaw, result, r);
                var room = ResolveImportRoom(rooms, Val(row, "Mã giảng đường"), Val(row, "Tên phòng"), result, r);
                var roomId = room?.RoomId ?? 0;
                var examFormatValue = Val(row, "Hình thức thi").Trim();
                Required(result, r, "Hình thức thi", examFormatValue);
                var examFormat = examFormats.FirstOrDefault(x => IsExamFormatMatch(x.Code, x.Name, examFormatValue));
                if (examFormat == null) result.Errors.Add(Error(r, "Hình thức thi", examFormatValue, "Không tồn tại trong danh mục hình thức thi."));
                var offering = offerings.FirstOrDefault(x => x.SemesterId == semesterId && string.Equals(x.SubjectId, Val(row, "Mã môn"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.UserName, Val(row, "Tên đăng nhập giảng viên"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.ClassName, Val(row, "Lớp học phần"), StringComparison.OrdinalIgnoreCase) && string.Equals(x.GroupNumber, Val(row, "Nhóm"), StringComparison.OrdinalIgnoreCase));
                if (offering == null) result.Errors.Add(Error(r, "Học phần mở", "", "Không tìm thấy học phần mở theo Mã môn + Giảng viên + Lớp + Nhóm + Học kỳ."));
                var offeringId = offering?.OfferingId ?? 0;
                if (!TryDateTime(row, "Ngày thi", result, r, out var examDate)) examDate = default;
                var status = Val(row, "Trạng thái").Trim();
                if (string.IsNullOrWhiteSpace(status)) status = "Chờ phân công";
                if (!ValidScheduleStatuses.Contains(status)) result.Errors.Add(Error(r, "Trạng thái", status, "Không hợp lệ."));
                foreach (var target in targetSlots)
                {
                    var schedule = new E.ExamSchedule { OfferingId = offeringId, AcademyYearId = yearId, SemesterId = semesterId, PeriodId = periodId, SessionId = target.SessionId, SlotId = target.SlotId, RoomId = roomId, Room = room?.Entity, ExamFormatId = examFormat?.ExamFormatId, ExamDate = examDate!.Value, Status = status };
                    if (offering?.Entity != null) schedule.Offering = offering.Entity;
                    list.Add(schedule);
                }
            }
            return list;
        }

        private static List<(int SessionId, int SlotId)> ResolveScheduleTargets<TSession, TSlot>(
            IEnumerable<TSession> sessionSource,
            IEnumerable<TSlot> slotSource,
            string sessionRaw,
            string slotRaw,
            ImportResultDto result,
            int row)
            where TSession : class
            where TSlot : class
        {
            var sessions = sessionSource.ToList();
            var slots = slotSource.ToList();
            var sessionIdProperty = typeof(TSession).GetProperty("SessionId") ?? throw new InvalidOperationException("Session lookup thiếu SessionId.");
            var sessionNameProperty = typeof(TSession).GetProperty("SessionName") ?? throw new InvalidOperationException("Session lookup thiếu SessionName.");
            var slotIdProperty = typeof(TSlot).GetProperty("SlotId") ?? throw new InvalidOperationException("Slot lookup thiếu SlotId.");
            var slotNameProperty = typeof(TSlot).GetProperty("SlotName") ?? throw new InvalidOperationException("Slot lookup thiếu SlotName.");
            var slotSessionIdProperty = typeof(TSlot).GetProperty("SessionId") ?? throw new InvalidOperationException("Slot lookup thiếu SessionId.");

            int SessionId(TSession x) => (int)(sessionIdProperty.GetValue(x) ?? 0);
            string? SessionName(TSession x) => sessionNameProperty.GetValue(x)?.ToString();
            int SlotId(TSlot x) => (int)(slotIdProperty.GetValue(x) ?? 0);
            string? SlotName(TSlot x) => slotNameProperty.GetValue(x)?.ToString();
            int SlotSessionId(TSlot x) => (int)(slotSessionIdProperty.GetValue(x) ?? 0);

            if (IsAllDaySession(sessionRaw))
            {
                if (!string.IsNullOrWhiteSpace(slotRaw) && !IsFullSessionSlot(slotRaw))
                {
                    result.Errors.Add(Error(row, "Ca thi", slotRaw, "Khi buổi thi là Cả ngày, ca thi phải để trống hoặc nhập Nguyên buổi."));
                    return [];
                }

                var allDayTargets = sessions
                    .SelectMany(session => slots
                        .Where(slot => SlotSessionId(slot) == SessionId(session))
                        .Select(slot => (SessionId: SessionId(session), SlotId: SlotId(slot))))
                    .ToList();

                if (allDayTargets.Count > 0) return allDayTargets;
                result.Errors.Add(Error(row, "Buổi thi", sessionRaw, "Không tìm thấy buổi/ca thi nào thuộc đợt thi đã chọn."));
                return [];
            }

            var session = ResolveOne(sessions, SessionName, sessionRaw, result, row, "Buổi thi");
            var sessionId = session == null ? 0 : SessionId(session);
            var sessionSlots = slots
                .Where(x => session != null && SlotSessionId(x) == sessionId)
                .Select(x => (SlotId: SlotId(x), SlotName: SlotName(x)))
                .ToList();

            return ResolveScheduleSlotIds(sessionSlots, slotRaw, result, row)
                .Select(slotId => (sessionId, slotId))
                .ToList();
        }

        private static List<int> ResolveScheduleSlotIds(IEnumerable<(int SlotId, string? SlotName)> source, string raw, ImportResultDto result, int row)
        {
            var slots = source.ToList();
            if (!Required(result, row, "Ca thi", raw)) return [];

            if (IsFullSessionSlot(raw))
            {
                if (slots.Count > 0) return slots.Select(x => x.SlotId).ToList();

                result.Errors.Add(Error(row, "Ca thi", raw, "Không tìm thấy ca thi nào thuộc buổi thi đã chọn."));
                return [];
            }

            var normalized = NormalizeLookup(NormalizeSlotName(raw));
            var matches = slots.Where(x => NormalizeLookup(x.SlotName) == normalized).ToList();
            if (matches.Count >= 1) return [matches.OrderBy(x => x.SlotId).First().SlotId];

            result.Errors.Add(Error(row, "Ca thi", raw, matches.Count == 0 ? "Không tìm thấy dữ liệu khớp trong hệ thống." : "Tên bị trùng trong hệ thống hoặc trong phạm vi cha, cần kiểm tra lại dữ liệu."));
            return [];
        }

        private async Task<List<E.LecturerBusySlot>> MapBusySlots(List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, string currentRole, CancellationToken ct)
        {
            var users = (await _db.Users.Include(x => x.Role).Include(x => x.Information).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var currentFacultyId = await _db.Users.Where(x => x.UserId == currentUserId).Select(x => x.FacultyId).FirstOrDefaultAsync(ct);
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var today = DateTime.Today;
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId, x.EndDate }).ToListAsync(ct);
            var periods = await _db.ExamPeriods.Select(x => new { x.PeriodId, x.PeriodName, x.SemesterId }).ToListAsync(ct);
            var sessions = await _db.ExamSessions.Select(x => new { x.SessionId, x.SessionName, x.PeriodId }).ToListAsync(ct);
            var slots = await _db.ExamSlots.Select(x => new { x.SlotId, x.SlotName, x.SessionId }).ToListAsync(ct);
            var existing = await _db.LecturerBusySlots.Select(x => new { x.UserId, x.SlotId, x.BusyDate }).ToListAsync(ct);
            var existingSet = existing.Select(x => (x.UserId, x.SlotId, x.BusyDate)).ToHashSet();
            var assignmentRows = await _db.ExamInvigilators
                .AsNoTracking()
                .Where(x =>
                    x.Status != ExamInvigilatorStatuses.Rejected &&
                    x.Status != ExamInvigilatorStatuses.RejectedCode &&
                    x.Status != ExamInvigilatorStatuses.Cancelled &&
                    x.Status != ExamInvigilatorStatuses.CancelledCode)
                .Select(x => new
                {
                    x.ExamSchedule.PeriodId,
                    AssigneeFacultyId = x.Assignee.FacultyId,
                    NewAssigneeFacultyId = x.NewAssignee == null ? null : x.NewAssignee.FacultyId
                })
                .ToListAsync(ct);
            var lockedFacultyPeriods = new HashSet<(int FacultyId, int PeriodId)>();
            foreach (var assignment in assignmentRows)
            {
                if (assignment.AssigneeFacultyId.HasValue)
                    lockedFacultyPeriods.Add((assignment.AssigneeFacultyId.Value, assignment.PeriodId));
                if (assignment.NewAssigneeFacultyId.HasValue)
                    lockedFacultyPeriods.Add((assignment.NewAssigneeFacultyId.Value, assignment.PeriodId));
            }
            var seen = new HashSet<(int UserId, int SlotId, DateOnly Date)>();
            var list = new List<E.LecturerBusySlot>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                var userName = Val(row, "Tên đăng nhập giảng viên").Trim();
                if (!users.TryGetValue(userName, out var user)) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Tài khoản không tồn tại."));
                if (currentRole == "Thư ký khoa" && user != null && user.FacultyId != currentFacultyId) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Không thuộc khoa của thư ký hiện tại."));
                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var semester = ResolveSemester(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                if (semester?.EndDate.HasValue == true && semester.EndDate.Value.Date < today)
                    result.Errors.Add(Error(r, "Học kỳ", Val(row, "Học kỳ"), "Không thể import lịch bận vào học kỳ đã kết thúc."));
                var period = ResolveExamPeriod(periods.Where(x => semester == null || x.SemesterId == semester.SemesterId), x => x.PeriodName, Val(row, "Đợt thi"), result, r, "Đợt thi");
                if (user?.FacultyId.HasValue == true && period != null && lockedFacultyPeriods.Contains((user.FacultyId.Value, period.PeriodId)))
                    result.Errors.Add(Error(r, "Đợt thi", period.PeriodName, "Đợt thi của khoa đã bắt đầu phân công giám thị, không thể import lịch bận."));
                var session = ResolveOne(sessions.Where(x => period == null || x.PeriodId == period.PeriodId), x => x.SessionName, Val(row, "Buổi thi"), result, r, "Buổi thi");
                var slot = ResolveOne(slots.Where(x => session == null || x.SessionId == session.SessionId), x => x.SlotName, Val(row, "Ca thi"), result, r, "Ca thi");
                var slotId = slot?.SlotId ?? 0;
                if (!TryDateOnly(row, "Ngày bận", result, r, out var busyDate)) busyDate = default;
                var note = EmptyToNull(Val(row, "Ghi chú"));
                if (string.IsNullOrWhiteSpace(note)) result.Errors.Add(Error(r, "Ghi chú", string.Empty, "Vui lòng nhập lý do bận."));
                var key = (user?.UserId ?? 0, slotId, busyDate);
                if (!seen.Add(key) || existingSet.Contains(key)) result.Errors.Add(Error(r, "Dòng", $"{userName}-{slotId}-{busyDate:yyyy-MM-dd}", "Lịch bận bị trùng."));
                list.Add(new E.LecturerBusySlot { UserId = user?.UserId ?? 0, SlotId = slotId, BusyDate = busyDate, Note = note ?? string.Empty, CreateAt = DateTime.Now, ApprovalStatus = BusyApprovalStatuses.Approved, ApprovedById = currentUserId, ApprovedAt = DateTime.Now });
            }
            return list;
        }

        private async Task<List<E.LecturerPeriodAvailability>> MapPeriodAvailabilities(List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, string currentRole, CancellationToken ct)
        {
            var users = (await _db.Users.Include(x => x.Role).Include(x => x.Information).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var years = await _db.AcademyYears.Select(x => new { x.AcademyYearId, x.AcademyYearName }).ToListAsync(ct);
            var semesters = await _db.Semesters.Select(x => new { x.SemesterId, x.SemesterName, x.AcademyYearId }).ToListAsync(ct);
            var periods = await _db.ExamPeriods.Select(x => new { x.PeriodId, x.PeriodName, x.SemesterId }).ToListAsync(ct);
            var seen = new HashSet<(int UserId, int PeriodId)>();
            var list = new List<E.LecturerPeriodAvailability>();

            foreach (var row in rows)
            {
                var r = RowNo(row);
                var userName = Val(row, "Tên đăng nhập giảng viên").Trim();
                var lastName = Val(row, "Họ");
                var firstName = Val(row, "Tên");
                if (!users.TryGetValue(userName, out var user)) result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Tài khoản không tồn tại."));
                Required(result, r, "Họ", lastName);
                Required(result, r, "Tên", firstName);
                if (user?.Information != null)
                {
                    if (!string.IsNullOrWhiteSpace(lastName) && NormalizePersonName(lastName) != NormalizePersonName(user.Information.LastName))
                        result.Errors.Add(Error(r, "Họ", lastName, $"Không khớp hồ sơ hệ thống ({user.Information.LastName})."));
                    if (!string.IsNullOrWhiteSpace(firstName) && NormalizePersonName(firstName) != NormalizePersonName(user.Information.FirstName))
                        result.Errors.Add(Error(r, "Tên", firstName, $"Không khớp hồ sơ hệ thống ({user.Information.FirstName})."));

                    var importedFullName = NormalizePersonName($"{lastName} {firstName}");
                    var systemFullName = NormalizePersonName($"{user.Information.LastName} {user.Information.FirstName}");
                    if (!string.IsNullOrWhiteSpace(importedFullName) && importedFullName != systemFullName)
                        result.Errors.Add(Error(r, "Họ/Tên", $"{lastName} {firstName}", $"Không cùng tài khoản với tên đăng nhập {userName} ({user.Information.LastName} {user.Information.FirstName})."));
                }
                else if (user != null)
                {
                    result.Errors.Add(Error(r, "Tên đăng nhập", userName, "Tài khoản chưa gắn hồ sơ thông tin để đối chiếu họ tên."));
                }

                var year = ResolveOne(years, x => x.AcademyYearName, Val(row, "Năm học"), result, r, "Năm học");
                var semester = ResolveSemester(semesters.Where(x => year == null || x.AcademyYearId == year.AcademyYearId), x => x.SemesterName, Val(row, "Học kỳ"), result, r, "Học kỳ");
                var period = ResolveExamPeriod(periods.Where(x => semester == null || x.SemesterId == semester.SemesterId), x => x.PeriodName, Val(row, "Đợt thi"), result, r, "Đợt thi");
                var key = (user?.UserId ?? 0, period?.PeriodId ?? 0);
                if (!seen.Add(key)) result.Errors.Add(Error(r, "Dòng", $"{userName}-{period?.PeriodName}", "Tài khoản bị trùng trong danh sách khả thi."));

                list.Add(new E.LecturerPeriodAvailability
                {
                    UserId = user?.UserId ?? 0,
                    PeriodId = period?.PeriodId ?? 0,
                    Note = EmptyToNull(Val(row, "Ghi chú")),
                    Source = "Import",
                    CreatedById = currentUserId,
                    CreatedAt = DateTime.Now
                });
            }

            return list;
        }

        private async Task<List<E.ExamInvigilator>> MapInvigilators(List<Dictionary<string, string>> rows, ImportResultDto result, int currentUserId, CancellationToken ct)
        {
            var currentFacultyId = await _db.Users.Where(x => x.UserId == currentUserId).Select(x => x.FacultyId).FirstOrDefaultAsync(ct);
            var users = (await _db.Users.Include(x => x.Role).ToListAsync(ct)).ToDictionary(x => x.UserName, StringComparer.OrdinalIgnoreCase);
            var schedules = await _db.ExamSchedules.Include(x => x.Offering).ToDictionaryAsync(x => x.ExamScheduleId, ct);
            var supportAvailability = await _db.LecturerPeriodAvailabilities
                .AsNoTracking()
                .Select(x => new { x.UserId, x.PeriodId })
                .ToListAsync(ct);
            var supportAvailabilitySet = supportAvailability.Select(x => (x.UserId, x.PeriodId)).ToHashSet();
            var existing = await _db.ExamInvigilators.Select(x => new { x.ExamScheduleId, x.PositionNo, x.AssigneeId }).ToListAsync(ct);
            var occupiedPositions = existing.Select(x => (x.ExamScheduleId, x.PositionNo)).ToHashSet();
            var assignedUsers = existing.Select(x => (x.ExamScheduleId, x.AssigneeId)).ToHashSet();
            var seenPos = new HashSet<(int ScheduleId, byte Position)>();
            var list = new List<E.ExamInvigilator>();
            foreach (var row in rows)
            {
                var r = RowNo(row);
                E.ExamSchedule? schedule = null;
                if (!TryInt(row, "Mã lịch thi", result, r, out var scheduleId) || !schedules.TryGetValue(scheduleId, out schedule)) result.Errors.Add(Error(r, "Mã lịch thi", Val(row, "Mã lịch thi"), "Không tồn tại."));
                else if (schedule.Offering.UserId == currentUserId) { }
                var userName = Val(row, "Tên đăng nhập giám thị").Trim();
                if (!users.TryGetValue(userName, out var user)) result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Tài khoản không tồn tại."));
                if (user != null && schedule != null && user.FacultyId != currentFacultyId && (!schedule.SupportRequestedAt.HasValue || !supportAvailabilitySet.Contains((user.UserId, schedule.PeriodId))))
                    result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Tài khoản khác khoa chỉ được phân công sau khi lịch đã gửi hỗ trợ CBCT và tài khoản có trong danh sách khả dụng đã import."));
                if (!TryByte(row, "Vị trí", result, r, out var pos) || pos is < 1 or > 2) result.Errors.Add(Error(r, "Vị trí", Val(row, "Vị trí"), "Chỉ nhận 1 hoặc 2."));
                if (!seenPos.Add((scheduleId, pos)) || occupiedPositions.Contains((scheduleId, pos))) result.Errors.Add(Error(r, "Vị trí", pos.ToString(), "Vị trí giám thị của lịch thi đã có người hoặc bị trùng trong file."));
                if (user != null && assignedUsers.Contains((scheduleId, user.UserId))) result.Errors.Add(Error(r, "Tên đăng nhập giám thị", userName, "Tài khoản đã được phân công ở lịch này."));
                var status = Val(row, "Trạng thái").Trim();
                if (string.IsNullOrWhiteSpace(status)) status = ExamInvigilatorStatuses.PendingConfirmation;
                if (!ValidInvigilatorStatuses.Contains(status)) result.Errors.Add(Error(r, "Trạng thái", status, "Không hợp lệ."));
                list.Add(new E.ExamInvigilator { ExamScheduleId = scheduleId, AssigneeId = user?.UserId ?? 0, AssignerId = currentUserId, PositionNo = pos, Status = status, CreateAt = DateTime.Now });
            }
            return list;
        }

        private async Task<int> AddEntitiesAsync(string module, List<object> entities, CancellationToken ct)
        {
            switch (module)
            {
                case "subject": return await UpsertSubjectsAsync(entities.Cast<E.Subject>(), ct);
                case "information-user": _db.Users.AddRange(entities.Cast<E.User>()); return entities.Count;
                case "course-offering": return await UpsertCourseOfferingsAsync(entities.Cast<E.CourseOffering>(), ct);
                case "exam-schedule": return await UpsertExamSchedulesAsync(entities.Cast<E.ExamSchedule>(), ct);
                case "lecturer-busy-slot": _db.LecturerBusySlots.AddRange(entities.Cast<E.LecturerBusySlot>()); return entities.Count;
                case "lecturer-period-availability": return await UpsertPeriodAvailabilitiesAsync(entities.Cast<E.LecturerPeriodAvailability>(), ct);
                case "exam-invigilator": _db.ExamInvigilators.AddRange(entities.Cast<E.ExamInvigilator>()); return entities.Count;
            }

            return 0;
        }

        private async Task<int> UpsertPeriodAvailabilitiesAsync(IEnumerable<E.LecturerPeriodAvailability> items, CancellationToken ct)
        {
            var incoming = items.Where(x => x.UserId > 0 && x.PeriodId > 0).ToList();
            if (incoming.Count == 0) return 0;

            var userIds = incoming.Select(x => x.UserId).Distinct().ToList();
            var periodIds = incoming.Select(x => x.PeriodId).Distinct().ToList();
            var facultyIds = await _db.Users
                .Where(x => userIds.Contains(x.UserId) && x.FacultyId.HasValue)
                .Select(x => x.FacultyId!.Value)
                .Distinct()
                .ToListAsync(ct);
            var incomingKeys = incoming.Select(x => (x.UserId, x.PeriodId)).ToHashSet();

            var existing = await _db.LecturerPeriodAvailabilities
                .Where(x => periodIds.Contains(x.PeriodId) && x.User.FacultyId.HasValue && facultyIds.Contains(x.User.FacultyId.Value))
                .ToListAsync(ct);
            var existingMap = existing.ToDictionary(x => (x.UserId, x.PeriodId));
            var changed = 0;

            foreach (var stale in existing.Where(x => !incomingKeys.Contains((x.UserId, x.PeriodId))).ToList())
            {
                _db.LecturerPeriodAvailabilities.Remove(stale);
                changed++;
            }

            foreach (var item in incoming)
            {
                if (existingMap.TryGetValue((item.UserId, item.PeriodId), out var current))
                {
                    current.Note = item.Note;
                    current.Source = item.Source;
                    current.CreatedById = item.CreatedById;
                    current.CreatedAt = DateTime.Now;
                    changed++;
                }
                else
                {
                    _db.LecturerPeriodAvailabilities.Add(item);
                    changed++;
                }
            }

            return changed;
        }

        private async Task<int> UpsertSubjectsAsync(IEnumerable<E.Subject> subjects, CancellationToken ct)
        {
            var incoming = subjects
                .Where(x => !string.IsNullOrWhiteSpace(x.SubjectId))
                .GroupBy(x => x.SubjectId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .ToList();

            if (incoming.Count == 0) return 0;

            var ids = incoming.Select(x => x.SubjectId).ToList();
            var existing = await _db.Subjects
                .Where(x => ids.Contains(x.SubjectId))
                .ToListAsync(ct);
            var existingById = existing.ToDictionary(x => x.SubjectId, StringComparer.OrdinalIgnoreCase);

            foreach (var item in incoming)
            {
                if (existingById.TryGetValue(item.SubjectId, out var current))
                {
                    current.SubjectName = item.SubjectName;
                    current.Credit = item.Credit;
                    current.FacultyId = item.FacultyId;
                    continue;
                }

                _db.Subjects.Add(item);
            }

            return incoming.Count;
        }

        private async Task<int> UpsertCourseOfferingsAsync(IEnumerable<E.CourseOffering> offerings, CancellationToken ct)
        {
            var incoming = offerings
                .GroupBy(x => CourseOfferingImportKey(x.UserId, x.SemesterId, x.SubjectId, x.ClassName, x.GroupNumber), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .ToList();

            if (incoming.Count == 0) return 0;

            var userIds = incoming.Select(x => x.UserId).Distinct().ToList();
            var semesterIds = incoming.Select(x => x.SemesterId).Distinct().ToList();
            var subjectIds = incoming.Select(x => x.SubjectId).Distinct().ToList();
            var existing = await _db.CourseOfferings
                .Where(x => userIds.Contains(x.UserId)
                    && semesterIds.Contains(x.SemesterId)
                    && subjectIds.Contains(x.SubjectId))
                .ToListAsync(ct);
            var existingByKey = existing.ToDictionary(x => CourseOfferingImportKey(x.UserId, x.SemesterId, x.SubjectId, x.ClassName, x.GroupNumber), StringComparer.OrdinalIgnoreCase);

            foreach (var item in incoming)
            {
                if (existingByKey.ContainsKey(CourseOfferingImportKey(item.UserId, item.SemesterId, item.SubjectId, item.ClassName, item.GroupNumber)))
                    continue;

                _db.CourseOfferings.Add(item);
            }

            return incoming.Count;
        }

        private static string CourseOfferingImportKey(int userId, int semesterId, string subjectId, string className, string groupNumber)
            => $"{userId}|{semesterId}|{subjectId}|{className}|{groupNumber}";

        private async Task<int> UpsertExamSchedulesAsync(IEnumerable<E.ExamSchedule> schedules, CancellationToken ct)
        {
            var incoming = schedules.ToList();
            await EnsureImportedScheduleRoomsAsync(incoming, ct);

            incoming = incoming
                .GroupBy(x => ScheduleImportKey(x.OfferingId, x.RoomId, x.ExamDate, x.SlotId), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .ToList();

            if (incoming.Count == 0) return 0;

            var offeringIds = incoming.Select(x => x.OfferingId).Distinct().ToList();
            var roomIds = incoming.Select(x => x.RoomId).Distinct().ToList();
            var slotIds = incoming.Select(x => x.SlotId).Distinct().ToList();
            var dates = incoming.Select(x => x.ExamDate.Date).Distinct().ToList();
            var existing = await _db.ExamSchedules
                .Where(x => offeringIds.Contains(x.OfferingId)
                    && roomIds.Contains(x.RoomId)
                    && slotIds.Contains(x.SlotId)
                    && dates.Contains(x.ExamDate.Date))
                .ToListAsync(ct);

            var existingByKey = existing
                .GroupBy(x => ScheduleImportKey(x.OfferingId, x.RoomId, x.ExamDate, x.SlotId), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in incoming)
            {
                if (existingByKey.TryGetValue(ScheduleImportKey(item.OfferingId, item.RoomId, item.ExamDate, item.SlotId), out var current))
                {
                    current.AcademyYearId = item.AcademyYearId;
                    current.SemesterId = item.SemesterId;
                    current.PeriodId = item.PeriodId;
                    current.SessionId = item.SessionId;
                    current.ExamFormatId = item.ExamFormatId;
                    current.Status = item.Status;
                    continue;
                }

                _db.ExamSchedules.Add(item);
            }

            return incoming.Count;
        }

        private async Task EnsureImportedScheduleRoomsAsync(List<E.ExamSchedule> schedules, CancellationToken ct)
        {
            var roomsToResolve = schedules
                .Where(x => x.RoomId <= 0 && x.Room != null)
                .Select(x => x.Room!)
                .GroupBy(x => RoomImportKey(x.BuildingId, x.RoomName), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            if (roomsToResolve.Count == 0) return;

            var buildingIds = roomsToResolve.Select(x => x.BuildingId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingBuildings = await _db.Buildings
                .Where(x => buildingIds.Contains(x.BuildingId))
                .Select(x => x.BuildingId)
                .ToListAsync(ct);
            var existingBuildingSet = existingBuildings.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var buildingId in buildingIds.Where(x => !existingBuildingSet.Contains(x)))
            {
                _db.Buildings.Add(new E.Building
                {
                    BuildingId = buildingId,
                    BuildingName = BuildImportedBuildingName(buildingId)
                });
            }

            var existingRooms = await _db.Rooms
                .Where(x => buildingIds.Contains(x.BuildingId))
                .ToListAsync(ct);
            var existingRoomByKey = existingRooms
                .GroupBy(x => RoomImportKey(x.BuildingId, x.RoomName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var room in roomsToResolve)
            {
                if (existingRoomByKey.ContainsKey(RoomImportKey(room.BuildingId, room.RoomName)))
                    continue;

                room.Building = null!;
                _db.Rooms.Add(room);
                existingRoomByKey[RoomImportKey(room.BuildingId, room.RoomName)] = room;
            }

            await _db.SaveChangesAsync(ct);

            foreach (var schedule in schedules.Where(x => x.RoomId <= 0 && x.Room != null))
            {
                var room = existingRoomByKey[RoomImportKey(schedule.Room!.BuildingId, schedule.Room.RoomName)];
                schedule.RoomId = room.RoomId;
                schedule.Room = null!;
            }
        }

        private static string ScheduleImportKey(int offeringId, int roomId, DateTime examDate, int slotId)
            => $"{offeringId}|{roomId}|{examDate:yyyy-MM-dd}|{slotId}";

        private List<Dictionary<string, string>> ReadRows(string module, IFormFile file, ImportResultDto result)
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

                var templateColumns = GetTemplateColumns(module);
                var requiredHeaders = templateColumns.Where(x => x.Required).Select(x => x.Header).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var headerRow = excelRows.FirstOrDefault(row =>
                {
                    var rowHeaders = row.Elements<Cell>()
                        .Select(c => GetCellValue(workbookPart, c).Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    return requiredHeaders.Count > 0 && requiredHeaders.All(rowHeaders.Contains);
                });

                if (headerRow == null && module is "subject" or "course-offering" or "exam-schedule")
                    return ReadSchoolExamScheduleRows(module, workbookPart, excelRows, file.FileName);

                headerRow ??= excelRows[0];

                var headerRowIndex = headerRow.RowIndex?.Value ?? 1;
                var headersByColumn = headerRow.Elements<Cell>()
                    .Select(c => new { Column = GetColumnName(c.CellReference?.Value), Header = GetCellValue(workbookPart, c).Trim() })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Column) && !string.IsNullOrWhiteSpace(x.Header))
                    .ToDictionary(x => x.Column!, x => x.Header, StringComparer.OrdinalIgnoreCase);
                foreach (var row in excelRows.Where(x => (x.RowIndex?.Value ?? 0) > headerRowIndex))
                {
                    var currentRowIndex = row.RowIndex?.Value ?? 0;
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["__RowNumber"] = row.RowIndex?.Value.ToString() ?? "0" };
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var column = GetColumnName(cell.CellReference?.Value);
                        if (column != null && headersByColumn.TryGetValue(column, out var header))
                            dict[header] = GetCellValue(workbookPart, cell);
                    }
                    foreach (var header in headersByColumn.Values)
                        dict.TryAdd(header, string.Empty);
                    if (IsTemplateHelperRow(dict, templateColumns)) continue;
                    if (dict.Where(x => x.Key != "__RowNumber").Any(x => !string.IsNullOrWhiteSpace(x.Value))) rows.Add(dict);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(Error(0, "File", file.FileName, "Không đọc được file .xlsx: " + ex.Message));
            }
            return rows;
        }

        private static List<Dictionary<string, string>> ReadSchoolExamScheduleRows(string module, WorkbookPart workbookPart, List<Row> excelRows, string fileName)
        {
            var rows = new List<Dictionary<string, string>>();
            var headerRow = excelRows.FirstOrDefault(row =>
            {
                var values = ReadRowCells(workbookPart, row).Values.Select(NormalizeLookup).ToHashSet();
                return values.Contains(NormalizeLookup("Mã học phần"))
                    || values.Contains(NormalizeLookup("Ma học phần"))
                    || values.Contains(NormalizeLookup("Ma HP"))
                    || values.Contains(NormalizeLookup("Mã HP"));
            });

            if (headerRow == null) return rows;

            var headerIndex = headerRow.RowIndex?.Value ?? 1;
            var headersByColumn = ReadRowCells(workbookPart, headerRow);
            var title = excelRows.TakeWhile(x => (x.RowIndex?.Value ?? 0) < headerIndex)
                .SelectMany(x => ReadRowCells(workbookPart, x).Values)
                .FirstOrDefault(x => NormalizeLookup(x).Contains("danh sach hoc phan") || NormalizeLookup(x).Contains("danh sách học phần")) ?? string.Empty;
            var periodSource = FirstNonEmpty(title, fileName);

            foreach (var row in excelRows.Where(x => (x.RowIndex?.Value ?? 0) > headerIndex))
            {
                var cells = ReadRowCells(workbookPart, row);
                string Get(params string[] names)
                {
                    foreach (var name in names)
                    {
                        var column = headersByColumn.FirstOrDefault(x => HeaderMatches(x.Value, name)).Key;
                        if (!string.IsNullOrWhiteSpace(column) && cells.TryGetValue(column, out var value)) return value.Trim();
                    }
                    return string.Empty;
                }

                var subjectId = Get("Mã học phần", "Ma học phần", "Ma HP", "Mã HP");
                if (string.IsNullOrWhiteSpace(subjectId)) continue;

                var lecturer = Get("Cán bộ giảng dạy");
                var roomRaw = Get("Tên phòng thi", "Tên phòng", "phòng thi");
                var building = Get("Dãy phòng", "Day phòng");
                if (string.IsNullOrWhiteSpace(building)) building = ParseBuilding(roomRaw);
                var credit = FirstNonEmpty(Get("Tên chữ"), Get("Số tín chỉ"), Get("Tin chỉ"), Get("Tín chỉ"));
                var subjectFaculty = ParseOrgName(FirstNonEmpty(Get("Đơn vị quản lý học phần"), Get("Don vị quản lý học phần"), Get("Don vi quản lý học phần"), Get("Đơn vị quản lí học phần")));

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["__RowNumber"] = row.RowIndex?.Value.ToString() ?? "0",
                    ["__SchoolSchedule"] = "1",
                    ["Mã môn"] = subjectId,
                    ["Tên môn"] = Get("Tên học phần"),
                    ["Số tín chỉ"] = credit,
                    ["Tên khoa"] = subjectFaculty,
                    ["Tên đăng nhập giảng viên"] = ParseLecturerCode(lecturer),
                    ["Lớp học phần"] = Get("Lớp HP"),
                    ["Nhóm"] = FirstNonEmpty(Get("Nhóm HP"), Get("Nhóm thi"), "01"),
                    ["Năm học"] = InferAcademyYear(title, Get("Ngày thi")),
                    ["Học kỳ"] = "Học kỳ 2",
                    ["Đợt thi"] = InferPeriod(periodSource),
                    ["Buổi thi"] = Get("Buổi thi"),
                    ["Ca thi"] = NormalizeSlotName(Get("Ca thi")),
                    ["Mã giảng đường"] = building,
                    ["Tên phòng"] = ParseRoomName(roomRaw, building),
                    ["Ngày thi"] = Get("Ngày thi"),
                    ["Hình thức thi"] = NormalizeExamFormat(Get("Tên hình thức thi")),
                    ["Trạng thái"] = "Chờ phân công"
                };

                rows.Add(dict);
            }

            return module switch
            {
                "subject" => rows
                    .GroupBy(x => Val(x, "Mã môn"), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList(),
                "course-offering" => rows
                    .GroupBy(x => string.Join("|", Val(x, "Tên đăng nhập giảng viên"), Val(x, "Năm học"), Val(x, "Học kỳ"), Val(x, "Mã môn"), Val(x, "Lớp học phần"), Val(x, "Nhóm")), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList(),
                _ => rows
            };
        }

        private static Dictionary<string, string> ReadRowCells(WorkbookPart workbookPart, Row row)
        {
            return row.Elements<Cell>()
                .Where(c => !string.IsNullOrWhiteSpace(c.CellReference?.Value))
                .ToDictionary(c => GetColumnName(c.CellReference!.Value) ?? string.Empty, c => GetCellValue(workbookPart, c), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsTemplateHelperRow(Dictionary<string, string> row, List<ImportColumnDto> columns)
        {
            var values = columns
                .Select(c => Val(row, c.Header))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (values.Count == 0) return true;

            var exampleValues = columns.Select(x => x.Example ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var descriptionValues = columns.Select(x => x.Description ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return values.All(x => exampleValues.Contains(x)) || values.All(x => descriptionValues.Contains(x));
        }

        private static bool HeaderMatches(string? actual, string expected) => NormalizeHeader(actual) == NormalizeHeader(expected);
        private static string NormalizeHeader(string? value) => NormalizeLookup(value).Replace("đ", "d").Replace(" ", string.Empty);
        private static string FirstNonEmpty(params string[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
        private static string ParseLecturerCode(string value)
        {
            var code = (value ?? string.Empty).Split('-', 2)[0].Trim();
            return string.IsNullOrWhiteSpace(code) ? "UNKNOWN" : code;
        }
        private static string ParseBuilding(string room)
        {
            room = (room ?? string.Empty).Trim();
            var cut = room.IndexOfAny(['.', '-']);
            return cut > 0 ? room[..cut] : string.Empty;
        }
        private static string ParseRoomName(string room, string building)
        {
            room = (room ?? string.Empty).Trim();
            building = (building ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(building) && room.StartsWith(building, StringComparison.OrdinalIgnoreCase))
                room = room[building.Length..].TrimStart('.', '-').Trim();
            return room;
        }

        private static ImportRoomLookup? ResolveImportRoom(List<ImportRoomLookup> rooms, string buildingRaw, string roomRaw, ImportResultDto result, int row)
        {
            var (buildingId, roomName) = NormalizeImportRoomParts(buildingRaw, roomRaw);

            Required(result, row, "Tên phòng", roomName);
            if (string.IsNullOrWhiteSpace(roomName)) return null;

            if (buildingId.Length > 10)
            {
                result.Errors.Add(Error(row, "Mã giảng đường", buildingId, "Tối đa 10 ký tự."));
                return null;
            }

            if (roomName.Length > 50)
            {
                result.Errors.Add(Error(row, "Tên phòng", roomName, "Tối đa 50 ký tự."));
                return null;
            }

            var existing = rooms.FirstOrDefault(x =>
                string.Equals(x.BuildingId, buildingId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.RoomName, roomName, StringComparison.OrdinalIgnoreCase));

            if (existing != null) return existing;

            var created = new ImportRoomLookup(0, roomName, buildingId)
            {
                Entity = new E.Room
                {
                    BuildingId = buildingId,
                    RoomName = roomName
                }
            };
            rooms.Add(created);
            return created;
        }

        private static (string BuildingId, string RoomName) NormalizeImportRoomParts(string buildingRaw, string roomRaw)
        {
            var room = NormalizeImportRoomName(roomRaw);
            var building = NormalizeImportBuildingId(buildingRaw);

            if (string.IsNullOrWhiteSpace(building))
            {
                var parsedBuilding = ParseBuilding(room);
                if (!string.Equals(parsedBuilding, room, StringComparison.OrdinalIgnoreCase))
                {
                    building = NormalizeImportBuildingId(parsedBuilding);
                    room = NormalizeImportRoomName(ParseRoomName(room, parsedBuilding));
                }
            }
            else
            {
                room = NormalizeImportRoomName(ParseRoomName(room, building));
            }

            if (string.IsNullOrWhiteSpace(building))
                building = "KHAC";

            return (building, room);
        }

        private static string NormalizeImportBuildingId(string value)
        {
            value = (value ?? string.Empty).Trim();
            value = RegexReplaceWhitespace(value, string.Empty);
            return value.ToUpperInvariant();
        }

        private static string NormalizeImportRoomName(string value)
        {
            value = (value ?? string.Empty).Trim();
            value = RegexReplaceWhitespace(value, " ");
            return value.ToUpperInvariant();
        }

        private static string RegexReplaceWhitespace(string value, string replacement)
        {
            var builder = new StringBuilder(value.Length);
            var lastWasWhitespace = false;
            foreach (var ch in value)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasWhitespace && replacement.Length > 0)
                        builder.Append(replacement);
                    lastWasWhitespace = true;
                    continue;
                }

                builder.Append(ch);
                lastWasWhitespace = false;
            }

            return builder.ToString().Trim();
        }

        private static string RoomImportKey(string buildingId, string roomName) => $"{buildingId}|{roomName}";

        private static string BuildImportedBuildingName(string buildingId)
        {
            return string.Equals(buildingId, "KHAC", StringComparison.OrdinalIgnoreCase)
                ? "Phòng thi độc lập/khác"
                : $"Khu {buildingId}";
        }

        private static string ParseOrgName(string value)
        {
            value = (value ?? string.Empty).Trim();
            var parts = value.Split('-', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? parts[1] : value;
        }

        private static string NormalizeSlotName(string slot)
        {
            slot = (slot ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(slot) || IsFullSessionSlot(slot)) return slot;
            return NormalizeLookup(slot).StartsWith("ca ") ? slot : $"Ca {slot}";
        }

        private static bool IsFullSessionSlot(string value)
        {
            var normalized = NormalizeLookup(value);
            return normalized is "nguyen buoi" or "ca nguyen buoi" or "ca nguyen" or "nguyen ca" or "ca buoi" or "ca buong"
                || normalized.Contains("nguyen buoi")
                || normalized.Contains("ca buoi")
                || normalized.Contains("ca buổi")
                || normalized.Contains("cả buổi");
        }

        private static bool IsAllDaySession(string value)
        {
            var normalized = NormalizeLookup(value);
            return normalized is "ca ngay" or "cả ngày" or "nguyen ngay" or "nguyên ngày" or "toan ngay" or "toàn ngày"
                || normalized.Contains("ca ngay")
                || normalized.Contains("nguyen ngay")
                || normalized.Contains("toan ngay");
        }
        private static string InferAcademyYear(string title, string date)
        {
            var normalized = title ?? string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(normalized, @"20\d{2}\s*-\s*20\d{2}");
            if (match.Success) return match.Value.Replace(" ", string.Empty);
            if (DateTime.TryParse(date, out var dt)) return $"{dt.Year - 1}-{dt.Year}";
            return "2025-2026";
        }
        private static string InferPeriod(string title)
        {
            var normalized = NormalizeLookup(title);
            var dotMatch = System.Text.RegularExpressions.Regex.Match(normalized, @"\b(?:dot|đot|đợt)\s*(\d+)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (dotMatch.Success) return $"Đợt {dotMatch.Groups[1].Value}";
            if (ContainsFinalExamPeriod(normalized)) return "Cuối kỳ";
            if (ContainsMidtermExamPeriod(normalized)) return "Giữa kỳ";
            return "Cuối kỳ";
        }

        private static bool ContainsFinalExamPeriod(string normalized)
        {
            return normalized.Contains("cuoi ky")
                || normalized.Contains("cuoi ki")
                || normalized.Contains("ket thuc hoc ky")
                || normalized.Contains("ket thuc hoc ki")
                || normalized.Contains("thi ket thuc")
                || normalized.Contains("final");
        }

        private static bool ContainsMidtermExamPeriod(string normalized)
        {
            return normalized.Contains("giua ky")
                || normalized.Contains("giua ki")
                || normalized.Contains("midterm")
                || normalized.Contains("mid term");
        }
        private static string NormalizeExamFormat(string value)
        {
            value = (value ?? string.Empty).Trim();
            var code = value.ToUpperInvariant();
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\s+", string.Empty);
            code = System.Text.RegularExpressions.Regex.Replace(code, @"[-/]", "-");
            return code switch
            {
                "TN" => "TN",
                "TN-TL" => "TN-TL",
                "BTL" => "BTL",
                "BTL-VD" => "BTL-VD",
                "TL-VD" => "TL-VD",
                "NTL-VD" => "NTL-VD",
                "TL" => "TL",
                "PM" => "PM",
                "PTH" or "TH" => "TH",
                "DA" => "DA",
                "VD" => "VD",
                _ => string.IsNullOrWhiteSpace(code) ? value : code
            };
        }

        private static bool IsExamFormatMatch(string? code, string? name, string raw)
        {
            var rawKey = NormalizeExamFormatLookup(raw);
            return rawKey == NormalizeExamFormatLookup(code)
                || rawKey == NormalizeExamFormatLookup(name)
                || rawKey == NormalizeExamFormatLookup($"{code} - {name}")
                || rawKey == NormalizeExamFormatLookup($"{code}/{name}");
        }

        private static string NormalizeExamFormatLookup(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(ch))
                    builder.Append(ch == 'Đ' ? 'D' : ch);
            }

            return System.Text.RegularExpressions.Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[-/]", "-");
        }

        private static Columns BuildImportColumns(IReadOnlyList<ImportColumnDto> columns)
        {
            var cols = new Columns();
            for (var i = 0; i < columns.Count; i++)
            {
                var width = Math.Clamp(columns[i].Header.Length * 1.2 + 10, 14, 34);
                cols.Append(new Column
                {
                    Min = (uint)(i + 1),
                    Max = (uint)(i + 1),
                    Width = width,
                    CustomWidth = true
                });
            }

            return cols;
        }

        private static Stylesheet BuildImportStylesheet()
        {
            var fonts = new Fonts(
                new Font(),
                new Font(
                    new Bold(),
                    new Color { Rgb = "FFFFFF" }),
                new Font(
                    new Bold(),
                    new FontSize { Val = 14D }),
                new Font(
                    new Bold(),
                    new FontSize { Val = 11D }),
                new Font(
                    new Italic(),
                    new FontSize { Val = 10D },
                    new Color { Rgb = "64748B" }));

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = "1D4ED8" }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = "EFF6FF" }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = "F8FAFC" }) { PatternType = PatternValues.Solid }));

            var borders = new Borders(
                new Border(),
                new Border(
                    new LeftBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new RightBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new BottomBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new DiagonalBorder()),
                new Border(
                    new LeftBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new RightBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new TopBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new BottomBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
                    new DiagonalBorder()));

            var cellFormats = new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0, ApplyFont = true },
                new CellFormat { FontId = 1, FillId = 2, BorderId = 0, ApplyFont = true, ApplyAlignment = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center } },
                new CellFormat { FontId = 2, FillId = 4, BorderId = 0, ApplyFont = true, ApplyAlignment = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center } },
                new CellFormat { FontId = 3, FillId = 3, BorderId = 1, ApplyFont = true, ApplyFill = true, ApplyBorder = true, ApplyAlignment = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center } },
                new CellFormat { FontId = 4, FillId = 4, BorderId = 0, ApplyFont = true, ApplyFill = true, ApplyAlignment = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, WrapText = true } },
                new CellFormat { FontId = 0, FillId = 0, BorderId = 2, ApplyBorder = true, ApplyAlignment = true, Alignment = new Alignment { WrapText = true } });

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }

        private static Row BuildRow(uint index, IEnumerable<string> values, uint styleIndex = 0)
        {
            var row = new Row { RowIndex = index };
            var column = 1;
            foreach (var value in values)
            {
                row.Append(new Cell
                {
                    CellReference = GetExcelColumnName(column++) + index,
                    DataType = CellValues.String,
                    CellValue = new CellValue(value ?? string.Empty),
                    StyleIndex = styleIndex
                });
            }
            return row;
        }

        private static string GetCellValue(WorkbookPart workbookPart, Cell? cell)
        {
            if (cell == null) return string.Empty;
            if (cell.DataType?.Value == CellValues.InlineString) return cell.InlineString?.InnerText ?? string.Empty;
            if (cell.CellValue == null) return string.Empty;
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
            if (double.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var oaDate) && oaDate > 20000 && oaDate < 60000)
            {
                value = DateTime.FromOADate(oaDate).Date;
                return true;
            }
            if (DateTime.TryParseExact(raw, ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "M/d/yyyy", "dd-MM-yyyy", "d-M-yyyy", "M-d-yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) || DateTime.TryParse(raw, out dt)) { value = dt.Date; return true; }
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

        private static T? ResolveSemester<T>(IEnumerable<T> source, Func<T, string?> selector, string raw, ImportResultDto result, int row, string column) where T : class
        {
            if (!Required(result, row, column, raw)) return null;

            var list = source.ToList();
            var normalized = NormalizeLookup(raw);
            var matches = list.Where(x => NormalizeLookup(selector(x)) == normalized).ToList();
            if (matches.Count == 1) return matches[0];

            var canonical = NormalizeSemesterName(raw);
            if (!string.IsNullOrWhiteSpace(canonical))
            {
                var canonicalMatches = list.Where(x => NormalizeSemesterName(selector(x)) == canonical).ToList();
                if (canonicalMatches.Count == 1) return canonicalMatches[0];
            }

            result.Errors.Add(Error(row, column, raw, matches.Count == 0 ? "Không tìm thấy dữ liệu khớp trong hệ thống." : "Tên bị trùng trong hệ thống hoặc trong phạm vi cha, cần kiểm tra lại dữ liệu."));
            return null;
        }

        private static T? ResolveExamPeriod<T>(IEnumerable<T> source, Func<T, string?> selector, string raw, ImportResultDto result, int row, string column) where T : class
        {
            if (!Required(result, row, column, raw)) return null;

            var list = source.ToList();
            var normalized = NormalizeLookup(raw);
            var matches = list.Where(x => NormalizeLookup(selector(x)) == normalized).ToList();
            if (matches.Count == 1) return matches[0];

            var fallbackNames = new List<string>();
            if (normalized.StartsWith("dot ") || normalized.StartsWith("dot")) fallbackNames.Add("Cuối kỳ");
            if (ContainsFinalExamPeriod(normalized)) fallbackNames.Add("Đợt 1");
            if (ContainsMidtermExamPeriod(normalized)) fallbackNames.Add("Giữa kỳ");

            foreach (var fallback in fallbackNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var fallbackMatches = list.Where(x => NormalizeLookup(selector(x)) == NormalizeLookup(fallback)).ToList();
                if (fallbackMatches.Count == 1) return fallbackMatches[0];
            }

            if (matches.Count == 0 && list.Count == 1) return list[0];

            result.Errors.Add(Error(row, column, raw, matches.Count == 0 ? "Không tìm thấy dữ liệu khớp trong hệ thống." : "Tên bị trùng trong hệ thống hoặc trong phạm vi cha, cần kiểm tra lại dữ liệu."));
            return null;
        }

        private static T? ResolveFaculty<T>(IEnumerable<T> source, Func<T, string?> selector, string raw, ImportResultDto result, int row, string column) where T : class
        {
            if (!Required(result, row, column, raw)) return null;

            var list = source.ToList();
            var exact = list.Where(x => NormalizeLookup(selector(x)) == NormalizeLookup(raw)).ToList();
            if (exact.Count == 1) return exact[0];

            var normalized = NormalizeOrgUnitLookup(raw);
            var matches = list.Where(x => NormalizeOrgUnitLookup(selector(x)) == normalized).ToList();
            if (matches.Count == 1) return matches[0];

            var preferred = matches
                .Where(x => StartsWithOrgUnitPrefix(selector(x)))
                .ToList();
            if (preferred.Count == 1) return preferred[0];

            result.Errors.Add(Error(row, column, raw, matches.Count == 0 ? "Không tìm thấy khoa/trường/trung tâm khớp trong hệ thống." : "Tên khoa/trường/trung tâm bị trùng theo tên rút gọn, cần gộp dữ liệu trước khi import."));
            return null;
        }

        private static bool StartsWithOrgUnitPrefix(string? value)
        {
            var normalized = NormalizeLookup(value);
            return normalized.StartsWith("khoa ") || normalized.StartsWith("trung tâm ") || normalized.StartsWith("trung tam ") || normalized.StartsWith("trường ") || normalized.StartsWith("truong ");
        }

        private static string NormalizeOrgUnitLookup(string? value)
        {
            var normalized = NormalizeLookup(value);
            return System.Text.RegularExpressions.Regex.Replace(normalized, @"^(khoa|trung tâm|trung tam|trường|truong)\s+", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static string NormalizeSemesterName(string? value)
        {
            var normalized = NormalizeLookup(value);
            if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

            normalized = normalized
                .Replace("hoc ky", "hk")
                .Replace("hoc ki", "hk")
                .Replace("học kỳ", "hk")
                .Replace("học kì", "hk")
                .Replace("ky", "hk")
                .Replace("ki", "hk");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", string.Empty);

            return normalized switch
            {
                "1" or "hk1" or "hki" => "hk1",
                "2" or "hk2" or "hkii" => "hk2",
                "3" or "hk3" or "he" or "hè" => "hk3",
                _ => normalized
            };
        }

        private static string NormalizePersonName(string? value)
            => NormalizeLookup(value).Replace(" ", string.Empty);

        private static string NormalizeLookup(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch == 'đ' ? 'd' : ch);
            }

            return string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private sealed record ImportOfferingLookup(
            int OfferingId,
            string UserName,
            int SemesterId,
            string SubjectId,
            string ClassName,
            string GroupNumber,
            E.CourseOffering? Entity);

        private sealed record ImportRoomLookup(int RoomId, string RoomName, string BuildingId)
        {
            public E.Room? Entity { get; init; }
        }
    }
}
