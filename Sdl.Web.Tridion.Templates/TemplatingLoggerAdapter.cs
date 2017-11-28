using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates
{
    public class TemplatingLoggerAdapter : ILogger
    {
        private readonly TemplatingLogger _logger;

        public TemplatingLoggerAdapter(TemplatingLogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Warning(string message) => _logger.Warning(message);
        public void Error(string message) => _logger.Error(message);
    }
}
