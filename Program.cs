using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ExamInvigilationManagement.Domain.Entities;
using ExamInvigilationManagement.Infrastructure.Services;
using System;
using ExamInvigilationManagement.Infrastructure.Repositories;
using ExamInvigilationManagement.Application.Services;
using ExamInvigilationManagement.Application.Interfaces.Service;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Common;
using ExamInvigilationManagement.Infrastructure.UI;
using ExamInvigilationManagement.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailLogService, EmailLogService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IInformationRepository, InformationRepository>();
builder.Services.AddScoped<IInformationService, InformationService>();
builder.Services.AddScoped<IFacultyRepository, FacultyRepository>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAcademyYearRepository, AcademyYearRepository>();
builder.Services.AddScoped<IAcademyYearService, AcademyYearService>();
builder.Services.AddScoped<IAcademyYearGeneratorService, AcademyYearGeneratorService>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IPeriodRepository, PeriodRepository>();
builder.Services.AddScoped<IPeriodService, PeriodService>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
builder.Services.AddScoped<ICourseOfferingService, CourseOfferingService>();
builder.Services.AddScoped<IAutoAssignmentService, AutoAssignmentService>();
builder.Services.AddScoped<IAutoAssignmentRepository, AutoAssignmentRepository>();
builder.Services.AddScoped<ILecturerBusySlotService, LecturerBusySlotService>();
builder.Services.AddScoped<ILecturerBusySlotRepository, LecturerBusySlotRepository>();
builder.Services.AddScoped<IExamScheduleService, ExamScheduleService>();
builder.Services.AddScoped<IExamScheduleRepository, ExamScheduleRepository>();
builder.Services.AddScoped<IManualAssignmentRepository, ManualAssignmentRepository>();
builder.Services.AddScoped<IManualAssignmentService, ManualAssignmentService>();
builder.Services.AddScoped<IInvigilatorResponseRepository, InvigilatorResponseRepository>();
builder.Services.AddScoped<IInvigilatorResponseService, InvigilatorResponseService>();
builder.Services.AddScoped<IInvigilatorSubstitutionRepository, InvigilatorSubstitutionRepository>();
builder.Services.AddScoped<IInvigilatorSubstitutionService, InvigilatorSubstitutionService>();
builder.Services.AddScoped<IExamScheduleApprovalRepository, ExamScheduleApprovalRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddScoped<IExamScheduleApprovalService, ExamScheduleApprovalService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
builder.Services.AddScoped<IBulkImportService, BulkImportService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

//builder.Services
//    .AddControllersWithViews()
//    .AddRazorOptions(options =>
//    {
//        options.ViewLocationExpanders.Add(new RoleViewLocationExpander());
//    });
// Program.cs
//builder.Services.AddTransient<EmailService>();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddSignalR();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var hasher = new PasswordHasher<ExamInvigilationManagement.Domain.Entities.User>();

//    var users = db.Users.ToList();

//    foreach (var u in users)
//    {
//        u.PasswordHash = hasher.HashPassword(null!, "123456");
//    }

//    db.SaveChanges();
//}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
