using System;

namespace Sdl.Web.Tridion.Templates.Tests
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string message) => Console.WriteLine($"DEBUG: {message}");
        public void Info(string message) => Console.WriteLine($"INFO: {message}");
        public void Warning(string message) => Console.WriteLine($"WARNING: {message}");
        public void Error(string message) => Console.WriteLine($"ERROR: {message}");
    }
}
