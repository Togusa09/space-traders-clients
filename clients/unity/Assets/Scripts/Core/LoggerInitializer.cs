using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;

namespace SpaceTraders.Core
{
    public static class LoggerInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            // Configure the default logger
            var config = new LoggerConfig();
            
            // Add a standard Console sink (Standard Unity Console)
            config.SaveToStdout();
            
            // Optional: You could add a File sink or others here
            // config.SaveToFile("logs/game.log");

            Log.Logger = config.CreateLogger();
            
            Log.Info("Structured Logging initialized.");
        }

        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Redact bearer tokens or general "token":"..." patterns
            string sanitized = System.Text.RegularExpressions.Regex.Replace(
                text,
                "\"token\"\\s*:\\s*\"[^\"]+\"",
                "\"token\":\"[REDACTED]\""
            );
            
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                "Bearer\\s+[A-Za-z0-9-_=]+\\.[A-Za-z0-9-_=]+\\.[A-Za-z0-9-_.+/=]+",
                "Bearer [REDACTED]"
            );
            
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                "Authorization:\\s*Bearer\\s+[^\\s]+",
                "Authorization: Bearer [REDACTED]"
            );

            return sanitized;
        }
    }
}
