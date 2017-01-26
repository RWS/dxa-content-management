using System.Collections.Generic;
using System.IO;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.Tests
{
    internal class MockBinaryPublisher
    {
        internal const string PublishedUrlPrefix = "MockBinaryPublisher:";

        internal IList<Component> PublishedComponents { get; } = new List<Component>();

        internal string AddBinary(Component component)
        {
            PublishedComponents.Add(component);
            return PublishedUrlPrefix + component.Id;
        }

        internal string AddBinaryStream(Stream stream, string fileName, Component relatedComponent, string mimeType)
        {
            PublishedComponents.Add(relatedComponent);
            return PublishedUrlPrefix + relatedComponent.Id;
        }

    }
}
