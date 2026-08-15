using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

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

builder.Services.AddScoped<NodeRepository>();
builder.Services.AddScoped<NodeService>();

builder.Services.AddScoped<ServiceTypeRepository>();
builder.Services.AddScoped<ServiceTypeService>();

builder.Services.AddScoped<ServiceNodeRuleRepository>();
builder.Services.AddScoped<ServiceNodeRuleService>();

builder.Services.AddScoped<RunnerRepository>();
builder.Services.AddScoped<RunnerService>();

// 账户模块
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AddressRepository>();
builder.Services.AddScoped<AddressService>();

// 任务与接单派单流转模块
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<AssignService>();

builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<RefundRepository>();
builder.Services.AddScoped<RefundService>();

// 评价投诉模块
builder.Services.AddScoped<ReviewsRepository>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<ComplaintRepository>();
builder.Services.AddScoped<ComplaintService>();

// 结算、审计与报表模块
builder.Services.AddScoped<SettlementRepository>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<ReportService>();

// ===== Cookie 认证配置 =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.LoginPath = "/Auth/Login";
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// 认证 & 授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
