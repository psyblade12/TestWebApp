using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TestWebApp.Services;

namespace TestWebApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CosmosTestController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;
        private readonly RedisCacheService _redis;

        public CosmosTestController(CosmosDbService cosmosService, RedisCacheService redis)
        {
            _cosmosService = cosmosService;
            _redis = redis;
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
            return await Task.FromResult($"Hello....");
        }

        [HttpGet("GetByIDWithCache")]
        public async Task<IActionResult> GetByIDWithCache(string id, string partitionKey)
        {
            string cacheKey = $"product:{id}";
            var cachedValue = await _redis.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                var cachedProduct = System.Text.Json.JsonSerializer.Deserialize<AccountNumberData>(cachedValue);
                return Ok(cachedProduct);
            }

            var items = await _cosmosService.GetItemAsync<AccountNumberData>(id, partitionKey);

            var serialized = System.Text.Json.JsonSerializer.Serialize(items);
            await _redis.SetAsync(cacheKey, serialized);

            return Ok(items);
        }

        [HttpGet("TestQueryTimeCosmosRedis")]
        public async Task<string> TestQueryTimeCosmosRedis(string id, string partitionKey)
        {
            //Prepare cache:
            var items = await _cosmosService.GetItemAsync<AccountNumberData>(id, partitionKey);
            var serialized = System.Text.Json.JsonSerializer.Serialize(items);
            string cacheKey = $"product:{id}";
            await _redis.SetAsync(cacheKey, serialized);

            //Perform the test
            StringBuilder returnString = new StringBuilder();

            //Cosmos part:
            returnString.Append($"Testing cosmos... \r\n");
            for (int i = 0; i<10; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var item = await _cosmosService.GetItemAsync<AccountNumberData>(id, partitionKey);
                sw.Stop();

                returnString.Append($"The returned data {JsonSerializer.Serialize(item)}. Elapsed time: {sw.ElapsedMilliseconds}ms \r\n");
            }

            //Redis part:
            returnString.Append($"Testing redis... \r\n");
            for (int i = 0; i < 10; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var item = await _redis.GetAsync(cacheKey); 
                sw.Stop();

                returnString.Append($"The returned data {item}. Elapsed time: {sw.ElapsedMilliseconds}ms \r\n");
            }


            return returnString.ToString(); ;
        }
    }

    class AccountNumberData
    {
        public string? ID { get; set; }
        public string? Data { get; set; }
        public string? AccountNumber { get; set; }
    }
}
