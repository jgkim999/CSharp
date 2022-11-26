using Example1.Areas.Identity;
using Example1.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/rumble-.txt", rollingInterval: RollingInterval.Hour)
            //.WriteTo.File(
            //   System.IO.Path.Combine(path1: Environment.GetEnvironmentVariable("HOME"), "LogFiles", "Application", "diagnostics.txt"),
            //   rollingInterval: RollingInterval.Day,
            //   fileSizeLimitBytes: 10 * 1024 * 1024,
            //   retainedFileCountLimit: 2,
            //   rollOnFileSizeLimit: true,
            //   shared: true,
            //   flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

//new LoggerConfiguration()
//.MinimumLevel.Debug()
//.WriteTo.Console()
//.WriteTo.File("logs/rumble-.txt", rollingInterval: RollingInterval.Day)
//.CreateLogger();
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

Log.Information("Log init");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
