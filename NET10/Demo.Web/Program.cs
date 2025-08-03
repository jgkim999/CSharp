using Demo.Application;
using Demo.Application.Repositories;
using Demo.Infra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfra(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/user", async (IUserRepository userRepository) =>
    {
        var users = await userRepository.GetAllAsync();
        return users;
    })
    .WithName("GetUser");

app.Run();
