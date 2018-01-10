using System;
using System.IO;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.DXAResolver
{
    internal class LogAdapter
    {
        private readonly TemplatingLogger _log;
        private readonly string _logFile;

        public LogAdapter(Type theType)
        {
            _log = TemplatingLogger.GetLogger(theType);
            try
            {
                string logging = Environment.GetEnvironmentVariable("DXA_LOGGING");
                if (!string.IsNullOrEmpty(logging))
                {
                    FileInfo fi = new FileInfo(logging);
                    _logFile = fi.FullName;
                    File.Delete(_logFile);
                }
            }
            catch (Exception)
            {
                // invalid so ignore
                _logFile = null;
            }
        }

        public void Debug(string msg)
        {
            _log.Debug(msg);
            if (string.IsNullOrEmpty(_logFile)) return;
            using (var sw = File.AppendText(_logFile))
            {
                sw.WriteLine(msg);
            }
        }
    }
}
