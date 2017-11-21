namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Extension methods for class <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        public static string NullIfEmpty(this string stringValue)
            => string.IsNullOrEmpty(stringValue) ? null : stringValue;
    }
}
