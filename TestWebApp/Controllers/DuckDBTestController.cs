using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using TestWebApp.Services;

namespace TestWebApp.Controllers
{
    [Route("[controller]")]
    public class DuckDBTestController : Controller
    {
        private readonly DuckDbService _duckDb;

        public DuckDBTestController(DuckDbService duckDb)
        {
            _duckDb = duckDb;
        }

        [HttpGet("preview")]
        public async Task<IActionResult> GetPreview()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "generated", "*.parquet");
            var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM read_parquet('{filePath}') g INNER JOIN (SELECT * FROM read_parquet('{filePath}') WHERE datevalue = '2025-12-22' AND intvalue1 > 400 AND intvalue1  <950) as a ON g.ID <> a.ID WHERE g.datevalue = '2025-12-22' AND g.intvalue1 > 400 AND g.intvalue1 < 950 GROUP BY g.datevalue;");
            return Ok(result);
        }

        [HttpGet("preview2")]
        public async Task<IActionResult> GetPreview2()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "flights-1m.parquet");
            var result = await _duckDb.QueryAsync<FlightData>($"SELECT * FROM read_parquet('{filePath}') LIMIT 10;");
            return Ok(result);
        }

        [HttpGet("TestLatency")]
        public async Task<string> TestLatency()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string filePath = Path.Combine(AppContext.BaseDirectory, "data", "generated", "*.parquet");
                var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM flights;");
                sw.Stop();

                string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
                return resultString;
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        [HttpGet("TestLatency2")]
        public async Task<string> TestLatency2()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "generated", "*.parquet");
            var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM generated g INNER JOIN (SELECT * FROM generated WHERE datevalue = '2025-12-22' AND intvalue1 > 400 AND intvalue1  <950) as a ON g.ID <> a.ID WHERE g.datevalue = '2025-12-22' AND g.intvalue1 > 400 AND g.intvalue1 < 950 GROUP BY g.datevalue;");
            sw.Stop();

            string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
            return resultString;
        }

        [HttpGet("TestLatency3")]
        public async Task<string> TestLatency3()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "flights-1m.parquet");
            var result = await _duckDb.QueryAsync<FlightData>($"SELECT * FROM read_parquet('{filePath}') LIMIT 10;");
            sw.Stop();

            string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
            return resultString;
        }

        [HttpGet("TestLatency4")]
        public async Task<string> TestLatency4()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "flights-1m.parquet");
            var result = await _duckDb.QueryAsync<FlightData>($"SELECT * FROM flights LIMIT 10;");
            sw.Stop();

            string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
            return resultString;
        }

        [HttpGet("TestLatency5")]
        public async Task<string> TestLatency5()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var result = await _duckDb.QueryStoredProcedureAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM flights;");
                sw.Stop();

                string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
                return resultString;
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        [HttpGet("TestLatency6")]
        public async Task<string> TestLatency6()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM flights WHERE FL_DATE >= '2006-01-01' AND FL_DATE <= '2006-01-01';");
                sw.Stop();

                string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
                return resultString;
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        [HttpGet("TestLatency7")]
        public async Task<string> TestLatency7()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM generated WHERE datevalue >= '2025-12-01' AND datevalue <= '2025-12-31'");
            sw.Stop();

            string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
            return resultString;
        }

        [HttpGet("TestLatency8")]
        public async Task<string> TestLatency8()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string filePath = Path.Combine(AppContext.BaseDirectory, "data", "generated", "*.parquet");
            var result = await _duckDb.QueryAsync<FlightResult>($"SELECT COUNT(1) AS COUNT FROM '{filePath}' WHERE datevalue >= '2025-12-01' AND datevalue <= '2025-12-31';");
            sw.Stop();

            string resultString = $"Elapsed time is: {sw.ElapsedMilliseconds} ms. Data: {JsonSerializer.Serialize(result)}";
            return resultString;
        }

        [HttpGet("ReturnString")]
        public async Task<string> ReturnString()
        {
            string result = await Task.FromResult("a string...");
            return result;
        }
    }

    public class FlightData
    {
        public DateOnly FL_DATE { get; set; }
        public int DEP_DELAY { get; set; }
        public int ARR_DELAY { get; set; }
        public int AIR_TIME { get; set; }
        public int DISTANCE { get; set; }
        public decimal DEP_TIME { get; set; }
        public decimal ARR_TIME { get; set; }
    }
    public class FlightResult
    {
        public double COUNT { get; set; }
    }
}