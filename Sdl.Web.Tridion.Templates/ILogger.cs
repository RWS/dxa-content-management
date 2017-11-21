namespace Sdl.Web.Tridion.Templates
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
