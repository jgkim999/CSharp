using MassTransit;
using RabbitMqDemo.Consumer;

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
        rabbit.UseMessageRetry(x => x.Interval(2, 1000));
   
        rabbit.Host("localhost", "/", h =>
        {
            h.Username("user");
            h.Password("1234");
        });
        
        // Register a receive endpoint
        rabbit.ReceiveEndpoint("games-ranking-queue", endpoint =>
        {
            // Sets the consumer to be used for the endpoint
            endpoint.ConfigureConsumer<WeatherConsumer>(context);
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