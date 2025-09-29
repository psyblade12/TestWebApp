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
if (builder.Environment.IsDevelopment())
{
    // Local development - bind to specific localhost port
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(7254, o => 
        {
            o.Protocols = HttpProtocols.Http1AndHttp2;
            o.UseHttps();
        });
    });
}
else
{
    // Azure deployment - let Azure handle port binding
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}

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
