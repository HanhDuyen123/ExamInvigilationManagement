using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExamInvigilationManagement.Application.DTOs.Statistics;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExamInvigilationManagement.Application.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IStatisticsRepository _repository;
        private readonly ICurrentAcademicContextService _currentAcademicContextService;
        private static readonly string[] ChartColors = ["4F46E5", "16A34A", "F59E0B", "EF4444", "06B6D4", "8B5CF6"];
        private static readonly string[] ResponseColors = ["16A34A", "EF4444", "94A3B8"];

        public StatisticsService(IStatisticsRepository repository, ICurrentAcademicContextService currentAcademicContextService)
        {
            _repository = repository;
            _currentAcademicContextService = currentAcademicContextService;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<StatisticsDashboardDto> GetDashboardAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken = default)
        {
            ValidateFilter(filter);
            await ApplyDefaultAcademicContextAsync(userId, roleName, filter, cancellationToken);
            return await _repository.GetDashboardAsync(userId, roleName, filter, cancellationToken);
        }

        private async Task ApplyDefaultAcademicContextAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken)
        {
            if (filter.HasAcademicContext)
                return;

            var context = await _currentAcademicContextService.GetCurrentContextAsync(userId, roleName, filter.FacultyId, cancellationToken);
            if (context is null)
                return;

            filter.AcademyYearId = context.AcademyYearId;
            filter.SemesterId = context.SemesterId;
            filter.PeriodId = context.PeriodId;
        }

        public byte[] ExportExcel(StatisticsDashboardDto dashboard, string? templatePath = null)
        {
            using var stream = new MemoryStream();
            if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath))
            {
                var bytes = File.ReadAllBytes(templatePath);
                stream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Báo cáo" });
                    workbookPart.Workbook.Save();
                }
            }

            stream.Position = 0;
            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Template Excel không hợp lệ.");
                var worksheetPart = workbookPart.WorksheetParts.FirstOrDefault() ?? workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());
                sheetData.RemoveAllChildren<Row>();

                var stylesPart = workbookPart.WorkbookStylesPart ?? workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildStylesheet();
                stylesPart.Stylesheet.Save();

                EnsureSingleReportSheet(workbookPart, worksheetPart);
                ApplyExcelColumns(worksheetPart.Worksheet);
                BuildExcelReport(sheetData, dashboard);
                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        public byte[] ExportCsv(StatisticsDashboardDto dashboard)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Csv(DocumentLetterhead.Ministry, DocumentLetterhead.Nation));
            sb.AppendLine(Csv(DocumentLetterhead.School, DocumentLetterhead.Motto));
            sb.AppendLine();
            sb.AppendLine("BÁO CÁO THỐNG KÊ COI THI");
            sb.AppendLine(Csv("Phạm vi", dashboard.ScopeName));
            sb.AppendLine(Csv("Vai trò", dashboard.RoleName));
            sb.AppendLine(Csv("Ngày xuất", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
            sb.AppendLine();

            sb.AppendLine("TỔNG QUAN");
            sb.AppendLine(Csv("Chỉ tiêu", "Giá trị", "Ghi chú"));
            foreach (var metric in dashboard.Metrics)
                sb.AppendLine(Csv(metric.Label, metric.Value, metric.Hint));

            AppendChartCsv(sb, "TRẠNG THÁI LỊCH THI", dashboard.ScheduleStatus);
            AppendChartCsv(sb, "PHẢN HỒI GIẢNG VIÊN", dashboard.ResponseStatus);
            AppendChartCsv(sb, "LỊCH THI THEO ĐỢT", dashboard.SchedulesByPeriod);
            AppendChartCsv(sb, "TỪ CHỐI THEO BUỔI/CA", dashboard.RejectionsBySession);

            sb.AppendLine();
            sb.AppendLine("HIỆU SUẤT GIẢNG VIÊN");
            sb.AppendLine(Csv("Giảng viên", "Phân công", "Xác nhận", "Từ chối", "Chưa phản hồi", "Tỷ lệ xác nhận"));
            foreach (var item in dashboard.LecturerWorkloads)
                sb.AppendLine(Csv(item.LecturerName, item.AssignedCount.ToString(CultureInfo.InvariantCulture), item.ConfirmedCount.ToString(CultureInfo.InvariantCulture), item.RejectedCount.ToString(CultureInfo.InvariantCulture), item.PendingCount.ToString(CultureInfo.InvariantCulture), item.ConfirmationRate + "%"));

            sb.AppendLine();
            sb.AppendLine("ĐỘ PHỦ GIÁM THỊ THEO CA");
            sb.AppendLine(Csv("Đợt thi", "Buổi", "Ca", "Số lịch", "Đủ 2 giám thị", "Tỷ lệ phủ"));
            foreach (var item in dashboard.SlotCoverage)
                sb.AppendLine(Csv(item.PeriodName, item.SessionName, item.SlotName, item.ScheduleCount.ToString(CultureInfo.InvariantCulture), item.FullCoveredCount.ToString(CultureInfo.InvariantCulture), item.CoverageRate + "%"));

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public byte[] ExportPdf(StatisticsDashboardDto dashboard)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(26);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(PdfColor("172033")));

                    page.Header().Element(c => ComposePdfHeader(c, dashboard));
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Element(c => ComposeMetricGrid(c, dashboard.Metrics));
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeDonutLikePanel(c, "Cơ cấu trạng thái lịch thi", dashboard.ScheduleStatus, "lịch", ChartColors));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => ComposeDonutLikePanel(c, "Cơ cấu phản hồi giảng viên", dashboard.ResponseStatus, "phản hồi", ResponseColors));
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => ComposeHorizontalBarPanel(c, "Số lịch thi theo đợt", dashboard.SchedulesByPeriod, "lịch", "4F46E5"));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => ComposeHorizontalBarPanel(c, "Từ chối theo buổi/ca", dashboard.RejectionsBySession, "lần", "EF4444"));
                        });
                        column.Item().Element(c => ComposeWorkloadTable(c, dashboard.LecturerWorkloads));
                        column.Item().Element(c => ComposeCoverageTable(c, dashboard.SlotCoverage));
                    });
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Trang ").FontSize(8).FontColor(PdfColor("64748B"));
                        text.CurrentPageNumber().FontSize(8).FontColor(PdfColor("64748B"));
                        text.Span(" / ").FontSize(8).FontColor(PdfColor("64748B"));
                        text.TotalPages().FontSize(8).FontColor(PdfColor("64748B"));
                    });
                });
            }).GeneratePdf();
        }

        private static void BuildExcelReport(SheetData sheetData, StatisticsDashboardDto dashboard)
        {
            AddRow(sheetData, 1, [DocumentLetterhead.Ministry, string.Empty, string.Empty, string.Empty, DocumentLetterhead.Nation], 1);
            AddRow(sheetData, 2, [DocumentLetterhead.School, string.Empty, string.Empty, string.Empty, DocumentLetterhead.Motto], 1);
            AddRow(sheetData, 4, ["BÁO CÁO THỐNG KÊ COI THI"], 2);
            AddRow(sheetData, 5, [$"Phạm vi: {dashboard.ScopeName}", $"Vai trò: {dashboard.RoleName}", $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}"], 0);

            var row = 7u;
            AddRow(sheetData, row++, ["TỔNG QUAN"], 3);
            AddRow(sheetData, row++, ["Chỉ tiêu", "Giá trị", "Ghi chú"], 4);
            foreach (var metric in dashboard.Metrics)
                AddRow(sheetData, row++, [metric.Label, metric.Value, metric.Hint], 0);

            row += 1;
            row = AddExcelSummaryTable(sheetData, row, "CƠ CẤU TRẠNG THÁI LỊCH THI", dashboard.ScheduleStatus);
            row = AddExcelSummaryTable(sheetData, row + 1, "CƠ CẤU PHẢN HỒI GIẢNG VIÊN", dashboard.ResponseStatus);
            row = AddExcelSummaryTable(sheetData, row + 1, "SỐ LỊCH THI THEO ĐỢT", dashboard.SchedulesByPeriod);
            row = AddExcelSummaryTable(sheetData, row + 1, "TỪ CHỐI THEO BUỔI/CA", dashboard.RejectionsBySession);

            row += 1;
            AddRow(sheetData, row++, [$"HIỆU SUẤT COI THI CỦA GIẢNG VIÊN ({dashboard.LecturerWorkloads.Count:N0} NGƯỜI)"], 3);
            AddRow(sheetData, row++, ["Giảng viên", "Phân công", "Xác nhận", "Từ chối", "Chưa phản hồi", "Tỷ lệ xác nhận"], 4);
            foreach (var item in dashboard.LecturerWorkloads)
            {
                AddRow(sheetData, row++, [item.LecturerName, item.AssignedCount.ToString(), item.ConfirmedCount.ToString(), item.RejectedCount.ToString(), item.PendingCount.ToString(), item.ConfirmationRate + "%"], 0);
            }

            row += 1;
            AddRow(sheetData, row++, [$"ĐỘ PHỦ GIÁM THỊ THEO CA THI ({dashboard.SlotCoverage.Count:N0} DÒNG)"], 3);
            AddRow(sheetData, row++, ["Đợt thi", "Buổi", "Ca", "Số lịch", "Đủ 2 giám thị", "Tỷ lệ phủ"], 4);
            foreach (var item in dashboard.SlotCoverage)
            {
                AddRow(sheetData, row++, [item.PeriodName, item.SessionName, item.SlotName, item.ScheduleCount.ToString(), item.FullCoveredCount.ToString(), item.CoverageRate + "%"], 0);
            }
        }

        private static uint AddExcelSummaryTable(SheetData sheetData, uint startRow, string title, IReadOnlyList<StatisticChartPointDto> data)
        {
            AddRow(sheetData, startRow++, [title], 3);
            AddRow(sheetData, startRow++, ["Nhãn", "Số lượng", "Tỷ lệ"], 4);
            if (!data.Any())
            {
                AddRow(sheetData, startRow++, ["Không có dữ liệu"], 0);
                return startRow;
            }

            for (var i = 0; i < data.Count; i++)
            {
                var item = data[i];
                AddRow(sheetData, startRow++, [item.Label, item.Value.ToString(), item.Rate + "%"], 0);
            }

            return startRow;
        }

        private static void ComposePdfHeader(IContainer container, StatisticsDashboardDto dashboard)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(DocumentLetterhead.Ministry).SemiBold().FontSize(9);
                        left.Item().Text(DocumentLetterhead.School).Bold().FontSize(9);
                    });
                    row.RelativeItem().AlignRight().Column(right =>
                    {
                        right.Item().Text(DocumentLetterhead.Nation).Bold().FontSize(9);
                        right.Item().Text(DocumentLetterhead.Motto).Italic().FontSize(9);
                    });
                });
                column.Item().PaddingTop(12).AlignCenter().Text("BÁO CÁO THỐNG KÊ COI THI").Bold().FontSize(16).FontColor(PdfColor("172033"));
                column.Item().PaddingTop(4).AlignCenter().Text($"Phạm vi: {dashboard.ScopeName} | Vai trò: {dashboard.RoleName} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(PdfColor("64748B"));
                column.Item().PaddingTop(8).LineHorizontal(1).LineColor(PdfColor("CBD5E1"));
            });
        }

        private static void ComposeMetricGrid(IContainer container, IReadOnlyList<StatisticMetricDto> metrics)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                foreach (var metric in metrics.Take(8))
                {
                    table.Cell().Padding(3).Border(1).BorderColor(PdfColor("E2E8F0")).Background(PdfColor("F8FAFC")).Padding(8).Column(col =>
                    {
                        col.Item().Text(metric.Label).FontSize(8).FontColor(PdfColor("64748B"));
                        col.Item().Text(metric.Value).Bold().FontSize(14).FontColor(MetricColor(metric.Tone));
                        col.Item().Text(metric.Hint).FontSize(7).FontColor(PdfColor("64748B"));
                    });
                }
            });
        }

        private static void ComposeDonutLikePanel(IContainer container, string title, IReadOnlyList<StatisticChartPointDto> data, string unit, IReadOnlyList<string> colors)
        {
            var total = Math.Max(0, data.Sum(x => x.Value));
            container.Border(1).BorderColor(PdfColor("E2E8F0")).Background(PdfColor("FFFFFF")).Padding(10).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(title).Bold().FontSize(10);
                    row.ConstantItem(70).AlignRight().Text($"{total:N0} {unit}").FontSize(8).FontColor(PdfColor("64748B"));
                });
                column.Item().PaddingVertical(8).AlignCenter().Text(total.ToString("N0")).Bold().FontSize(24).FontColor(PdfColor("172033"));
                foreach (var item in data.Take(7).Select((value, index) => new { value, index }))
                    column.Item().Element(c => ComposeLegendBar(c, item.value.Label, item.value.Value, item.value.Rate, total, colors[item.index % colors.Count]));
                if (!data.Any()) column.Item().AlignCenter().Text("Không có dữ liệu").FontColor(PdfColor("94A3B8"));
            });
        }

        private static void ComposeHorizontalBarPanel(IContainer container, string title, IReadOnlyList<StatisticChartPointDto> data, string unit, string color)
        {
            var max = Math.Max(1, data.Any() ? data.Max(x => x.Value) : 1);
            container.Border(1).BorderColor(PdfColor("E2E8F0")).Background(PdfColor("FFFFFF")).Padding(10).Column(column =>
            {
                column.Item().Text(title).Bold().FontSize(10);
                column.Item().PaddingTop(8).Column(items =>
                {
                    foreach (var item in data.Take(8))
                        items.Item().Element(c => ComposeValueBar(c, item.Label, item.Value, max, unit, color));
                    if (!data.Any()) items.Item().AlignCenter().Text("Không có dữ liệu").FontColor(PdfColor("94A3B8"));
                });
            });
        }

        private static void ComposeLegendBar(IContainer container, string label, int value, decimal rate, int total, string color)
        {
            var percent = total <= 0 ? 0 : Math.Clamp((float)value / total, 0, 1);
            var filled = Math.Max(0.001f, percent);
            var empty = Math.Max(0.001f, 1 - percent);
            container.PaddingBottom(5).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(8).FontColor(PdfColor("334155"));
                    row.ConstantItem(80).AlignRight().Text($"{value:N0} ({rate}%)").FontSize(8).SemiBold();
                });
                column.Item().Height(6).Background(PdfColor("E2E8F0")).Row(row =>
                {
                    row.RelativeItem(filled).Background(PdfColor(color));
                    row.RelativeItem(empty);
                });
            });
        }

        private static void ComposeValueBar(IContainer container, string label, int value, int max, string unit, string color)
        {
            var percent = Math.Clamp((float)value / max, 0, 1);
            var filled = Math.Max(0.001f, percent);
            var empty = Math.Max(0.001f, 1 - percent);
            container.PaddingBottom(6).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(8).FontColor(PdfColor("334155"));
                    row.ConstantItem(58).AlignRight().Text($"{value:N0} {unit}").FontSize(8).SemiBold();
                });
                column.Item().Height(7).Background(PdfColor("E2E8F0")).Row(row =>
                {
                    row.RelativeItem(filled).Background(PdfColor(color));
                    row.RelativeItem(empty);
                });
            });
        }

        private static void ComposeWorkloadTable(IContainer container, IReadOnlyList<LecturerWorkloadStatisticDto> data)
        {
            ComposeTable(container, $"Hiệu suất coi thi của giảng viên ({data.Count:N0} người)", ["Giảng viên", "PC", "XN", "TC", "Chờ", "Tỷ lệ"], data.Select(x => new[] { x.LecturerName, x.AssignedCount.ToString(), x.ConfirmedCount.ToString(), x.RejectedCount.ToString(), x.PendingCount.ToString(), x.ConfirmationRate + "%" }));
        }

        private static void ComposeCoverageTable(IContainer container, IReadOnlyList<SlotCoverageStatisticDto> data)
        {
            ComposeTable(container, $"Độ phủ giám thị theo ca thi ({data.Count:N0} dòng)", ["Đợt", "Buổi", "Ca", "Lịch", "Đủ GT", "Tỷ lệ"], data.Select(x => new[] { x.PeriodName, x.SessionName, x.SlotName, x.ScheduleCount.ToString(), x.FullCoveredCount.ToString(), x.CoverageRate + "%" }));
        }

        private static void ComposeTable(IContainer container, string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            container.Border(1).BorderColor(PdfColor("E2E8F0")).Background(PdfColor("FFFFFF")).Padding(10).Column(column =>
            {
                column.Item().Text(title).Bold().FontSize(10);
                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.1f);
                        for (var i = 1; i < headers.Count; i++) columns.RelativeColumn();
                    });
                    foreach (var header in headers)
                        table.Cell().Background(PdfColor("EEF2FF")).Padding(4).Text(header).Bold().FontSize(7);
                    foreach (var row in rows)
                    {
                        foreach (var value in row)
                            table.Cell().BorderBottom(1).BorderColor(PdfColor("F1F5F9")).Padding(4).Text(value).FontSize(7);
                    }
                });
            });
        }

        private static void EnsureSingleReportSheet(WorkbookPart workbookPart, WorksheetPart worksheetPart)
        {
            var sheets = workbookPart.Workbook.Sheets ?? workbookPart.Workbook.AppendChild(new Sheets());
            var sheet = sheets.Elements<Sheet>().FirstOrDefault(x => x.Name == "Báo cáo") ?? sheets.Elements<Sheet>().FirstOrDefault();
            if (sheet == null)
            {
                sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Báo cáo" });
                return;
            }

            sheet.Id = workbookPart.GetIdOfPart(worksheetPart);
            sheet.Name = "Báo cáo";
        }

        private static void ApplyExcelColumns(Worksheet worksheet)
        {
            worksheet.RemoveAllChildren<Columns>();
            var columns = new Columns(
                new Column { Min = 1, Max = 1, Width = 28, CustomWidth = true },
                new Column { Min = 2, Max = 2, Width = 14, CustomWidth = true },
                new Column { Min = 3, Max = 3, Width = 14, CustomWidth = true },
                new Column { Min = 4, Max = 4, Width = 16, CustomWidth = true },
                new Column { Min = 5, Max = 5, Width = 18, CustomWidth = true },
                new Column { Min = 6, Max = 6, Width = 16, CustomWidth = true },
                new Column { Min = 7, Max = 10, Width = 18, CustomWidth = true });
            var sheetData = worksheet.GetFirstChild<SheetData>();
            if (sheetData == null) worksheet.Append(columns);
            else worksheet.InsertBefore(columns, sheetData);
        }

        private static void AddRow(SheetData sheetData, uint rowIndex, IReadOnlyList<string> values, uint styleIndex)
        {
            var row = new Row { RowIndex = rowIndex };
            for (var i = 0; i < values.Count; i++)
            {
                row.Append(new Cell
                {
                    CellReference = ColumnName(i + 1) + rowIndex,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(values[i] ?? string.Empty)),
                    StyleIndex = styleIndex
                });
            }
            sheetData.Append(row);
        }

        private static Stylesheet BuildStylesheet()
        {
            return new Stylesheet(
                new DocumentFormat.OpenXml.Spreadsheet.Fonts(
                    new Font(new FontName { Val = "Arial" }, new FontSize { Val = 10 }),
                    new Font(new Bold(), new FontName { Val = "Arial" }, new FontSize { Val = 10 }),
                    new Font(new Bold(), new FontName { Val = "Arial" }, new FontSize { Val = 16 }),
                    new Font(new Bold(), new FontName { Val = "Arial" }, new FontSize { Val = 11 }, new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FFFFFF" })
                ),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                    new Fill(new PatternFill(new ForegroundColor { Rgb = "4F46E5" }) { PatternType = PatternValues.Solid }),
                    new Fill(new PatternFill(new ForegroundColor { Rgb = "EEF2FF" }) { PatternType = PatternValues.Solid })
                ),
                new Borders(new Border(), new Border(new BottomBorder { Style = BorderStyleValues.Thin, Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "CBD5E1" } })),
                new CellFormats(
                    new CellFormat(),
                    new CellFormat { FontId = 1, ApplyFont = true, Alignment = new Alignment { WrapText = true, Vertical = VerticalAlignmentValues.Center } },
                    new CellFormat { FontId = 2, ApplyFont = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center } },
                    new CellFormat { FontId = 3, FillId = 2, ApplyFont = true, ApplyFill = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center } },
                    new CellFormat { FontId = 1, FillId = 3, BorderId = 1, ApplyFont = true, ApplyFill = true, ApplyBorder = true, Alignment = new Alignment { WrapText = true, Vertical = VerticalAlignmentValues.Center } }
                ));
        }

        private static string PdfColor(string color)
        {
            return color.StartsWith('#') ? color : "#" + color;
        }

        private static void ValidateFilter(StatisticsFilterDto filter)
        {
            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
                throw new InvalidOperationException("Khoảng ngày thống kê không hợp lệ.");
        }

        private static void AppendChartCsv(StringBuilder sb, string title, IReadOnlyList<StatisticChartPointDto> data)
        {
            sb.AppendLine();
            sb.AppendLine(title);
            sb.AppendLine(Csv("Nhãn", "Số lượng", "Tỷ lệ"));
            foreach (var item in data)
                sb.AppendLine(Csv(item.Label, item.Value.ToString(CultureInfo.InvariantCulture), item.Rate + "%"));
        }

        private static string Csv(params string?[] values)
        {
            return string.Join(',', values.Select(x => "\"" + (x ?? string.Empty).Replace("\"", "\"\"") + "\""));
        }

        private static string MetricColor(string? tone)
        {
            return tone switch
            {
                "success" => PdfColor("15803D"),
                "info" => PdfColor("0369A1"),
                "warning" => PdfColor("B45309"),
                _ => PdfColor("4F46E5")
            };
        }

        private static string ColumnName(int number)
        {
            var name = string.Empty;
            while (number > 0)
            {
                var mod = (number - 1) % 26;
                name = (char)('A' + mod) + name;
                number = (number - mod) / 26;
            }
            return name;
        }
    }
}
