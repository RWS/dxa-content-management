using System;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.Templates.Tests
{
    public class TestLogger : ILogger
    {
        public IList<LogMessage> LoggedMessages { get; } = new List<LogMessage>();

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message) => Log(LogLevel.Error, message);

        private void Log(LogLevel logLevel, string message)
        {
            LogMessage logMessage = new LogMessage(logLevel, message);
            LoggedMessages.Add(logMessage);
            Console.WriteLine(logMessage);
        }
    }

    public class LogMessage
    {
        public LogLevel LogLevel { get; }
        public string Message { get; }

        public LogMessage(LogLevel logLevel, string message)
        {
            LogLevel = logLevel;
            Message = message;
        }

        public override string ToString()
            => $"{LogLevel.ToString().ToUpper()}: {Message}";

        public override bool Equals(object obj)
        {
            LogMessage other = obj as LogMessage;
            return (other != null) && (other.LogLevel == LogLevel) && (other.Message == Message);
        }

        public override int GetHashCode()
            => LogLevel.GetHashCode() ^ Message.GetHashCode();
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    };
}
