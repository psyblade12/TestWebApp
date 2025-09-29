using Grpc.Net.Client;
using MyApi.Grpc;

Console.WriteLine("🚀 Testing unified HTTPS endpoint at localhost:7254");
Console.WriteLine("📋 This port supports both REST (HTTP/1.1) and gRPC (HTTP/2)");
Console.WriteLine();

// Test 1: REST API
Console.WriteLine("🌐 Testing REST API...");
try
{
    using var httpClient = new HttpClient();
    // Accept self-signed certificates for development
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    
    using var client = new HttpClient(handler);
    var response = await client.GetStringAsync("https://localhost:7254/");
    Console.WriteLine($"✅ REST SUCCESS: {response}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ REST ERROR: {ex.Message}");
}

Console.WriteLine();

// Test 2: gRPC API
Console.WriteLine("📡 Testing gRPC API...");
try
{
    // Configure handler to accept self-signed certificates
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    
    using var channel = GrpcChannel.ForAddress("https://localhost:7254", new GrpcChannelOptions
    {
        HttpHandler = handler
    });
    
    var client = new WeatherService.WeatherServiceClient(channel);
    
    var request = new WeatherRequest { City = "London" };
    var response = await client.GetWeatherAsync(request);
    
    Console.WriteLine($"✅ gRPC SUCCESS: {response.Forecast}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ gRPC ERROR: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("🎯 Test completed! Single HTTPS port (7254) handling both protocols.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();