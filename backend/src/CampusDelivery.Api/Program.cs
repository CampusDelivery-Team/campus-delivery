using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // ---> 新增：引入 Cookie 认证命名空间

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

builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<NodeRepository>();
builder.Services.AddScoped<NodeService>();

// ---> 新增 1：注册账户模块服务
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();

// ---> 新增 2：配置 Cookie 认证服务
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // 告诉系统，没登录的人强制踢回这个页面
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// ---> 新增 3：启用认证和授权中间件 (必须放在 UseRouting 和 MapControllerRoute 之间)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
