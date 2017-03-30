using System.Reflection;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Tests
{
    /// <summary>
    /// Templating Engine for unit/integration testing purposes
    /// </summary>
    /// <remarks>
    /// It is currently impossible to inherit directly from abstract base class Engine (you would have to override an abstract internal member),
    /// hence we inherit from the concrete TemplatingRenderer subclass here.
    /// We don't actually use that class' public Render method, though.
    /// </remarks>
    internal class TestEngine : TemplatingRenderer
    {
        internal TestEngine(RenderedItem renderedItem)
        {
            _session = renderedItem.ResolvedItem.Session;

            // Using reflection to set the private field TemplatingRenderer._renderedItem too (otherwise you get an error that the Engine is not initialized):
            FieldInfo renderedItemField = GetType().BaseType.GetField("_renderedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            renderedItemField?.SetValue(this, renderedItem);
        }
    }
}
