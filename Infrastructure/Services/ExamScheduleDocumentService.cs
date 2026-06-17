using System.Globalization;
using System.Net;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class ExamScheduleDocumentService : IExamScheduleDocumentService
    {
        public byte[] BuildSupportRequestExcel(ExamScheduleSupportRequestDocumentDto request, byte[] templateBytes)
        {
            using var stream = new MemoryStream();
            stream.Write(templateBytes, 0, templateBytes.Length);
            stream.Position = 0;

            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("File mẫu không hợp lệ.");
                var worksheetPart = workbookPart.WorksheetParts.FirstOrDefault() ?? throw new InvalidOperationException("File mẫu không có worksheet.");
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? throw new InvalidOperationException("Worksheet mẫu không hợp lệ.");

                SetCellValue(worksheetPart, workbookPart, "A1", DocumentLetterhead.Ministry);
                SetCellValue(worksheetPart, workbookPart, "D1", DocumentLetterhead.Nation);
                SetCellValue(worksheetPart, workbookPart, "A2", DocumentLetterhead.School);
                SetCellValue(worksheetPart, workbookPart, "D2", DocumentLetterhead.Motto);
                SetCellValue(worksheetPart, workbookPart, "A4", request.Title);

                var headerRowIndex = FindHeaderRowIndex(sheetData, workbookPart);
                var dataStartIndex = headerRowIndex + 1;
                var templateRow = sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex?.Value == dataStartIndex)
                    ?? sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex?.Value > headerRowIndex)
                    ?? new Row { RowIndex = dataStartIndex };

                var templateRowIndex = templateRow.RowIndex?.Value ?? dataStartIndex;
                var generatedCount = request.Schedules.Count;
                var shiftBy = generatedCount > 0 ? generatedCount - 1 : 0;
                var preservedRows = sheetData.Elements<Row>()
                    .Where(x => (x.RowIndex?.Value ?? 0) > templateRowIndex)
                    .OrderByDescending(x => x.RowIndex?.Value ?? 0)
                    .ToList();

                foreach (var row in preservedRows)
                    ShiftRow(row, (uint)shiftBy);

                if (templateRow.Parent != null)
                    templateRow.Remove();

                for (var i = 0; i < request.Schedules.Count; i++)
                {
                    var rowIndex = (uint)(dataStartIndex + i);
                    var row = (Row)templateRow.CloneNode(true);
                    row.RowIndex = rowIndex;
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var column = GetColumnName(cell.CellReference?.Value ?? "A");
                        cell.CellReference = column + rowIndex;
                        cell.CellValue = null;
                        cell.DataType = null;
                    }

                    var x = request.Schedules[i];
                    SetRowCell(row, "A", rowIndex, (i + 1).ToString());
                    SetRowCell(row, "B", rowIndex, x.SubjectId);
                    SetRowCell(row, "C", rowIndex, x.SubjectName);
                    SetRowCell(row, "D", rowIndex, "TL - Viết Tự luận/Trắc nghiệm");
                    SetRowCell(row, "E", rowIndex, x.Credit?.ToString());
                    SetRowCell(row, "F", rowIndex, x.GroupNumber);
                    SetRowCell(row, "G", rowIndex, x.GroupNumber);
                    SetRowCell(row, "H", rowIndex, x.ClassName);
                    SetRowCell(row, "I", rowIndex, x.ExamDate?.ToString("dd-MM-yyyy"));
                    SetRowCell(row, "J", rowIndex, x.SessionName);
                    SetRowCell(row, "K", rowIndex, GetSlotNumber(x));
                    SetRowCell(row, "L", rowIndex, FormatTime(x.SlotTimeStart));
                    SetRowCell(row, "M", rowIndex, x.RoomName);
                    SetRowCell(row, "N", rowIndex, x.BuildingName ?? x.BuildingId);
                    SetRowCell(row, "O", rowIndex, x.RoomCapacity?.ToString());
                    SetRowCell(row, "P", rowIndex, x.UserName);
                    SetRowCell(row, "Q", rowIndex, x.FacultyName);
                    SetRowCell(row, "R", rowIndex, x.Lecturer1Code);
                    SetRowCell(row, "S", rowIndex, x.Lecturer1Name);
                    SetRowCell(row, "T", rowIndex, x.Lecturer1FacultyName);
                    SetRowCell(row, "U", rowIndex, x.Lecturer2Code);
                    SetRowCell(row, "V", rowIndex, x.Lecturer2Name);
                    SetRowCell(row, "W", rowIndex, x.Lecturer2FacultyName);

                    var anchor = sheetData.Elements<Row>().FirstOrDefault(x => (x.RowIndex?.Value ?? 0) > rowIndex);
                    if (anchor == null)
                        sheetData.Append(row);
                    else
                        sheetData.InsertBefore(row, anchor);
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        public string BuildSupportRequestFileName(ExamScheduleSupportRequestDocumentDto request)
        {
            var year = SanitizeFileName(request.AcademyYearName);
            var semester = SanitizeFileName(request.SemesterName);
            return $"De-nghi-ho-tro-CBCT-{semester}-{year}.xlsx";
        }

        public byte[] BuildExamScheduleExportExcel(IReadOnlyList<ExamScheduleDto> schedules, string? templatePath = null)
        {
            if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath))
                return BuildExamScheduleExportFromTemplate(schedules, templatePath);

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Lịch thi" });

                AppendExportRow(sheetData, 1, ["TRƯỜNG ĐẠI HỌC NHA TRANG"]);
                AppendExportRow(sheetData, 2, ["KHOA/VIỆN/BỘ MÔN"]);
                AppendExportRow(sheetData, 3, [BuildExamScheduleExportTitle(schedules)]);
                AppendExportRow(sheetData, 4, [$"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}"]);
                AppendExportRow(sheetData, 5,
                [
                    "STT", "Mã môn", "Tên môn", "Số TC", "Lớp", "Nhóm", "Hình thức thi", "Ngày thi", "Buổi", "Ca",
                    "Giờ", "Phòng", "Giảng đường", "Sĩ số", "Giảng viên dạy", "CBCT 1", "CBCT 2", "Trạng thái"
                ]);

                for (var i = 0; i < schedules.Count; i++)
                {
                    var item = schedules[i];
                    AppendExportRow(sheetData, (uint)(6 + i),
                    [
                        (i + 1).ToString(), item.SubjectId, item.SubjectName, item.Credit?.ToString(), item.ClassName,
                        item.GroupNumber, FormatExamFormat(item), item.ExamDate?.ToString("dd/MM/yyyy"), item.SessionName,
                        GetSlotNumber(item), FormatTime(item.SlotTimeStart), item.RoomName, item.BuildingName ?? item.BuildingId,
                        item.RoomCapacity?.ToString(), item.UserName, FormatLecturer(item.Lecturer1Code, item.Lecturer1Name),
                        FormatLecturer(item.Lecturer2Code, item.Lecturer2Name), item.Status
                    ]);
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        public string BuildSupportRequestEmailBody(ExamScheduleSupportRequestDocumentDto request, string? replyTo)
        {
            var missingCount = request.Schedules.Sum(CountMissingInvigilators);
            var sender = string.IsNullOrWhiteSpace(request.FacultyName) ? "Khoa quản lý" : $"Khoa {request.FacultyName}";
            var replyLine = string.IsNullOrWhiteSpace(replyTo) ? string.Empty : $"<p>Email phản hồi: <b>{Html(replyTo)}</b></p>";

            return $"<p>Kính gửi Quý đơn vị,</p>" +
                   $"<p>{Html(sender)} gửi đề nghị hỗ trợ thêm CBCT cho <b>{request.Schedules.Count}</b> lịch thi thuộc <b>{Html(request.SemesterName)}</b>, năm học <b>{Html(request.AcademyYearName)}</b>.</p>" +
                   $"<p>Tổng số vị trí CBCT còn cần hỗ trợ: <b>{missingCount}</b>.</p>" +
                   replyLine +
                   "<p>File danh sách chi tiết được đính kèm trong email này.</p>" +
                   "<p>Trân trọng.</p>";
        }

        public async Task<byte[]> GetSupportTemplateBytesAsync(ImportFileDto? uploadedTemplate, string defaultTemplatePath)
        {
            if (uploadedTemplate != null && uploadedTemplate.Length > 0)
            {
                var extension = Path.GetExtension(uploadedTemplate.FileName);
                if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("File mẫu phải là định dạng .xlsx.");

                await using var uploadStream = uploadedTemplate.OpenReadStream();
                using var memory = new MemoryStream();
                await uploadStream.CopyToAsync(memory);
                return memory.ToArray();
            }

            if (!File.Exists(defaultTemplatePath))
                throw new InvalidOperationException("Không tìm thấy file mẫu wwwroot/templates/MAU DE NGHI HO TRO CBCT.xlsx.");

            return await File.ReadAllBytesAsync(defaultTemplatePath);
        }

        private static byte[] BuildExamScheduleExportFromTemplate(IReadOnlyList<ExamScheduleDto> schedules, string templatePath)
        {
            var bytes = File.ReadAllBytes(templatePath);
            using var stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;

            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Template Excel không hợp lệ.");
                var worksheetPart = workbookPart.WorksheetParts.First();
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());
                var headerRowIndex = FindHeaderRowIndex(sheetData, workbookPart);
                var dataStartIndex = headerRowIndex + 1;
                var headerColumns = GetHeaderColumns(sheetData, workbookPart, headerRowIndex);
                var templateRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == dataStartIndex)
                    ?? new Row { RowIndex = dataStartIndex };
                var borderedStyleIndexes = new Dictionary<uint, uint>();

                foreach (var row in sheetData.Elements<Row>().Where(r => r.RowIndex?.Value >= dataStartIndex).ToList())
                    row.Remove();

                for (var i = 0; i < schedules.Count; i++)
                {
                    var item = schedules[i];
                    var rowIndex = dataStartIndex + (uint)i;
                    var row = (Row)templateRow.CloneNode(true);
                    row.RowIndex = rowIndex;
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var column = GetColumnName(cell.CellReference?.Value ?? "A");
                        cell.CellReference = column + rowIndex;
                        cell.CellValue = null;
                        cell.InlineString = null;
                        cell.DataType = null;
                    }

                    foreach (var header in headerColumns)
                    {
                        var value = GetExamScheduleExportValue(item, i + 1, header.HeaderText);
                        if (value != null)
                            SetRowCell(row, header.ColumnName, rowIndex, value);
                    }

                    ApplyDataBorder(row, headerColumns, rowIndex, workbookPart, borderedStyleIndexes);
                    sheetData.Append(row);
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static string BuildExamScheduleExportTitle(IReadOnlyList<ExamScheduleDto> schedules)
        {
            var periods = schedules.Select(x => x.PeriodName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var years = schedules.Select(x => x.AcademyYearName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var dates = schedules.Where(x => x.ExamDate.HasValue).Select(x => x.ExamDate!.Value.Date).ToList();
            var periodText = periods.Count == 0 ? "..." : string.Join(", ", periods);
            var yearText = years.Count == 0 ? "..." : string.Join(", ", years);
            var fromText = dates.Count == 0 ? "....." : dates.Min().ToString("dd/MM/yyyy");
            var toText = dates.Count == 0 ? "....." : dates.Max().ToString("dd/MM/yyyy");
            return $"DANH SÁCH LỊCH PHÂN CÔNG COI THI ĐỢT THI {periodText} NĂM HỌC {yearText} (TỪ NGÀY {fromText} ĐẾN NGÀY {toText})";
        }

        private static void AppendExportRow(SheetData sheetData, uint rowIndex, IReadOnlyList<string?> values)
        {
            var row = new Row { RowIndex = rowIndex };
            for (var i = 0; i < values.Count; i++)
                SetRowCell(row, GetExcelColumnName(i + 1), rowIndex, values[i]);
            sheetData.Append(row);
        }

        private static string? GetExamScheduleExportValue(ExamScheduleDto item, int ordinal, string headerText)
        {
            var key = NormalizeExportHeader(headerText);
            return key switch
            {
                "stt" or "sothutu" => ordinal.ToString(),
                "mahp" or "mahocphan" or "mamon" or "mamonhoc" => item.SubjectId,
                "tenhp" or "tenhocphan" or "tenmon" or "tenmonhoc" => item.SubjectName,
                "tenhinhthucthi" or "hinhthucthi" => FormatExamFormat(item),
                "sotinchi" or "tinchi" or "sotc" or "tc" => item.Credit?.ToString(),
                "lop" or "lophp" or "lophocphan" => item.ClassName,
                "nhom" or "nhomhp" or "nhomthi" => item.GroupNumber,
                "ngaythi" => item.ExamDate?.ToString("dd/MM/yyyy"),
                "buoithi" => item.SessionName,
                "cathi" => GetSlotNumber(item),
                "giothi" or "giobatdau" or "thoigianthi" or "thoigian" => FormatTime(item.SlotTimeStart),
                "tenphongthi" or "phongthi" or "phong" or "tenphong" => FormatRoomDisplay(item),
                "dayphong" or "magiangduong" or "giangduong" or "toa" or "khu" => FormatBuildingDisplay(item),
                "siso" or "succhua" or "succhuaphong" => item.RoomCapacity?.ToString(),
                "canbogiangday" or "giangvienday" or "giangvienphutrach" or "cbgd" => FormatLecturer(item.OfferingUserCode, item.OfferingUserFullName ?? item.UserName),
                "donviquanlyhocphan" or "donviquanly" or "khoaquanly" or "khoa" => item.FacultyName,
                "cbct1" or "canbocoithi1" or "canboct1" or "giamthi1" => FormatLecturer(item.Lecturer1Code, item.Lecturer1Name),
                "macbct1" or "macanbocoithi1" or "magiamthi1" => item.Lecturer1Code,
                "hotencbct1" or "hotencanbocoithi1" or "tencbct1" or "tengiamthi1" => item.Lecturer1Name,
                "donvicbct1" or "khoacbct1" or "donvigiamthi1" => item.Lecturer1FacultyName,
                "cbct2" or "canbocoithi2" or "canboct2" or "giamthi2" => FormatLecturer(item.Lecturer2Code, item.Lecturer2Name),
                "macbct2" or "macanbocoithi2" or "magiamthi2" => item.Lecturer2Code,
                "hotencbct2" or "hotencanbocoithi2" or "tencbct2" or "tengiamthi2" => item.Lecturer2Name,
                "donvicbct2" or "khoacbct2" or "donvigiamthi2" => item.Lecturer2FacultyName,
                "trangthai" => item.Status,
                _ => null
            };
        }

        private static string FormatRoomDisplay(ExamScheduleDto item)
        {
            if (string.Equals(item.BuildingId, "KHAC", StringComparison.OrdinalIgnoreCase)) return item.RoomName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(item.BuildingId)) return item.RoomName ?? string.Empty;
            return string.IsNullOrWhiteSpace(item.RoomName) ? item.BuildingId : $"{item.BuildingId}.{item.RoomName}";
        }

        private static string FormatBuildingDisplay(ExamScheduleDto item)
            => string.Equals(item.BuildingId, "KHAC", StringComparison.OrdinalIgnoreCase) ? string.Empty : item.BuildingName ?? item.BuildingId ?? string.Empty;

        private static string NormalizeExportHeader(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) builder.Append(ch == 'đ' ? 'd' : ch);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static int CountMissingInvigilators(ExamScheduleDto schedule)
        {
            var assigned = 0;
            if (!string.IsNullOrWhiteSpace(schedule.Lecturer1Name)) assigned++;
            if (!string.IsNullOrWhiteSpace(schedule.Lecturer2Name)) assigned++;
            return Math.Max(0, 2 - assigned);
        }

        private static string FormatTime(TimeOnly? time) => time.HasValue ? $"{time.Value.Hour}h{time.Value.Minute:00}" : string.Empty;

        private static string GetSlotNumber(ExamScheduleDto schedule)
        {
            var raw = schedule.SlotName ?? string.Empty;
            if (raw.Contains("Ca 1", StringComparison.OrdinalIgnoreCase)) return "1";
            if (raw.Contains("Ca 2", StringComparison.OrdinalIgnoreCase)) return "2";
            return raw.Replace("Ca", string.Empty, StringComparison.OrdinalIgnoreCase).Split('(')[0].Trim();
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var normalized = string.Join("-", (value ?? string.Empty).Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return normalized.Replace(" ", "-");
        }

        private static uint FindHeaderRowIndex(SheetData sheetData, WorkbookPart workbookPart)
        {
            foreach (var row in sheetData.Elements<Row>())
            {
                var headerTexts = row.Elements<Cell>().Select(x => NormalizeExportHeader(GetCellText(x, workbookPart))).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (headerTexts.Contains("stt") && (headerTexts.Contains("mahp") || headerTexts.Contains("mahocphan") || headerTexts.Contains("mamon")))
                    return row.RowIndex?.Value ?? 7;
            }
            return 7;
        }

        private static List<ExportHeaderColumn> GetHeaderColumns(SheetData sheetData, WorkbookPart workbookPart, uint headerRowIndex)
        {
            var headerRow = sheetData.Elements<Row>().FirstOrDefault(x => x.RowIndex?.Value == headerRowIndex);
            if (headerRow == null) return [];

            var headerTextByColumn = headerRow.Elements<Cell>()
                .Select(cell => new { ColumnName = GetColumnName(cell.CellReference?.Value ?? string.Empty), HeaderText = GetCellText(cell, workbookPart).Trim() })
                .Where(x => !string.IsNullOrWhiteSpace(x.ColumnName))
                .ToDictionary(x => x.ColumnName, x => x.HeaderText, StringComparer.OrdinalIgnoreCase);

            if (headerTextByColumn.Count == 0) return [];

            var lastColumnIndex = headerTextByColumn.Keys.Max(GetExcelColumnIndex);
            var columns = new List<ExportHeaderColumn>();
            for (var i = 1; i <= lastColumnIndex; i++)
            {
                var columnName = GetExcelColumnName(i);
                headerTextByColumn.TryGetValue(columnName, out var headerText);
                columns.Add(new ExportHeaderColumn(columnName, headerText ?? string.Empty));
            }
            return columns;
        }

        private static int GetExcelColumnIndex(string columnName)
        {
            var index = 0;
            foreach (var ch in (columnName ?? string.Empty).ToUpperInvariant())
            {
                if (ch < 'A' || ch > 'Z') continue;
                index = index * 26 + (ch - 'A' + 1);
            }
            return index;
        }

        private static string GetExcelColumnName(int index)
        {
            var name = string.Empty;
            while (index > 0)
            {
                index--;
                name = (char)('A' + index % 26) + name;
                index /= 26;
            }
            return name;
        }

        private static string FormatLecturer(string? code, string? name)
        {
            if (string.IsNullOrWhiteSpace(code)) return name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return code;
            return $"{code} - {name}";
        }

        private static string FormatExamFormat(ExamScheduleDto item)
        {
            if (string.IsNullOrWhiteSpace(item.ExamFormatCode)) return item.ExamFormatName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(item.ExamFormatName)) return item.ExamFormatCode;
            return $"{item.ExamFormatCode} - {item.ExamFormatName}";
        }

        private static void ApplyDataBorder(Row row, IReadOnlyList<ExportHeaderColumn> headerColumns, uint rowIndex, WorkbookPart workbookPart, Dictionary<uint, uint> borderedStyleIndexes)
        {
            foreach (var header in headerColumns)
            {
                var cell = GetOrCreateCell(row, header.ColumnName, rowIndex);
                var baseStyleIndex = cell.StyleIndex?.Value ?? 0;
                cell.StyleIndex = GetOrCreateBorderedStyleIndex(workbookPart, baseStyleIndex, borderedStyleIndexes);
            }
        }

        private static Cell GetOrCreateCell(Row row, string columnName, uint rowIndex)
        {
            var cellReference = columnName + rowIndex;
            var cell = row.Elements<Cell>().FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
            if (cell != null) return cell;
            cell = new Cell { CellReference = cellReference };
            var nextCell = row.Elements<Cell>().FirstOrDefault(c => string.Compare(GetColumnName(c.CellReference?.Value ?? string.Empty), columnName, StringComparison.OrdinalIgnoreCase) > 0);
            if (nextCell == null) row.Append(cell); else row.InsertBefore(cell, nextCell);
            return cell;
        }

        private static uint GetOrCreateBorderedStyleIndex(WorkbookPart workbookPart, uint baseStyleIndex, Dictionary<uint, uint> borderedStyleIndexes)
        {
            if (borderedStyleIndexes.TryGetValue(baseStyleIndex, out var styleIndex)) return styleIndex;
            var stylesPart = workbookPart.WorkbookStylesPart ?? workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet ??= new Stylesheet();
            var stylesheet = stylesPart.Stylesheet;
            stylesheet.Fonts ??= new Fonts(new Font()) { Count = 1 };
            stylesheet.Fills ??= new Fills(new Fill()) { Count = 1 };
            stylesheet.Borders ??= new Borders(new Border()) { Count = 1 };
            stylesheet.CellFormats ??= new CellFormats(new CellFormat()) { Count = 1 };

            var border = new Border(
                new LeftBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FF000000" } },
                new RightBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FF000000" } },
                new TopBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FF000000" } },
                new BottomBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FF000000" } },
                new DiagonalBorder());
            stylesheet.Borders.Append(border);
            stylesheet.Borders.Count = (uint)stylesheet.Borders.Count();
            var borderId = stylesheet.Borders.Count!.Value - 1;
            var baseCellFormat = stylesheet.CellFormats.Elements<CellFormat>().ElementAtOrDefault((int)baseStyleIndex);
            var borderedCellFormat = baseCellFormat != null ? (CellFormat)baseCellFormat.CloneNode(true) : new CellFormat();
            borderedCellFormat.BorderId = borderId;
            borderedCellFormat.ApplyBorder = true;
            stylesheet.CellFormats.Append(borderedCellFormat);
            stylesheet.CellFormats.Count = (uint)stylesheet.CellFormats.Count();
            styleIndex = stylesheet.CellFormats.Count!.Value - 1;
            borderedStyleIndexes[baseStyleIndex] = styleIndex;
            stylesheet.Save();
            return styleIndex;
        }

        private static string GetCellText(Cell? cell, WorkbookPart workbookPart)
        {
            if (cell == null) return string.Empty;
            if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(cell.InnerText, out var sharedStringIndex))
            {
                return workbookPart.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(sharedStringIndex)?.InnerText ?? string.Empty;
            }
            return cell.InnerText ?? string.Empty;
        }

        private static void SetCellValue(WorksheetPart worksheetPart, WorkbookPart workbookPart, string cellReference, string? value)
        {
            var rowIndex = uint.Parse(new string(cellReference.Where(char.IsDigit).ToArray()));
            var columnName = new string(cellReference.Where(char.IsLetter).ToArray());
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
            if (row == null)
            {
                row = new Row { RowIndex = rowIndex };
                sheetData.Append(row);
            }
            SetRowCell(row, columnName, rowIndex, value);
        }

        private static void SetRowCell(Row row, string columnName, uint rowIndex, string? value)
        {
            var cellReference = columnName + rowIndex;
            var cell = row.Elements<Cell>().FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
            if (cell == null)
            {
                cell = new Cell { CellReference = cellReference };
                row.Append(cell);
            }
            cell.CellValue = null;
            cell.InlineString = new InlineString(new Text(value ?? string.Empty));
            cell.DataType = CellValues.InlineString;
        }

        private static void ShiftRow(Row row, uint offset)
        {
            if (offset == 0) return;
            var currentIndex = row.RowIndex?.Value ?? 0;
            if (currentIndex == 0) return;
            var newIndex = currentIndex + offset;
            row.RowIndex = newIndex;
            foreach (var cell in row.Elements<Cell>())
            {
                var columnName = GetColumnName(cell.CellReference?.Value ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(columnName)) cell.CellReference = columnName + newIndex;
            }
        }

        private static string GetColumnName(string cellReference) => new((cellReference ?? string.Empty).Where(char.IsLetter).ToArray());

        private sealed record ExportHeaderColumn(string ColumnName, string HeaderText);
    }
}
