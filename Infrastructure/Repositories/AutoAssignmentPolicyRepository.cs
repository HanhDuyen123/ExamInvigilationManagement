using ExamInvigilationManagement.Application.DTOs.AutoAssign;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class AutoAssignmentPolicyRepository : IAutoAssignmentPolicyRepository
    {
        private readonly ApplicationDbContext _db;

        public AutoAssignmentPolicyRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int?> GetUserFacultyIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.FacultyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AutoAssignmentPolicyEditDto> GetOrCreateDefaultPolicyAsync(
            int facultyId,
            int actorUserId,
            CancellationToken cancellationToken = default)
        {
            var policy = await LoadDefaultPolicyAsync(facultyId, tracking: true, cancellationToken);
            if (policy == null)
            {
                policy = BuildDefaultPolicy(facultyId, actorUserId);
                await _db.AutoAssignmentPolicies.AddAsync(policy, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            await EnsureExamFormatRulesAsync(policy, cancellationToken);

            return ToEditDto(policy);
        }

        public async Task UpdateDefaultPolicyAsync(
            AutoAssignmentPolicyEditDto dto,
            int actorUserId,
            CancellationToken cancellationToken = default)
        {
            var policy = await LoadDefaultPolicyAsync(dto.FacultyId, tracking: true, cancellationToken);
            if (policy == null)
            {
                policy = BuildDefaultPolicy(dto.FacultyId, actorUserId);
                await _db.AutoAssignmentPolicies.AddAsync(policy, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            policy.PolicyName = dto.PolicyName;
            policy.RequiredInvigilatorsPerSchedule = (byte)dto.RequiredInvigilatorsPerSchedule;
            policy.AllowCrossFaculty = dto.AllowCrossFaculty;
            policy.RequirePeriodAvailabilityIfExists = dto.RequirePeriodAvailabilityIfExists;
            policy.AllowFacultyMemberAsFallback = dto.AllowFacultyMemberAsFallback;
            policy.MaxAssignmentsPerDay = dto.MaxAssignmentsPerDay;
            policy.MaxAssignmentsPerPeriod = dto.MaxAssignmentsPerPeriod;
            policy.MaxAssignmentsPerSlot = dto.MaxAssignmentsPerSlot;
            policy.SolverTimeLimitSeconds = dto.SolverTimeLimitSeconds;
            policy.UpdatedById = actorUserId;
            policy.UpdatedAt = DateTime.Now;

            await EnsureExamFormatRulesAsync(policy, cancellationToken);

            var incomingByCode = dto.Rules.ToDictionary(x => x.RuleCode, StringComparer.OrdinalIgnoreCase);
            foreach (var rule in policy.Rules)
            {
                if (!incomingByCode.TryGetValue(rule.RuleCode, out var incoming))
                    continue;

                rule.IsEnabled = incoming.IsEnabled;
                rule.Weight = incoming.Weight;
                rule.PriorityOrder = incoming.PriorityOrder;
            }

            var incomingFormatById = dto.ExamFormatPolicies.ToDictionary(x => x.ExamFormatId);
            foreach (var formatRule in policy.ExamFormatRules)
            {
                if (!incomingFormatById.TryGetValue(formatRule.ExamFormatId, out var incoming))
                    continue;

                formatRule.AssignmentMode = NormalizeAssignmentMode(incoming.AssignmentMode);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private Task<AutoAssignmentPolicy?> LoadDefaultPolicyAsync(
            int facultyId,
            bool tracking,
            CancellationToken cancellationToken)
        {
            var query = _db.AutoAssignmentPolicies
                .Include(x => x.Rules)
                .Include(x => x.ExamFormatRules)
                    .ThenInclude(x => x.ExamFormat)
                .Where(x =>
                    x.FacultyId == facultyId &&
                    x.IsActive &&
                    x.IsDefault &&
                    x.SemesterId == null &&
                    x.PeriodId == null);

            if (!tracking)
                query = query.AsNoTracking();

            return query
                .OrderByDescending(x => x.PolicyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static AutoAssignmentPolicy BuildDefaultPolicy(int facultyId, int actorUserId)
        {
            var policy = new AutoAssignmentPolicy
            {
                FacultyId = facultyId,
                PolicyName = "Chính sách phân công mặc định",
                IsDefault = true,
                IsActive = true,
                RequiredInvigilatorsPerSchedule = 2,
                AllowCrossFaculty = false,
                RequirePeriodAvailabilityIfExists = true,
                AllowFacultyMemberAsFallback = true,
                MaxAssignmentsPerSlot = 1,
                SolverTimeLimitSeconds = 8,
                CreatedById = actorUserId,
                CreatedAt = DateTime.Now
            };

            foreach (var rule in AutoAssignmentPolicyDefaults.BuildDefaultRules().Values.OrderBy(x => x.PriorityOrder))
            {
                policy.Rules.Add(new AutoAssignmentRule
                {
                    RuleCode = rule.RuleCode,
                    RuleName = rule.RuleName,
                    RuleType = rule.RuleType,
                    IsEnabled = rule.IsEnabled,
                    IsRequired = rule.IsRequired,
                    PriorityOrder = rule.PriorityOrder,
                    Weight = rule.Weight,
                    ParametersJson = rule.ParametersJson
                });
            }

            return policy;
        }

        private async Task EnsureExamFormatRulesAsync(AutoAssignmentPolicy policy, CancellationToken cancellationToken)
        {
            var formats = await _db.ExamFormats
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new { x.ExamFormatId, x.Code })
                .ToListAsync(cancellationToken);

            var existingIds = policy.ExamFormatRules.Select(x => x.ExamFormatId).ToHashSet();
            foreach (var format in formats.Where(x => !existingIds.Contains(x.ExamFormatId)))
            {
                policy.ExamFormatRules.Add(new AutoAssignmentExamFormatRule
                {
                    ExamFormatId = format.ExamFormatId,
                    PriorityGroup = ResolvePriorityGroup(format.Code),
                    AssignmentMode = AutoAssignmentExamFormatAssignmentModes.Full,
                    SpecialistWeight = 0,
                    ExactOwnerWeight = 0,
                    SameSubjectWeight = 0
                });
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(cancellationToken);
        }

        private static string ResolvePriorityGroup(string? code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "VD" or "BTL-VD" or "TL-VD" or "NTL-VD" => "Oral",
                "PM" or "DA" or "TH" or "PTH" => "Practical",
                _ => "Standard"
            };
        }

        private static AutoAssignmentPolicyEditDto ToEditDto(AutoAssignmentPolicy policy)
        {
            var defaults = AutoAssignmentPolicyDefaults.BuildDefaultRules();
            var rules = defaults.Values
                .OrderBy(x => x.PriorityOrder)
                .Select(defaultRule =>
                {
                    var storedRule = policy.Rules.FirstOrDefault(x => string.Equals(x.RuleCode, defaultRule.RuleCode, StringComparison.OrdinalIgnoreCase));
                    var source = storedRule == null
                        ? defaultRule
                        : new AutoAssignmentRuleDto
                        {
                            RuleCode = storedRule.RuleCode,
                            RuleName = defaultRule.RuleName,
                            RuleType = defaultRule.RuleType,
                            IsEnabled = storedRule.IsEnabled,
                            IsRequired = defaultRule.IsRequired,
                            PriorityOrder = storedRule.PriorityOrder,
                            Weight = storedRule.Weight,
                            ParametersJson = storedRule.ParametersJson
                        };

                    return new AutoAssignmentRuleEditDto
                    {
                        RuleCode = source.RuleCode,
                        RuleName = source.RuleName,
                        RuleType = source.RuleType,
                        IsEnabled = source.IsEnabled,
                        IsRequired = source.IsRequired,
                        PriorityOrder = source.PriorityOrder,
                        Weight = source.Weight,
                        Description = DescribeRule(source.RuleCode)
                    };
                })
                .ToList();

            var formatPolicies = policy.ExamFormatRules
                .Where(x => x.ExamFormat.IsActive)
                .OrderBy(x => x.ExamFormat.Code)
                .Select(x => new AutoAssignmentExamFormatPolicyEditDto
                {
                    ExamFormatId = x.ExamFormatId,
                    Code = x.ExamFormat.Code,
                    Name = x.ExamFormat.Name,
                    AssignmentMode = NormalizeAssignmentMode(x.AssignmentMode)
                })
                .ToList();

            return new AutoAssignmentPolicyEditDto
            {
                PolicyId = policy.PolicyId,
                FacultyId = policy.FacultyId,
                PolicyName = policy.PolicyName,
                RequiredInvigilatorsPerSchedule = policy.RequiredInvigilatorsPerSchedule,
                AllowCrossFaculty = policy.AllowCrossFaculty,
                RequirePeriodAvailabilityIfExists = policy.RequirePeriodAvailabilityIfExists,
                AllowFacultyMemberAsFallback = policy.AllowFacultyMemberAsFallback,
                MaxAssignmentsPerDay = policy.MaxAssignmentsPerDay,
                MaxAssignmentsPerPeriod = policy.MaxAssignmentsPerPeriod,
                MaxAssignmentsPerSlot = policy.MaxAssignmentsPerSlot,
                SolverTimeLimitSeconds = policy.SolverTimeLimitSeconds,
                IsDatabasePolicy = true,
                Rules = rules,
                ExamFormatPolicies = formatPolicies
            };
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

        private static string DescribeRule(string ruleCode)
        {
            return ruleCode switch
            {
                AutoAssignmentPolicyRuleCodes.ExactOwner => "Ưu tiên chính giảng viên đang dạy lớp học phần của lịch thi đó.",
                AutoAssignmentPolicyRuleCodes.SameSubject => "Tăng khả năng chọn người từng dạy hoặc có liên quan cùng môn thi.",
                AutoAssignmentPolicyRuleCodes.OralSpecialist => "Ưu tiên mạnh chuyên môn với các hình thức vấn đáp.",
                AutoAssignmentPolicyRuleCodes.PracticalSpecialist => "Ưu tiên mạnh chuyên môn với các hình thức thực hành/phòng máy/đồ án.",
                AutoAssignmentPolicyRuleCodes.LowLoad => "Giúp phân bổ đều hơn theo tổng tải trong học kỳ.",
                AutoAssignmentPolicyRuleCodes.SameDayLoad => "Giảm xu hướng dồn nhiều ca trong cùng một ngày.",
                AutoAssignmentPolicyRuleCodes.Location => "Giảm chi phí di chuyển giữa các phòng/giảng đường trong cùng buổi.",
                AutoAssignmentPolicyRuleCodes.OwnerReservePenalty => "Phạt nhẹ khi dùng một giảng viên đang là owner của nhiều lịch khác làm người dự phòng, để giữ họ cho lịch của chính họ.",
                AutoAssignmentPolicyRuleCodes.Emergency => "Cho phép phương án dự phòng khi thiếu người đúng chuyên môn.",
                AutoAssignmentPolicyRuleCodes.FacultyMember => "Mức phạt khi phải dùng vai trò trong khoa không phải giảng viên.",
                AutoAssignmentPolicyRuleCodes.Shortage => "Mục tiêu bắt buộc của hệ thống: luôn giảm tối đa số lịch thiếu giám thị trước khi xét các ưu tiên khác.",
                _ => "Tiêu chí tối ưu của thuật toán phân công."
            };
        }
    }
}
