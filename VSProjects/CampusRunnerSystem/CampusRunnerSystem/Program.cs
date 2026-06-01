var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<CampusRunnerSystem.Helpers.OracleDbHelper>();
builder.Services.AddScoped<CampusRunnerSystem.Repositories.IAccountRepository, CampusRunnerSystem.Repositories.AccountRepository>();
builder.Services.AddScoped<CampusRunnerSystem.Services.IAccountService, CampusRunnerSystem.Services.AccountService>();
builder.Services.AddScoped<CampusRunnerSystem.Repositories.INodeRepository, CampusRunnerSystem.Repositories.NodeRepository>();
builder.Services.AddScoped<CampusRunnerSystem.Services.INodeService, CampusRunnerSystem.Services.NodeService>();
builder.Services.AddScoped<CampusRunnerSystem.Repositories.IServiceTypeRepository, CampusRunnerSystem.Repositories.ServiceTypeRepository>();
builder.Services.AddScoped<CampusRunnerSystem.Services.IServiceTypeService, CampusRunnerSystem.Services.ServiceTypeService>();
builder.Services.AddScoped<CampusRunnerSystem.Repositories.IServiceNodeRuleRepository, CampusRunnerSystem.Repositories.ServiceNodeRuleRepository>();
builder.Services.AddScoped<CampusRunnerSystem.Services.IServiceNodeRuleService, CampusRunnerSystem.Services.ServiceNodeRuleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
