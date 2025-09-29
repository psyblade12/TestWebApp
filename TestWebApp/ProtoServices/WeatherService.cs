using Grpc.Core;
using MyApi.Grpc;

public class WeatherServiceImpl : WeatherService.WeatherServiceBase
{
    public override Task<WeatherReply> GetWeather(WeatherRequest request, ServerCallContext context)
    {
        var forecast = $"Weather in {request.City} is sunny";
        return Task.FromResult(new WeatherReply { Forecast = forecast });
    }
}