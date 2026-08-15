using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Presentation/wwwroot"
});

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}.cshtml");
    })
    .AddControllersAsServices();

// ===== Repository & Service 注册 =====
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<IRepositoryTransactionManager, OracleRepositoryTransactionManager>();

builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<INodeService, NodeService>();

builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();

builder.Services.AddScoped<IServiceNodeRuleRepository, ServiceNodeRuleRepository>();
builder.Services.AddScoped<IServiceNodeRuleService, ServiceNodeRuleService>();

builder.Services.AddScoped<IRunnerRepository, RunnerRepository>();
builder.Services.AddScoped<IRunnerService, RunnerService>();

// 账户与地址模块
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IAddressService, AddressService>();

// 任务与接单派单模块
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAssignRepository, AssignRepository>();
builder.Services.AddScoped<IAssignService, AssignService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IRefundService, RefundService>();

// 评价投诉模块
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();

// 结算、审计与报表模块
builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

// 数据库状态诊断模块
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// ===== Cookie 认证配置 =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.LoginPath = "/Auth/Login";
    });

var app = builder.Build();

// 所有环境都使用安全错误页，避免开发者异常页向浏览器泄露 SQL、堆栈和请求信息。
app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();

// 认证 & 授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
