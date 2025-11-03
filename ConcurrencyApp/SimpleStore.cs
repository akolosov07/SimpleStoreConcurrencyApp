namespace ConcurrencyApp;

public sealed class SimpleStore : IDisposable
{
    private readonly Dictionary<string, byte[]> _storage = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private long _setCount;
    private long _getCount;
    private long _deleteCount;
    private bool _disposed = false;

    public void Set(string key, byte[] value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        _lock.EnterWriteLock();
        try
        {
            _storage[key] = value;
            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public byte[]? Get(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _lock.EnterReadLock();
        try
        {
            _storage.TryGetValue(key, out var value);
            Interlocked.Increment(ref _getCount);
            return value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Delete(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _lock.EnterWriteLock();
        try
        {
            var removed = _storage.Remove(key);
            if (removed)
            {
                Interlocked.Increment(ref _deleteCount);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public (long setCount, long getCount, long deleteCount) GetStatistics()
    {
        long sets = Interlocked.Read(ref _setCount);
        long gets = Interlocked.Read(ref _getCount);
        long deletes = Interlocked.Read(ref _deleteCount);
        return (sets, gets, deletes);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _lock?.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

