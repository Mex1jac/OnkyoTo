using OnkyoIn.Web.Application.Services;
using OnkyoIn.Web.Domain.Gateways;
using OnkyoIn.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Typed HttpClient that talks to the OnkyoIn.Control API (base URL from config).
builder.Services.AddHttpClient<IOnkyoControlClient, OnkyoControlHttpClient>(client =>
{
    var controlApiUrl = builder.Configuration["ControlApi:BaseUrl"] ?? "https://localhost:7066";
    client.BaseAddress = new Uri(controlApiUrl);
});

// DDD wiring: application service depends on the gateway port (dependency inversion).
builder.Services.AddScoped<DeviceAppService>();

// CORS so the React dev server (Vite) can call this API from the browser.
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
