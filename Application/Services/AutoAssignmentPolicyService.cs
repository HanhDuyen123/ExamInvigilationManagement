using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Application.Services
{
    public class AutoAssignmentPolicyService : IAutoAssignmentPolicyService
    {
        private readonly IAutoAssignmentPolicyRepository _repository;

        public AutoAssignmentPolicyService(IAutoAssignmentPolicyRepository repository)
        {
            _repository = repository;
        }

        public async Task<AutoAssignmentPolicyEditDto> GetDefaultPolicyAsync(
            int actorUserId,
            CancellationToken cancellationToken = default)
        {
            var facultyId = await ResolveFacultyIdAsync(actorUserId, cancellationToken);
            return await _repository.GetOrCreateDefaultPolicyAsync(facultyId, actorUserId, cancellationToken);
        }

        public async Task UpdateDefaultPolicyAsync(
            AutoAssignmentPolicyEditDto dto,
            int actorUserId,
            CancellationToken cancellationToken = default)
        {
            var facultyId = await ResolveFacultyIdAsync(actorUserId, cancellationToken);
            dto.FacultyId = facultyId;
            Normalize(dto);
            Validate(dto);
            await _repository.UpdateDefaultPolicyAsync(dto, actorUserId, cancellationToken);
        }

        private async Task<int> ResolveFacultyIdAsync(int actorUserId, CancellationToken cancellationToken)
        {
            if (actorUserId <= 0)
                throw new ArgumentException("Không xác định được người dùng hiện tại.");

            var facultyId = await _repository.GetUserFacultyIdAsync(actorUserId, cancellationToken);
            if (!facultyId.HasValue || facultyId.Value <= 0)
                throw new InvalidOperationException("Không xác định được khoa của tài khoản hiện tại.");

            return facultyId.Value;
        }

        private static void Normalize(AutoAssignmentPolicyEditDto dto)
        {
            dto.PolicyName = string.IsNullOrWhiteSpace(dto.PolicyName)
                ? "Chính sách phân công mặc định"
                : dto.PolicyName.Trim();
            dto.RequiredInvigilatorsPerSchedule = Math.Clamp(dto.RequiredInvigilatorsPerSchedule, 1, 10);
            dto.MaxAssignmentsPerSlot = Math.Clamp(dto.MaxAssignmentsPerSlot, 1, 3);
            dto.SolverTimeLimitSeconds = Math.Clamp(dto.SolverTimeLimitSeconds, 1, 60);
            dto.Rules = dto.Rules
                .Where(x => !string.IsNullOrWhiteSpace(x.RuleCode))
                .GroupBy(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var rule in dto.Rules.Where(x => x.IsRequired))
                rule.IsEnabled = true;

            foreach (var formatPolicy in dto.ExamFormatPolicies)
                formatPolicy.AssignmentMode = NormalizeAssignmentMode(formatPolicy.AssignmentMode);
        }

        private static string NormalizeAssignmentMode(string? value)
        {
            return value switch
            {
                AutoAssignmentExamFormatAssignmentModes.OwnerOnly => AutoAssignmentExamFormatAssignmentModes.OwnerOnly,
                AutoAssignmentExamFormatAssignmentModes.Skip => AutoAssignmentExamFormatAssignmentModes.Skip,
                _ => AutoAssignmentExamFormatAssignmentModes.Full
            };
        }

        private static void Validate(AutoAssignmentPolicyEditDto dto)
        {
            if (dto.MaxAssignmentsPerDay.HasValue && dto.MaxAssignmentsPerDay.Value < 1)
                throw new ArgumentException("Số ca tối đa trong ngày không hợp lệ.");

            if (dto.MaxAssignmentsPerPeriod.HasValue && dto.MaxAssignmentsPerPeriod.Value < 1)
                throw new ArgumentException("Số ca tối đa trong đợt không hợp lệ.");

            if (dto.MaxAssignmentsPerSlot != 1)
                throw new ArgumentException("Để đảm bảo nghiệp vụ coi thi, mỗi giảng viên hiện chỉ được nhận tối đa 1 lịch trong cùng một slot.");

            if (!dto.Rules.Any(x => x.IsEnabled))
                throw new ArgumentException("Cần bật ít nhất một tiêu chí ưu tiên để hệ thống có cơ sở tối ưu.");
        }
    }
}
