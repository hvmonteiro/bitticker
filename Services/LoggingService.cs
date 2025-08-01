/*
Copyright (c) 2025 Hugo Monteiro
Licensed under the MIT License. See LICENSE file in the project root for license information.
*/

using System;

namespace BitTicker
{
    public class LoggingService : ILoggingService
    {
        public event EventHandler<LogEntry>? LogEntryAdded;

        private void AddLogEntry(LogLevel level, string exchange, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Exchange = exchange ?? "System",
                Message = message ?? string.Empty
            };

            LogEntryAdded?.Invoke(this, entry);
        }

        public void LogInfo(string exchange, string message)
        {
            AddLogEntry(LogLevel.Info, exchange, message);
        }

        public void LogWarning(string exchange, string message)
        {
            AddLogEntry(LogLevel.Warning, exchange, message);
        }

        public void LogError(string exchange, string message)
        {
            AddLogEntry(LogLevel.Error, exchange, message);
        }

        public void LogDebug(string exchange, string message)
        {
            AddLogEntry(LogLevel.Debug, exchange, message);
        }

        public void LogHttpRequest(string exchange, string method, string url)
        {
            LogDebug(exchange, $"HTTP {method} → {url}");
        }

        public void LogHttpResponse(string exchange, int statusCode, string responseSize)
        {
            if (statusCode >= 200 && statusCode < 300)
                LogInfo(exchange, $"HTTP Response: {statusCode} ({responseSize})");
            else
                LogWarning(exchange, $"HTTP Response: {statusCode} ({responseSize})");
        }

        public void LogHttpError(string exchange, string url, string error)
        {
            LogError(exchange, $"HTTP Error for {url}: {error}");
        }

        public void Clear()
        {
            // This will be handled by the log window
        }
    }
}
