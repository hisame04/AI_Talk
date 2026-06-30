using System;
using System.IO;
using UnityEngine;

public static class LocalEnv
{
    public static string Get(string key)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        string configPath = Path.Combine(Application.persistentDataPath, "config.json");
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var config = JsonUtility.FromJson<AppConfig>(json);
            if (key == "OPENAI_API_KEY" && !string.IsNullOrEmpty(config.openAiApiKey))
            {
                return config.openAiApiKey;
            }
        }

#if UNITY_EDITOR
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrEmpty(projectRoot))
        {
            var envPath = Path.Combine(projectRoot, ".env.local");
            if (File.Exists(envPath))
            {
                foreach (var rawLine in File.ReadAllLines(envPath))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0) continue;

                    var currentKey = line.Substring(0, separatorIndex).Trim();
                    if (currentKey == key)
                    {
                        return line.Substring(separatorIndex + 1).Trim().Trim('"');
                    }
                }
            }
        }
#endif

        return null;
    }

    [Serializable]
    private class AppConfig
    {
        public string openAiApiKey;
    }
}