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
            Log.Logger = new LoggerConfig()
                .MinimumLevel.Debug()
                .WriteTo.UnityDebugLog()
                .CreateLogger();
            
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
