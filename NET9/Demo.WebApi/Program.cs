using Demo.Application;
using Microsoft.AspNetCore.Http.Features;

using Scalar.AspNetCore;

using System.Diagnostics;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddApplicationServices();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            
        }

        app.UseAuthorization();
        // {Scheme}://{ServiceHost}:{ServicePort}/scalar/v1
        app.MapScalarApiReference();
        app.MapControllers();
        app.Run();
    }
}
