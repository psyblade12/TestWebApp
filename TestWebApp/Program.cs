using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure gRPC services
builder.Services.AddGrpc();

// Configure Kestrel to use one port for both REST and gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTPS port for both REST (HTTP/1.1) and gRPC (HTTP/2)
    options.ListenLocalhost(7254, o => 
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
        o.UseHttps();
    });
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

// Map gRPC services
app.MapGrpcService<WeatherServiceImpl>();

// Map REST API controllers
app.MapControllers();

app.Run();
