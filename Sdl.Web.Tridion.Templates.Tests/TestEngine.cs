using System;
using System.Reflection;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Publishing.Resolving;
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
            
            // Ensuring TestEngine has mocked PublishingContext
            var ctor = typeof(PublishingContext).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, 
                new Type[] {typeof(ResolvedItem), typeof(PublishInstruction), typeof(PublicationTarget), typeof(RenderedItem), typeof(RenderContext)}, null);
            var publishingContextInstance = (PublishingContext)ctor.Invoke(new object[] {null, null, null, renderedItem, null});

            var setPublishingContext = typeof(Engine).GetMethod("SetPublishingContext", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any,
                new Type[] { typeof(PublishingContext) }, null);
            setPublishingContext.Invoke(this, new[] { publishingContextInstance });

            // Using reflection to set the private field TemplatingRenderer._renderedItem too (otherwise you get an error that the Engine is not initialized):
            FieldInfo renderedItemField = GetType().BaseType.GetField("_renderedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            
            renderedItemField?.SetValue(this, renderedItem);
        }
    }
}
