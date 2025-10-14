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

            //Prepare in-memory dict
            Dictionary<string, AccountNumberData> dict = new Dictionary<string, AccountNumberData>();
            dict.Add("d4630187-37c1-4460-b766-688fb53d2ac8", new AccountNumberData() { ID = "d4630187-37c1-4460-b766-688fb53d2ac8", Data = "ABC-xyz222", AccountNumber = "WRAP00001" });
            dict.Add("c91c79d2-bfe2-4068-a60a-9f065ac8e078", new AccountNumberData() { ID = "c91c79d2-bfe2-4068-a60a-9f065ac8e078", Data = "ABC-xyz", AccountNumber = "WRAP00003" });
            dict.Add("edb028a5-d147-4ffc-a56c-9cd835064252", new AccountNumberData() { ID = "edb028a5-d147-4ffc-a56c-9cd835064252", Data = "ABC-xyzAAABBB", AccountNumber = "WRAP0002" });

            //Prepare disk data:
            string indexJson = @"{
              ""d4630187-37c1-4460-b766-688fb53d2ac8"": {
                ""offset"": 0,
                ""length"": 93
              },
              ""c91c79d2-bfe2-4068-a60a-9f065ac8e078"": {
                ""offset"": 93,
                ""length"": 90
              },
              ""edb028a5-d147-4ffc-a56c-9cd835064252"": {
                ""offset"": 183,
                ""length"": 96
              }
            }";

            var temp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(indexJson);
            var index = new Dictionary<string, (long offset, int length)>();
            foreach (var kv in temp!)
            {
                long offset = kv.Value.GetProperty("offset").GetInt64();
                int length = kv.Value.GetProperty("length").GetInt32();
                index[kv.Key] = (offset, length);
            }


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

                double microseconds = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
                returnString.Append($"The returned data {JsonSerializer.Serialize(item)}. Elapsed time: {microseconds:F2}µs \r\n");
            }

            //Redis part:
            returnString.Append($"Testing redis... \r\n");
            for (int i = 0; i < 10; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var item = await _redis.GetAsync(cacheKey); 
                sw.Stop();

                double microseconds = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
                returnString.Append($"The returned data {item}. Elapsed time: {microseconds:F2}µs \r\n");
            }

            //In-memory part:
            returnString.Append($"Testing in memory... \r\n");
            for (int i = 0; i < 10; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var item = dict[id];
                sw.Stop();

                double microseconds = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
                returnString.Append($"The returned data {JsonSerializer.Serialize(item)}. Elapsed ticks:{sw.ElapsedTicks}. Elapsed time: {microseconds:F2}µs \r\n");
            }

            //Disk lookup part part:
            returnString.Append($"Testing disk lookup... \r\n");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "accounts.bin");
            byte[] buffer = new byte[index["c91c79d2-bfe2-4068-a60a-9f065ac8e078"].length];

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                for (int i = 0; i < 10; i++)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    var position = index["c91c79d2-bfe2-4068-a60a-9f065ac8e078"];
                    fs.Seek(position.offset, SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);

                    string json = Encoding.UTF8.GetString(buffer);
                    var item = JsonSerializer.Deserialize<AccountNumberData>(json);

                    sw.Stop();

                    double microseconds = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
                    returnString.Append($"The returned data {JsonSerializer.Serialize(item)}. Elapsed ticks:{sw.ElapsedTicks}. Elapsed time: {microseconds:F2}µs \r\n");
                }
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
