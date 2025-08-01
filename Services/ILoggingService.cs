/*
Copyright (c) 2025 Hugo Monteiro

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;

namespace StockTicker
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public interface ILoggingService
    {
        event EventHandler<LogEntry> LogEntryAdded;
        void LogInfo(string exchange, string message);
        void LogWarning(string exchange, string message);
        void LogError(string exchange, string message);
        void LogDebug(string exchange, string message);
        void LogHttpRequest(string exchange, string method, string url);
        void LogHttpResponse(string exchange, int statusCode, string responseSize);
        void LogHttpError(string exchange, string url, string error);
        void Clear();
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Exchange { get; set; }
        public string Message { get; set; }
        public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] [{Level}] [{Exchange}] {Message}";
    }
}
