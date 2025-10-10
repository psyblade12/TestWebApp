using Microsoft.AspNetCore.Server.Kestrel.Core;
using MyApi.Grpc;
using TestWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddSingleton<RedisCacheService>();

// Configure gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

app.UseGrpcWeb();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Map gRPC services
app.MapGrpcService<WeatherServiceImpl>().EnableGrpcWeb();

// Map REST API controllers
app.MapControllers();

app.Run();
