using CsvHelper;
using System.Globalization;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace TestWebApp.Models
{
    public class UserData
    {
        public int Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; } 

        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
        public string Field3 { get; set; } = string.Empty;
        public string Field4 { get; set; } = string.Empty;
        public string Field5 { get; set; } = string.Empty;
        public string Field6 { get; set; } = string.Empty;
    }

    class CompressedUserData
    {
        public int Id { get; set; }
        public int Age { get; set; }

        public string NameAndFields { get; set; } = string.Empty;
        //public byte[] NameAndFields { get; set; } = Array.Empty<byte>();
    }

    class StreamData
    {
        public int Id { get; set; }
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
    }


    public class AppLogic
    {
        public Dictionary<string, byte[]> ReturnData(string input)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "data.csv");
            var records = new List<UserData>();

            long before = GC.GetTotalMemory(true);
            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    foreach (var record in csv.GetRecords<UserData>())
                    {
                        records.Add(record);
                    }
                }
            }

            var groups = new Dictionary<string, byte[]>();
            foreach (var nameGroup in records.GroupBy(r => r.Name))
            {
                var sb = new StringBuilder();

                foreach (var row in nameGroup)
                {
                    sb.Append(row.Id).Append(",")
                      .Append(row.Age).Append(",")
                      .Append(row.Field1).Append(",")
                      .Append(row.Field2).Append(",")
                      .Append(row.Field3).Append(",")
                      .Append(row.Field4).Append(",")
                      .Append(row.Field5).Append(",")
                      .Append(row.Field6)
                      .Append(";"); // separator between rows
                }

               //Convert to bytes
               var bytes = Encoding.UTF8.GetBytes(sb.ToString());

                // Compress
                using var ms = new MemoryStream();
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }

                groups[nameGroup.Key] = ms.ToArray();
            }

            //Dictionary<string, List<UserData>> dictionaryNaive = records
            //                                                .GroupBy(p => p.Name)
            //                                                .ToDictionary(g => g.Key, g => g.ToList());

            records = null;

            return groups;
        }

        public Dictionary<string, (long offset, int length)> FlushToDisk(Dictionary<string, byte[]> dict)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "dict.bin");

            var index = new Dictionary<string, (long offset, int length)>();
            long offset = 0;

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            foreach (var kvp in dict)
            {
                byte[] value = kvp.Value;
                fs.Write(value, 0, value.Length);

                index[kvp.Key] = (offset, value.Length);
                offset += value.Length;
            }

            return index;
        }

        public async Task<List<int>> ProcessDataByStream()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            long before = GC.GetTotalMemory(true);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "streamdata.csv");
            var records = new List<StreamData>();

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    await foreach (var record in csv.GetRecordsAsync<StreamData>())
                    {
                        records.Add(record);
                    }
                }
            }

            var result = new List<int>();
            foreach(var item in records)
            {
                var index = item.Text2.IndexOf("666");
                result.Add(index);
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            long after = GC.GetTotalMemory(true);
            var memoryFootPrint = (after - before) / 1024.0;
            return result;
        }

        public async Task<List<int>> ProcessDataByStream2()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            long before = GC.GetTotalMemory(true);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "streamdata.csv");
            var records = new List<StreamData>();

            var result = new List<int>();
            var listMemory = new List<int>();

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    await foreach (var record in csv.GetRecordsAsync<StreamData>())
                    {
                        var index = record.Text2.IndexOf("666");
                        result.Add(index);

                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                        GC.WaitForPendingFinalizers();
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                        long after = GC.GetTotalMemory(true);
                        var memoryFootPrint = (after - before) / 1024.0;
                        listMemory.Add((int)memoryFootPrint);
                    }
                }
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            long final = GC.GetTotalMemory(true);
            var memoryFootPrintFinal = (final - before) / 1024.0;

            return result;
        }
    }

    public static class DiskKVReader
    {
        private static MemoryMappedFile? mmf;
        private static Dictionary<string, (long offset, int length)>? index;

        // Call this once at startup to initialize
        public static void Initialize(Dictionary<string, (long offset, int length)> idx)
        {
            if (mmf != null)
                return; // already initialized

            // Open the file with read sharing to avoid locking issues
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "dict.bin");
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);

            index = idx ?? throw new ArgumentNullException(nameof(idx));
        }

        public static byte[] Get(string key)
        {
            if (index == null)
                throw new InvalidOperationException("DiskKVReader is not initialized.");

            if (!index.TryGetValue(key, out var info))
                return Array.Empty<byte>();

            using var accessor = mmf.CreateViewAccessor(info.offset, info.length, MemoryMappedFileAccess.Read);
            byte[] buffer = new byte[info.length];
            accessor.ReadArray(0, buffer, 0, info.length);
            return buffer;
        }
    }

    public static class GlobalIndex
    {
        public static Dictionary<string, (long offset, int length)> Index = new Dictionary<string, (long, int)>();
    }

    public static class IndexDisk
    {
        public static void SaveIndexToDisk(Dictionary<string, (long offset, int length)> index)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "index.bin");
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            foreach (var kvp in index)
            {
                string key = kvp.Key;
                var (offset, length) = kvp.Value;

                byte[] keyBytes = Encoding.UTF8.GetBytes(key);

                // Write key length, key bytes, offset, length
                writer.Write(keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write(offset);
                writer.Write(length);
            }
        }

        public static Dictionary<string, (long offset, int length)> LoadIndexFromDisk()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "index.bin");
            var index = new Dictionary<string, (long offset, int length)>();

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            while (stream.Position < stream.Length)
            {
                int keyLen = reader.ReadInt32();
                string key = Encoding.UTF8.GetString(reader.ReadBytes(keyLen));
                long offset = reader.ReadInt64();
                int length = reader.ReadInt32();

                index[key] = (offset, length);
            }

            return index;
        }
    }
}

