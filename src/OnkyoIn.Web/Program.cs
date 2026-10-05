using OnkyoIn.Web.Application.Services;
using OnkyoIn.Web.Domain.Gateways;
using OnkyoIn.Web.Infrastructure;
using OnkyoIn.Web.Infrastructure.Configuration;
using OnkyoIn.Web.Infrastructure.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Central error handling: maps control/domain exceptions to ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OnkyoExceptionHandler>();

// Typed HttpClient that talks to the OnkyoIn.Control API (base URL from config).
builder.Services.AddHttpClient<IOnkyoControlClient, OnkyoControlHttpClient>(client =>
{
    var controlApiUrl = builder.Configuration["ControlApi:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Missing configuration 'ControlApi:BaseUrl' (the URL of OnkyoIn.Control).");

    client.BaseAddress = new Uri(controlApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// DDD wiring: application service depends on the gateway port (dependency inversion).
builder.Services.AddScoped<DeviceAppService>();

// CORS: origins are configured per environment. In Development the Vite dev
// server is allowed; in Production the list must be set explicitly in
// appsettings.Production.json.
const string SpaCorsPolicy = "SpaCorsPolicy";
var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

if (corsOptions.AllowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "No CORS origins configured. Set 'Cors:AllowedOrigins' for this environment.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy =>
    {
        if (corsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(SpaCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
