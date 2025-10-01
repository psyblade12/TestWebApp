using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using TestWebApp.Services;

namespace TestWebApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CosmosTestController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;

        public CosmosTestController(CosmosDbService cosmosService)
        {
            _cosmosService = cosmosService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _cosmosService.GetItemsAsync<AccountNumberData>("SELECT c.AccountNumber, c.Data, c.id FROM c");
            return Ok(items);
        }

        [HttpGet("GetByID")]
        public async Task<IActionResult> GetByID(string id, string partitionKey)
        {
            var items = await _cosmosService.GetItemAsync<AccountNumberData>(id, partitionKey);
            return Ok(items);
        }

        [HttpGet("TestQueryTime")]
        public async Task<string> TestQueryTime(string id, string partitionKey)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var item = await _cosmosService.GetItemAsync<AccountNumberData>(id, partitionKey);
            sw.Stop();

            return $"The returned data {JsonSerializer.Serialize(item)}. Elapsed time: {sw.ElapsedMilliseconds}ms";
        }

        [HttpGet("TestEndpoint")]
        public async Task<string> TestEndpoint()
        {
            return $"Hello....";
        }
    }

    class AccountNumberData
    {
        public string? ID { get; set; }
        public string? Data { get; set; }
        public string? AccountNumber { get; set; }
    }
}
