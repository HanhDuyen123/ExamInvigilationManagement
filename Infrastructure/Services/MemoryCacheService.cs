using ExamInvigilationManagement.Application.Interfaces.Common;
using Microsoft.Extensions.Caching.Memory;

namespace ExamInvigilationManagement.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
    {
        _cache.Set(key, value, absoluteExpirationRelativeToNow);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}
