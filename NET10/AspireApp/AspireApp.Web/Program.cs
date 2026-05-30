using AspireApp.Web;
using AspireApp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Valkey 연결 (문자열로 접속)
// 개발 환경: Aspire가 자동으로 주입
// 프로덕션 환경: appsettings.json에서 가져옴
var valkeyConnectionString = builder.Configuration.GetConnectionString("valkey");

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"[Development] Valkey: {valkeyConnectionString}");
}
else
{
    Console.WriteLine($"[Production] Valkey: {valkeyConnectionString}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("http://apiservice"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
