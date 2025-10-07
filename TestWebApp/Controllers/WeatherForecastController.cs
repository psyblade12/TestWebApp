using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using TestWebApp.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TestWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("DoThings")]
        public List<string> DoThings()
        {
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); 
            //GC.WaitForPendingFinalizers(); 
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            //long before = GC.GetTotalMemory(true);

            var appLogic = new AppLogic();
            var result = appLogic.ReturnData("Co gi hot");

            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            //GC.WaitForPendingFinalizers();
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            //long after = GC.GetTotalMemory(true);
            //var memoryFootPrint = (after - before) / 1024.0; 

            var index = appLogic.FlushToDisk(result);
            GlobalIndex.Index = index;
            IndexDisk.SaveIndexToDisk(index);

            return new List<string>();
        }

        [HttpGet("ReadDataByKey")]
        public List<string> ReadDataByKey()
        {
            var index = GlobalIndex.Index;
            if (GlobalIndex.Index.Count == 0)
            {
                index = IndexDisk.LoadIndexFromDisk();
            }
            DiskKVReader.Initialize(index);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            byte[] data = DiskKVReader.Get("Tan");

            stopwatch.Stop();
            var perf = stopwatch.ElapsedMilliseconds;

            string result;
            using (var compressedStream = new MemoryStream(data))
            using (var gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var readerStream = new StreamReader(gzip, Encoding.ASCII))
            {
                result = readerStream.ReadToEnd();
            }

            List<string> rows = result.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            return rows;
        }

        [HttpGet("ReturnHello")]
        public string ReturnHello(string name)
        {
            return $"Hello, {name}";
        }


        [HttpGet("StreamData")]
        public string StreamData()
        {
            var appLogic = new AppLogic();
            var result = appLogic.ProcessDataByStream2();
            return $"OK...";
        }
    }
}
