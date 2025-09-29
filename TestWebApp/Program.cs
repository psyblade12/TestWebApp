using Microsoft.AspNetCore.Server.Kestrel.Core;
using MyApi.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
