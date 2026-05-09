using System.Globalization;
using System.Text;
using ExamInvigilationManagement.Application.DTOs.Statistics;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IStatisticsRepository _repository;

        public StatisticsService(IStatisticsRepository repository)
        {
            _repository = repository;
        }

        public async Task<StatisticsDashboardDto> GetDashboardAsync(int userId, string roleName, StatisticsFilterDto filter, CancellationToken cancellationToken = default)
        {
            ValidateFilter(filter);
            return await _repository.GetDashboardAsync(userId, roleName, filter, cancellationToken);
        }

        public byte[] ExportCsv(StatisticsDashboardDto dashboard)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BAO CAO THONG KE COI THI");
            sb.AppendLine(Csv("Pham vi", dashboard.ScopeName));
            sb.AppendLine(Csv("Vai tro", dashboard.RoleName));
            sb.AppendLine(Csv("Ngay xuat", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
            sb.AppendLine();

            sb.AppendLine("TONG QUAN");
            sb.AppendLine(Csv("Chi tieu", "Gia tri", "Ghi chu"));
            foreach (var metric in dashboard.Metrics)
                sb.AppendLine(Csv(metric.Label, metric.Value, metric.Hint));

            sb.AppendLine();
            sb.AppendLine("TRANG THAI LICH THI");
            sb.AppendLine(Csv("Trang thai", "So luong", "Ty le"));
            foreach (var item in dashboard.ScheduleStatus)
                sb.AppendLine(Csv(item.Label, item.Value.ToString(CultureInfo.InvariantCulture), item.Rate + "%"));

            sb.AppendLine();
            sb.AppendLine("PHAN HOI GIANG VIEN");
            sb.AppendLine(Csv("Trang thai phan hoi", "So luong", "Ty le"));
            foreach (var item in dashboard.ResponseStatus)
                sb.AppendLine(Csv(item.Label, item.Value.ToString(CultureInfo.InvariantCulture), item.Rate + "%"));

            sb.AppendLine();
            sb.AppendLine("LICH THI THEO DOT");
            sb.AppendLine(Csv("Dot thi", "So lich", "Ty le"));
            foreach (var item in dashboard.SchedulesByPeriod)
                sb.AppendLine(Csv(item.Label, item.Value.ToString(CultureInfo.InvariantCulture), item.Rate + "%"));

            sb.AppendLine();
            sb.AppendLine("TU CHOI THEO BUOI/CA");
            sb.AppendLine(Csv("Buoi/Ca", "So lan tu choi", "Ty le"));
            foreach (var item in dashboard.RejectionsBySession)
                sb.AppendLine(Csv(item.Label, item.Value.ToString(CultureInfo.InvariantCulture), item.Rate + "%"));

            sb.AppendLine();
            sb.AppendLine("HIEU SUAT GIANG VIEN");
            sb.AppendLine(Csv("Giang vien", "Phan cong", "Xac nhan", "Tu choi", "Chua phan hoi", "Ty le xac nhan"));
            foreach (var item in dashboard.LecturerWorkloads)
                sb.AppendLine(Csv(item.LecturerName, item.AssignedCount.ToString(CultureInfo.InvariantCulture), item.ConfirmedCount.ToString(CultureInfo.InvariantCulture), item.RejectedCount.ToString(CultureInfo.InvariantCulture), item.PendingCount.ToString(CultureInfo.InvariantCulture), item.ConfirmationRate + "%"));

            sb.AppendLine();
            sb.AppendLine("DO PHU GIAM THI THEO CA");
            sb.AppendLine(Csv("Dot thi", "Buoi", "Ca", "So lich", "Du 2 giam thi", "Ty le phu"));
            foreach (var item in dashboard.SlotCoverage)
                sb.AppendLine(Csv(item.PeriodName, item.SessionName, item.SlotName, item.ScheduleCount.ToString(CultureInfo.InvariantCulture), item.FullCoveredCount.ToString(CultureInfo.InvariantCulture), item.CoverageRate + "%"));
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public byte[] ExportPdf(StatisticsDashboardDto dashboard)
        {
            var lines = new List<string>
            {
                "BAO CAO THONG KE COI THI",
                "Pham vi: " + ToAscii(dashboard.ScopeName),
                "Role: " + ToAscii(dashboard.RoleName),
                "Ngay xuat: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                new string('-', 92),
                ""
            };

            AddTable(lines, "TONG QUAN", new[] { "Chi tieu", "Gia tri", "Ghi chu" }, dashboard.Metrics.Select(x => new[] { ToAscii(x.Label), x.Value, ToAscii(x.Hint) }));
            AddTable(lines, "TRANG THAI LICH THI", new[] { "Trang thai", "So luong", "Ty le" }, dashboard.ScheduleStatus.Select(x => new[] { ToAscii(x.Label), x.Value.ToString(CultureInfo.InvariantCulture), x.Rate + "%" }));
            AddTable(lines, "PHAN HOI GIANG VIEN", new[] { "Trang thai", "So luong", "Ty le" }, dashboard.ResponseStatus.Select(x => new[] { ToAscii(x.Label), x.Value.ToString(CultureInfo.InvariantCulture), x.Rate + "%" }));
            AddTable(lines, "LICH THI THEO DOT", new[] { "Dot thi", "So lich", "Ty le" }, dashboard.SchedulesByPeriod.Select(x => new[] { ToAscii(x.Label), x.Value.ToString(CultureInfo.InvariantCulture), x.Rate + "%" }));
            AddTable(lines, "TU CHOI THEO BUOI/CA", new[] { "Buoi/Ca", "So lan", "Ty le" }, dashboard.RejectionsBySession.Select(x => new[] { ToAscii(x.Label), x.Value.ToString(CultureInfo.InvariantCulture), x.Rate + "%" }));
            AddTable(lines, "HIEU SUAT GIANG VIEN", new[] { "Giang vien", "PC", "XN", "TC", "Cho", "Ty le" }, dashboard.LecturerWorkloads.Select(x => new[] { ToAscii(x.LecturerName), x.AssignedCount.ToString(CultureInfo.InvariantCulture), x.ConfirmedCount.ToString(CultureInfo.InvariantCulture), x.RejectedCount.ToString(CultureInfo.InvariantCulture), x.PendingCount.ToString(CultureInfo.InvariantCulture), x.ConfirmationRate + "%" }));
            AddTable(lines, "DO PHU GIAM THI THEO CA", new[] { "Dot", "Buoi", "Ca", "Lich", "Du GT", "Ty le" }, dashboard.SlotCoverage.Select(x => new[] { ToAscii(x.PeriodName), ToAscii(x.SessionName), ToAscii(x.SlotName), x.ScheduleCount.ToString(CultureInfo.InvariantCulture), x.FullCoveredCount.ToString(CultureInfo.InvariantCulture), x.CoverageRate + "%" }));

            return BuildSimplePdf(lines);
        }

        private static void ValidateFilter(StatisticsFilterDto filter)
        {
            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
                throw new InvalidOperationException("Khoảng ngày thống kê không hợp lệ.");
        }

        private static string Csv(params string?[] values)
        {
            return string.Join(',', values.Select(x => "\"" + (x ?? string.Empty).Replace("\"", "\"\"") + "\""));
        }

        private static void AddTable(List<string> lines, string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            lines.Add("");
            lines.Add(title);
            lines.Add(new string('-', 92));
            lines.Add(FormatRow(headers));
            lines.Add(new string('-', 92));
            var list = rows.ToList();
            if (!list.Any())
            {
                lines.Add("Khong co du lieu");
                return;
            }

            foreach (var row in list.Take(18))
                lines.Add(FormatRow(row));
        }

        private static string FormatRow(IReadOnlyList<string> values)
        {
            var widths = values.Count switch
            {
                <= 3 => new[] { 42, 16, 28 },
                <= 6 => new[] { 28, 10, 10, 10, 10, 14 },
                _ => new[] { 28, 10, 10, 10, 10, 14 }
            };

            return string.Join(" | ", values.Select((value, index) => TrimCell(value, widths[Math.Min(index, widths.Length - 1)]).PadRight(widths[Math.Min(index, widths.Length - 1)])));
        }

        private static string TrimCell(string? value, int width)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
            return text.Length <= width ? text : text[..Math.Max(0, width - 1)] + "~";
        }

        private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
        {
            var content = new StringBuilder("BT\n/F1 9 Tf\n36 806 Td\n12 TL\n");
            foreach (var raw in lines.Take(64))
                content.Append('(').Append(EscapePdf(raw.Length > 95 ? raw[..95] : raw)).Append(") Tj\nT*\n");
            content.Append("ET");

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream"
            };

            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int> { 0 };
            foreach (var obj in objects.Select((value, index) => new { value, number = index + 1 }))
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.Append(obj.number).Append(" 0 obj\n").Append(obj.value).Append("\nendobj\n");
            }

            var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
                pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            pdf.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static string EscapePdf(string value)
        {
            return ToAscii(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static string ToAscii(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(c switch { 'đ' => 'd', 'Đ' => 'D', _ => c <= 127 ? c : ' ' });
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
