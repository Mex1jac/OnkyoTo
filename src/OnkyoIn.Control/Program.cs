using OnkyoIn.Control.Application.Services;
using OnkyoIn.Control.Domain.Gateways;
using OnkyoIn.Control.Infrastructure.Onkyo;

var builder = WebApplication.CreateBuilder(args);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DDD wiring: application service depends on the gateway port (dependency inversion).
builder.Services.AddScoped<IOnkyoDeviceGateway, OnkyoDeviceGateway>();
builder.Services.AddScoped<DeviceControlService>();

// CORS so the React SPA (OnkyoIn.Web) can call this API from the browser.
const string SpaCorsPolicy = "SpaCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(SpaCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
