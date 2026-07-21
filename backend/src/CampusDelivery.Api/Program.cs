using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Presentation/wwwroot"
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
    });

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
builder.Services.AddScoped<TaskService>();

// 接单派单流转模块
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<AssignService>();

// 支付与退款模块
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<RefundRepository>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<RefundService>();

// 评价模块
builder.Services.AddScoped<ReviewsRepository>();
builder.Services.AddScoped<ReviewService>();

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
