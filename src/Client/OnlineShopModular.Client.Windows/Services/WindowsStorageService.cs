using System.IO.IsolatedStorage;

namespace OnlineShopModular.Client.Windows.Services;

public partial class WindowsStorageService : IStorageService
{
    private Dictionary<string, string?>? persistentStorage;
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<bool> IsPersistent(string key)
    {
        return string.IsNullOrEmpty(await GetItem(key)) is false;
    }

    public async ValueTask<string?> GetItem(string key)
    {
        if (tempStorage.TryGetValue(key, out string? value))
            return value;

        persistentStorage ??= await Restore();

        return persistentStorage.GetValueOrDefault(key, null);
    }

    public async ValueTask RemoveItem(string key)
    {
        tempStorage.Remove(key);

        persistentStorage ??= await Restore();

        if (persistentStorage.Remove(key))
        {
            await Save(persistentStorage);
        }
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (persistent)
        {
            persistentStorage ??= await Restore();
            persistentStorage[key] = value;
            await Save(persistentStorage);
        }
        else
        {
            tempStorage[key] = value;
        }
    }

    public async ValueTask Clear()
    {
        persistentStorage?.Clear();
        tempStorage?.Clear();
        await Save([]);
    }

    const string WindowsStorageFilename = "OnlineShopModular.Client.Windows.storage.json";
    private static readonly SemaphoreSlim ioLock = new(1, 1);
    // Restore application-scope property from isolated storage
    private static async Task<Dictionary<string, string?>> Restore()
    {
        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.OpenOrCreate, storage);
            if (stream.Length == 0)
                return [];
            return (await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.DictionaryStringString))!;
        }
        finally
        {
            ioLock.Release();
        }
    }

    // Persist application-scope property to isolated storage
    private static async Task Save(Dictionary<string, string?> data)
    {
        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.Create, storage);
            using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(JsonSerializer.Serialize(data, AppJsonContext.Default.DictionaryStringString));
        }
        finally
        {
            ioLock.Release();
        }
    }
}
