using System;
using System.IO;
using System.Text.Json;
using Syncora.Client.Models.Auth;

namespace Syncora.Client.Services;

public class AuthSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;

    public AuthSessionStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "Syncora");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "session.json");
    }

    public SavedSession? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SavedSession>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(SavedSession session)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
