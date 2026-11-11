using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _06_OrderProcessing.Shared;

public class JsonFileStore
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static SemaphoreSlim LockOf(string fileName) =>
        _locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1)); //intial count = 1, max count = 1 هنا علشان عاوز اودرر ب اوردر 

    //full path of the file 
    private static string PathOf(string fileName) =>
    Path.Combine(DataFiles.DataFolder, fileName);



    // Write the json in temp file and them move هحفظ الملفات في ملف مؤقت ثم نقلها للملف الاصلي 
    private static async Task SafeWriteAsync(string path, string json, CancellationToken ct)
    {
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, json, ct);
        File.Move(temp, path, overwrite: true);
    }

    private static void SafeWrite(string path, string json)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }





    ////Deserializing JSON to List<T> and returning 
    public async Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct = default)
    {
        var gate = LockOf(fileName);
        await gate.WaitAsync(ct);
        try
        {
            var path = PathOf(fileName);
            if (!File.Exists(path)) return new List<T>();

            try
            {
                await using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonSerializerOptions, ct)
                       ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }
        finally
        {
            gate.Release();
        }
    }


    public async Task WriteAllAsync<T>(string fileName, List<T> items, CancellationToken ct = default)
    {
        var gate = LockOf(fileName); // Locing using semaphoreslim
        await gate.WaitAsync(ct); //1) await TASk
        try
        {
            Directory.CreateDirectory(DataFiles.DataFolder);
            string json = JsonSerializer.Serialize(items, _jsonSerializerOptions);
            await SafeWriteAsync(PathOf(fileName), json, ct);
        }
        finally
        {
            gate.Release();    //2) release the lock   
        }
    }

    ///=================================================== sync methosd===============================================================================
    public List<T> ReadAll<T>(string fileName)
    {
        var gate = LockOf(fileName);
        gate.Wait();
        try
        {
            var path = PathOf(fileName);
            if (!File.Exists(path)) return new List<T>();

            try
            {
                using var stream = File.OpenRead(path);
                return JsonSerializer.Deserialize<List<T>>(stream, _jsonSerializerOptions)
                       ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }
        finally
        {
            gate.Release();
        }
    }


    public void WriteAll<T>(string fileName, List<T> items)
    {
        var gate = LockOf(fileName);
        gate.Wait();
        try
        {
            Directory.CreateDirectory(DataFiles.DataFolder);
            string json = JsonSerializer.Serialize(items, _jsonSerializerOptions);
            SafeWrite(PathOf(fileName), json);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpdateAsync<T>(string fileName, Action<List<T>> change, CancellationToken ct = default)
    {
        var gate = LockOf(fileName);
        await gate.WaitAsync(ct);
        try
        {
            var path = PathOf(fileName);
            var items = new List<T>();

            // 1) Reading files
            if (File.Exists(path))
            {
                try
                {
                    await using var stream = File.OpenRead(path);
                    items = await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonSerializerOptions, ct)
                            ?? new List<T>();
                }
                catch (JsonException ex)
                {
                    throw new DataFileCorruptedException(fileName, ex);
                }
            }

            // 2) 
            change(items);

            // 3) Writing files
            Directory.CreateDirectory(DataFiles.DataFolder);
            string json = JsonSerializer.Serialize(items, _jsonSerializerOptions);
            await SafeWriteAsync(path, json, ct);
        }
        finally
        {
            gate.Release();
        }
    }


    //update sync
    public void Update<T>(string fileName, Action<List<T>> change)
    {
        var gate = LockOf(fileName);
        gate.Wait();
        try
        {
            var path = PathOf(fileName);
            var items = new List<T>();

            if (File.Exists(path))
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    items = JsonSerializer.Deserialize<List<T>>(stream, _jsonSerializerOptions)
                            ?? new List<T>();
                }
                catch (JsonException ex)
                {
                    throw new DataFileCorruptedException(fileName, ex);
                }
            }

            change(items);

            Directory.CreateDirectory(DataFiles.DataFolder);
            string json = JsonSerializer.Serialize(items, _jsonSerializerOptions);
            SafeWrite(path, json);
        }
        finally
        {
            gate.Release();
        }
    }


}
