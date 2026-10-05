using OnkyoIn.Control.Application.Services;
using OnkyoIn.Control.Domain.Gateways;
using OnkyoIn.Control.Infrastructure.Configuration;
using OnkyoIn.Control.Infrastructure.ErrorHandling;
using OnkyoIn.Control.Infrastructure.Onkyo;

var builder = WebApplication.CreateBuilder(args);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Central error handling: maps domain exceptions to ProblemDetails responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OnkyoExceptionHandler>();

// DDD wiring: application service depends on the gateway port (dependency inversion).
builder.Services.AddScoped<IOnkyoDeviceGateway, OnkyoDeviceGateway>();
builder.Services.AddScoped<DeviceControlService>();

// CORS: origins are configured per environment. In Development the SPA
// (OnkyoIn.Web, or the Vite dev server) is allowed; in Production the list
// must be set explicitly in appsettings.Production.json.
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
