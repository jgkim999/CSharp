using FastEndpoints;
using FastEndpoints.Swagger;
using FastEndpoints.Security;
using GamePulse.Configs;
using GamePulse.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var section = builder.Configuration.GetSection("Jwt");
var jwtConfig = section.Get<JwtConfig>();
if (jwtConfig == null)
{
    throw new NullReferenceException();
}

builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
builder.Services.AddAuthorization();

builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
    {
        o.DocumentSettings = s => 
        {
            s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
            s.DocumentName = "Initial version";
            s.Title = "My API";
            s.Version = "v0";
        };
    })
    .SwaggerDocument(o =>
    {
        o.MaxEndpointVersion = 1;
        o.DocumentSettings = s => 
        {
            s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
            s.DocumentName = "Release 1";
            s.Title = "My API";
            s.Version = "v1";
        };
    })
    .SwaggerDocument(o =>
    {
        o.MaxEndpointVersion = 2;
        o.DocumentSettings = s => 
        {
            s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
            s.DocumentName = "Release 2";
            s.Title = "My API";
            s.Version = "v2";
        };
    });
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthentication();
app.UseFastEndpoints(c =>
{
    c.Versioning.Prefix = "v";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
    string[] versions = ["v1", "v2"];
    app.MapScalarApiReference(options => options.AddDocuments(versions));
}

app.Run();
