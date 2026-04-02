using System;
using System.IO;
using UnityEngine;

public static class LocalEnv
{
    public static string Get(string key)
    {
        //OSに設定されていたらそちらを優先する
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        //プロジェクトルートを探す
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            return null;
        }

        //.env.localを探す
        var envPath = Path.Combine(projectRoot, ".env.local");
        if (!File.Exists(envPath))
        {
            return null;
        }
        
        //.env.localを1行ずつ読んでAPIキーを探す
        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var currentKey = line.Substring(0, separatorIndex).Trim();
            if (!string.Equals(currentKey, key, StringComparison.Ordinal))
            {
                continue;
            }

            return line.Substring(separatorIndex + 1).Trim().Trim('"');
        }

        return null;
    }
}
