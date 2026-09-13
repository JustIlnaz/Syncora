using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Syncora.Client.Helpers;

/// <summary>
/// Загружает аватар по HTTP(S) в Bitmap. Avalonia Image.Source из строки
/// поддерживает только avares:// и локальные пути, но не http(s),
/// поэтому байты скачиваются вручную и превращаются в Bitmap.
/// </summary>
public static class AvatarImageLoader
{
    private static readonly HttpClient Http = new();
    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

    public static async Task<Bitmap?> LoadAsync(string? avatarUrl, int version = 0)
    {
        var absolute = AvatarUrlHelper.ToAbsolute(avatarUrl);
        if (string.IsNullOrWhiteSpace(absolute))
            return null;

        // version участвует в ключе, чтобы после смены аватара не брать старый из кэша
        var cacheKey = $"{absolute}|{version}";
        if (Cache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var bytes = await Http.GetByteArrayAsync(absolute);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            Cache[cacheKey] = bitmap;
            return bitmap;
        }
        catch (Exception ex)
        {
            // Аватар — не критичный ресурс: при недоступности API/файла
            // показываем инициалы, но оставляем след в отладочном выводе.
            System.Diagnostics.Debug.WriteLine($"Avatar load failed ({absolute}): {ex.Message}");
            return null;
        }
    }

    public static void Invalidate(string? avatarUrl)
    {
        var absolute = AvatarUrlHelper.ToAbsolute(avatarUrl);
        if (string.IsNullOrWhiteSpace(absolute))
            return;

        foreach (var key in Cache.Keys)
        {
            if (key.StartsWith(absolute, StringComparison.Ordinal))
                Cache.TryRemove(key, out _);
        }
    }
}
