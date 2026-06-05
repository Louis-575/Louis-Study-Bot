namespace LouisStudyBot.Utils;

public static class EnvConfig
{
    private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        string? envFile = FindEnvFile();
        if (envFile is not null)
        {
            LoadEnvFile(envFile);
        }

        foreach (var entry in Environment.GetEnvironmentVariables())
        {
            if (entry is System.Collections.DictionaryEntry dictionaryEntry
                && dictionaryEntry.Key is string key
                && dictionaryEntry.Value is string value)
            {
                Values[key] = value;
            }
        }

        _initialized = true;
    }

    public static string Get(string key, string defaultValue = "")
    {
        if (!_initialized)
        {
            Initialize();
        }

        return Values.TryGetValue(key, out string? value) ? value : defaultValue;
    }

    private static string? FindEnvFile()
    {
        string currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        string baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, ".env");
        if (File.Exists(baseDirectoryPath))
        {
            return baseDirectoryPath;
        }

        DirectoryInfo? directory = Directory.GetParent(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static void LoadEnvFile(string envFile)
    {
        foreach (string line in File.ReadAllLines(envFile))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            int splitIndex = trimmed.IndexOf('=');
            if (splitIndex <= 0)
            {
                continue;
            }

            string key = trimmed[..splitIndex].Trim();
            string value = trimmed[(splitIndex + 1)..].Trim();
            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            Values[key] = value;
        }
    }
}
