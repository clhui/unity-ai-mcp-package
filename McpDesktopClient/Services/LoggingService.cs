using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace McpDesktopClient.Services
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public Exception? Exception { get; set; }

        public LogEntry(LogLevel level, string category, string message, Exception? exception = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Category = category ?? "General";
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{Level.ToString().ToUpper()}] ");
            sb.Append($"[{Category}] ");
            sb.Append(Message);
            
            if (Exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {Exception.Message}");
                if (!string.IsNullOrEmpty(Exception.StackTrace))
                {
                    sb.AppendLine();
                    sb.Append($"StackTrace: {Exception.StackTrace}");
                }
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// 日志记录服务
    /// </summary>
    public class LoggingService
    {
        private readonly List<LogEntry> _logEntries;
        private readonly object _lockObject;
        private readonly string _logFilePath;
        private readonly int _maxLogEntries;
        
        public event EventHandler<LogEntry> LogEntryAdded;
        
        public LoggingService(int maxLogEntries = 1000)
        {
            _logEntries = new List<LogEntry>();
            _lockObject = new object();
            _maxLogEntries = maxLogEntries;
            
            // 设置日志文件路径到项目目录下
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logDirectory = Path.Combine(currentDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"mcp_client_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void LogDebug(string category, string message)
        {
            AddLogEntry(LogLevel.Debug, category, message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void LogInfo(string category, string message)
        {
            AddLogEntry(LogLevel.Info, category, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void LogWarning(string category, string message)
        {
            AddLogEntry(LogLevel.Warning, category, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(string category, string message, Exception? exception = null)
        {
            AddLogEntry(LogLevel.Error, category, message, exception);
        }

        /// <summary>
        /// 添加日志条目
        /// </summary>
        private void AddLogEntry(LogLevel level, string category, string message, Exception? exception = null)
        {
            var logEntry = new LogEntry(level, category, message, exception);
            
            lock (_lockObject)
            {
                _logEntries.Add(logEntry);
                
                // 限制内存中的日志条目数量
                if (_logEntries.Count > _maxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
            }
            
            // 异步写入文件
            Task.Run(() => WriteToFile(logEntry));
            
            // 触发事件
            LogEntryAdded?.Invoke(this, logEntry);
        }

        /// <summary>
        /// 写入日志文件
        /// </summary>
        private async Task WriteToFile(LogEntry logEntry)
        {
            try
            {
                var logText = logEntry.ToString() + Environment.NewLine;
                await File.AppendAllTextAsync(_logFilePath, logText, Encoding.UTF8);
            }
            catch
            {
                // 忽略文件写入错误，避免日志记录本身导致异常
            }
        }

        /// <summary>
        /// 获取所有日志条目
        /// </summary>
        public List<LogEntry> GetLogEntries()
        {
            lock (_lockObject)
            {
                return new List<LogEntry>(_logEntries);
            }
        }

        /// <summary>
        /// 获取指定级别的日志条目
        /// </summary>
        public List<LogEntry> GetLogEntries(LogLevel level)
        {
            lock (_lockObject)
            {
                return _logEntries.FindAll(entry => entry.Level == level);
            }
        }

        /// <summary>
        /// 获取指定类别的日志条目
        /// </summary>
        public List<LogEntry> GetLogEntries(string category)
        {
            lock (_lockObject)
            {
                return _logEntries.FindAll(entry => entry.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// 清空内存中的日志
        /// </summary>
        public void ClearLogs()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// 导出日志到指定文件
        /// </summary>
        public async Task ExportLogsAsync(string filePath)
        {
            var logs = GetLogEntries();
            var sb = new StringBuilder();
            
            foreach (var log in logs)
            {
                sb.AppendLine(log.ToString());
            }
            
            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}