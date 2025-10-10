using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace TestWebApp.Services
{
    public class RedisCacheService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(10);

        public RedisCacheService(IConfiguration configuration)
        {
            string connectionString = configuration["RedisConnection"] ?? throw new InvalidOperationException("Redis connection string is missing in appsettings.json"); ;
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
        }

        public async Task SetAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value, _defaultExpiry);
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }
    }
}
