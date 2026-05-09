using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Domain.Enums;

namespace ExamInvigilationManagement.Common.Helpers
{
    public static class DefaultDataBuilder
    {
        public static CreateAcademyYearDto Build()
        {
            return new CreateAcademyYearDto
            {
                AutoGenerate = true,
                Semesters = new List<SemesterOptionDto>
                {
                    BuildSemester(SemesterType.Semester1, true),
                    BuildSemester(SemesterType.Semester2, true),
                    BuildSemester(SemesterType.Summer, false)
                }
            };
        }
        private static SemesterOptionDto BuildSemester(SemesterType type, bool full)
        {
            return new SemesterOptionDto
            {
                Type = type,
                Selected = true,
                Periods = full
                ? new List<ExamPeriodOptionDto>
                {
                    BuildPeriod("Giữa kỳ"),
                    BuildPeriod("Cuối kỳ")
                }
                : new List<ExamPeriodOptionDto>
                {
                    BuildPeriod("Cuối kỳ")
                }
            };
        }
        //private static SemesterOptionDto BuildSemester(string name, bool full)
        //{
        //    return new SemesterOptionDto
        //    {
        //        Name = name,
        //        Selected = true,
        //        Periods = full
        //            ? new List<ExamPeriodOptionDto>
        //            {
        //            BuildPeriod("Giữa kỳ"),
        //            BuildPeriod("Cuối kỳ")
        //            }
        //            : new List<ExamPeriodOptionDto>
        //            {
        //            BuildPeriod("Cuối kỳ")
        //            }
        //    };
        //}

        private static ExamPeriodOptionDto BuildPeriod(string name)
        {
            return new ExamPeriodOptionDto
            {
                Name = name,
                Selected = true,
                Sessions = new List<ExamSessionOptionDto>
            {
                new()
                {
                    Name = "Sáng",
                    Selected = true,
                    Slots = new()
                    {
                        new() { Name="Ca 1", Selected=true, TimeStart=new TimeOnly(7,30)},
                        new() { Name="Ca 2", Selected=true, TimeStart=new TimeOnly(9,30)}
                    }
                },
                new()
                {
                    Name = "Chiều",
                    Selected = true,
                    Slots = new()
                    {
                        new() { Name="Ca 1", Selected=true, TimeStart=new TimeOnly(13,30)},
                        new() { Name="Ca 2", Selected=true, TimeStart=new TimeOnly(15,30)}
                    }
                },
                new()
                {
                    Name = "Tối",
                    Selected = true,
                    Slots = new()
                    {
                        new() { Name="Ca 1", Selected=true, TimeStart=new TimeOnly(18,30)}
                    }
                }
            }
            };
        }
    }
}
