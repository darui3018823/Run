using System.IO;
using System.Text.Json;

namespace Run;

public sealed class HistoryManager
{
    public static readonly HistoryManager Instance = new();

    private const int MaxItems = 50;
    private readonly string _path;
    private List<string> _items = new();

    public IReadOnlyList<string> Items => _items;

    private HistoryManager()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RunDialog");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "history.json");
        Load();
    }

    public void AddAndSave(string command)
    {
        _items.Remove(command);
        _items.Insert(0, command);
        if (_items.Count > MaxItems)
            _items.RemoveRange(MaxItems, _items.Count - MaxItems);
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var json = File.ReadAllText(_path);
            _items = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            _items = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch { }
    }
}
