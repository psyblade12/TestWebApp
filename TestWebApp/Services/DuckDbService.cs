using DuckDB.NET.Data;
using DuckDB.NET.Native;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace TestWebApp.Services
{
    public class DuckDbService
    {
        private readonly ConcurrentQueue<DuckDBConnection> _pool;
        private readonly int _poolSize = 20;
        private readonly Dictionary<DuckDBConnection, DuckDBCommand> _preparedQueries;

        public DuckDbService(IConfiguration configuration)
        {
            var blobStorageConnectionString = configuration["BlobStorageConnectionString"];
            _preparedQueries = new Dictionary<DuckDBConnection, DuckDBCommand>();
            _pool = new ConcurrentQueue<DuckDBConnection>();


            for (int i = 0; i < _poolSize; i++)
            {
                var conn = new DuckDBConnection("DataSource=:memory:?cache=shared");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SET azure_storage_connection_string = '{blobStorageConnectionString}'; ";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $"SET azure_transport_option_type = 'curl';";
                cmd.ExecuteNonQuery();

                if (i == 0)
                {
                    cmd.CommandText = $"INSTALL azure; LOAD azure;";
                    cmd.CommandText = $"INSTALL iceberg; LOAD iceberg;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE OR REPLACE VIEW generated AS SELECT * FROM read_parquet('azure://tantestdatalake.blob.core.windows.net/data/generated/*.parquet');";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE OR REPLACE VIEW flights AS SELECT * FROM read_parquet('azure://tantestdatalake.blob.core.windows.net/data/flightdata/*.parquet');";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE OR REPLACE VIEW iceberg AS SELECT * FROM iceberg_scan('azure://tantestdatalake.blob.core.windows.net/data/iceberg_flights', version_name_format = '%s.metadata.json%s', allow_moved_paths = true);";
                    cmd.ExecuteNonQuery();
                }

                //Prepare, so that it can save compiling time
                var preparedCommand = conn.CreateCommand();
                preparedCommand.CommandText = "SELECT COUNT(1) AS COUNT FROM flights WHERE FL_DATE >= ? AND FL_DATE <= ?;";
                preparedCommand.Prepare();

                _preparedQueries[conn] = preparedCommand;

                _pool.Enqueue(conn);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : new()
        {
            if (!_pool.TryDequeue(out var connection))
                throw new InvalidOperationException("No available DuckDB connections in pool");

            using var cmd = connection.CreateCommand();

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

                results.Add(item);
            }

            _pool.Enqueue(connection);
            return results;
        }

        public async Task<IEnumerable<T>> QueryStoredProcedureAsync<T>(string sql) where T : new()
        {
            if (!_pool.TryDequeue(out var connection))
                throw new InvalidOperationException("No available DuckDB connections in pool");

            using var cmd = _preparedQueries[connection];
            cmd.Parameters.Add(new DuckDBParameter { Value = new DateTime(2006, 1, 1) });
            cmd.Parameters.Add(new DuckDBParameter { Value = new DateTime(2006, 1, 1) });

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

                results.Add(item);
            }

            _pool.Enqueue(connection);
            return results;
        }
    }
}