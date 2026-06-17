using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Common.Helpers;
using ExamInvigilationManagement.Infrastructure.Mapping;
using E = ExamInvigilationManagement.Infrastructure.Data.Entities;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class AcademyYearGeneratorService : IAcademyYearGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public AcademyYearGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateAsync(Domain.Entities.AcademyYear year, List<SemesterOptionDto> options)
        {
            var dataYear = year.ToEntity();
            var semesters = new List<E.Semester>();

            foreach (var semOpt in options.Where(x => x.Selected))
            {
                var sem = new E.Semester
                {
                    SemesterName = SemesterHelper.ToName(semOpt.Type),
                    AcademyYear = dataYear
                };

                var periods = new List<E.ExamPeriod>();

                foreach (var perOpt in semOpt.Periods.Where(x => x.Selected))
                {
                    var period = new E.ExamPeriod
                    {
                        PeriodName = perOpt.Name,
                        Semester = sem
                    };

                    var sessions = new List<E.ExamSession>();

                    foreach (var sesOpt in perOpt.Sessions.Where(x => x.Selected))
                    {
                        var session = new E.ExamSession
                        {
                            SessionName = sesOpt.Name,
                            Period = period
                        };

                        var slots = new List<E.ExamSlot>();

                        foreach (var slotOpt in sesOpt.Slots.Where(x => x.Selected))
                        {
                            slots.Add(new E.ExamSlot
                            {
                                SlotName = slotOpt.Name,
                                TimeStart = slotOpt.TimeStart,
                                Session = session
                            });
                        }

                        session.ExamSlots = slots;
                        sessions.Add(session);
                    }

                    period.ExamSessions = sessions;
                    periods.Add(period);
                }

                sem.ExamPeriods = periods;
                semesters.Add(sem);
            }

            dataYear.Semesters = semesters;

            _context.AcademyYears.Add(dataYear);
            await _context.SaveChangesAsync();
        }
    }
}
