using System;
using System.IO;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion
{
    public class InternalLogger
    {
        private readonly TemplatingLogger _log;
        private readonly string _logFile = null;

        public InternalLogger(TemplatingLogger log)
        {
            _log = log;
            try
            {
                _logFile = Environment.GetEnvironmentVariable("DXA_LOGGING");
            }
            catch
            {
                // invalid so ignore
            }
        }

        public void Debug(string msg)
        {
            try
            {
                _log?.Debug(msg);
                if (string.IsNullOrEmpty(_logFile)) return;
                using (var sw = File.AppendText(_logFile))
                {
                    sw.WriteLine(msg);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
