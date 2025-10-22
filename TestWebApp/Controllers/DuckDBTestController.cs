using Microsoft.AspNetCore.Mvc;
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