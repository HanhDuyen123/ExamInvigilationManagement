using ExamInvigilationManagement.Infrastructure.Data;
using ExamInvigilationManagement.Infrastructure.Data.Entities;
using ExamInvigilationManagement.Application.DTOs.Admin.AcademyYear;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Common.Helpers;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class AcademyYearGeneratorService : IAcademyYearGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public AcademyYearGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateAsync(AcademyYear year, List<SemesterOptionDto> options)
        {
            var semesters = new List<Semester>();

            foreach (var semOpt in options.Where(x => x.Selected))
            {
                var sem = new Semester
                {
                    SemesterName = SemesterHelper.ToName(semOpt.Type),
                    AcademyYear = year
                };

                var periods = new List<ExamPeriod>();

                foreach (var perOpt in semOpt.Periods.Where(x => x.Selected))
                {
                    var period = new ExamPeriod
                    {
                        PeriodName = perOpt.Name,
                        Semester = sem
                    };

                    var sessions = new List<ExamSession>();

                    foreach (var sesOpt in perOpt.Sessions.Where(x => x.Selected))
                    {
                        var session = new ExamSession
                        {
                            SessionName = sesOpt.Name,
                            Period = period
                        };

                        var slots = new List<ExamSlot>();

                        foreach (var slotOpt in sesOpt.Slots.Where(x => x.Selected))
                        {
                            slots.Add(new ExamSlot
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

            year.Semesters = semesters;

            _context.AcademyYears.Add(year);
            await _context.SaveChangesAsync();
        }
    }
}