using DuckDB.NET.Data;
using DuckDB.NET.Native;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace TestWebApp.Services
{
    public class DuckDbService : IAsyncDisposable
    {
        private readonly DuckDBConnection _connection;
        private readonly string _dataFolder;

        public DuckDbService()
        {
            var basePath = AppContext.BaseDirectory;
            var dbPath = Path.Combine(basePath, "local.duckdb");
            _dataFolder = Path.Combine(basePath, "data");

            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            _connection = new DuckDBConnection($"DataSource={dbPath}");
            _connection.Open();
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : new()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<T>();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Map column names to their ordinal index for efficient lookup
            var columnOrdinalMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnOrdinalMap[reader.GetName(i)] = i;
            }

            // You were here:
            while (await reader.ReadAsync())
            {
                var item = new T(); // Create a new instance of T

                foreach (var prop in props)
                {
                    // Check if a column with the property's name exists
                    if (columnOrdinalMap.TryGetValue(prop.Name, out var ordinal))
                    {
                        if (await reader.IsDBNullAsync(ordinal))
                        {
                            continue;
                        }

                        object value = reader.GetValue(ordinal);

                        try
                        {
                            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            var convertedValue = Convert.ChangeType(value, targetType);

                            prop.SetValue(item, convertedValue);
                        }
                        catch (InvalidCastException)
                        {
                            prop.SetValue(item, value);
                        }
                        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                        {
                        }
                    }
                }

                results.Add(item); // Add the populated object to the list
            }

            return results;
        }

        public string GetDataFolder() => _dataFolder;

        public ValueTask DisposeAsync()
        {
            _connection?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}