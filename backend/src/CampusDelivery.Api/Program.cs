using CampusDelivery.Api.Persistence.Oracle;

var builder = WebApplication.CreateBuilder(args);

const string frontendDevelopmentCorsPolicy = "FrontendDevelopment";

builder.Services.AddControllers();
builder.Services.AddSingleton<OracleConnectionFactory>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendDevelopmentCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors(frontendDevelopmentCorsPolicy);
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/api/health"));

app.Run();
