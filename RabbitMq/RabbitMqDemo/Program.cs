using MassTransit;
using MassTransit.Transports.Fabric;
using RabbitMqDemo;
using RabbitMqDemo.Consumer;
using RabbitMqDemo.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit((x) =>
{
    x.AddConsumer<WeatherConsumer>();
    x.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Durable = false;
        rabbit.UseMessageRetry(x => x.Interval(2, 1000));
        rabbit.Host("localhost", 5672, "/", h =>
        {
            h.Username("user");
            h.Password("1234");
        });
        // Register a receive endpoint
        rabbit.ReceiveEndpoint("queue1", endpoint =>
        {
            // Sets the consumer to be used for the endpoint
            endpoint.Durable = false;
            endpoint.ConfigureConsumer<WeatherConsumer>(context);
        });
    });;
});
builder.Services.AddMassTransit<ISecondBus>((x) =>
{
    x.AddConsumer<Queue2Consumer>();
    x.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Durable = false;
        rabbit.UseMessageRetry(x => x.Interval(2, 1000));
        rabbit.Host("localhost", 5672, "/", h =>
        {
            h.Username("user");
            h.Password("1234");
        });
        // Register a receive endpoint
        rabbit.ReceiveEndpoint("queue2", endpoint =>
        {
            // Sets the consumer to be used for the endpoint
            endpoint.Durable = false;
            endpoint.ConfigureConsumer<Queue2Consumer>(context);
        });
    });;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
