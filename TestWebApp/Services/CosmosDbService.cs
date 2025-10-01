using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace TestWebApp.Services
{
    public class CosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(IConfiguration configuration)
        {
            var accountEndpoint = configuration["CosmosDbAccountEndpoint"];
            var accountKey = configuration["CosmosDbAccountKey"];
            var databaseName = configuration["CosmosDbDatabaseName"];
            var containerName = configuration["CosmosDbContainerName"];

            var client = new CosmosClient(accountEndpoint, accountKey);
            _container = client.GetContainer(databaseName, containerName);
        }

        public async Task<T> GetItemAsync<T>(string id, string partitionKey)
        {
            ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>(string queryString)
        {
            var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
            List<T> results = new();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}
