using Sdl.Web.Tridion.Templates.Common;
using System.Text;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using ComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Renders the component presentations to the package output. Useful when there is no page layout for publishing data
    /// </summary>
    [TcmTemplateTitle("Render Component Presentations")]
    public class RenderComponentPresentations : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            Page page = GetPage();
            if (page == null)
            {
                throw new DxaException("No Page found. This TBB should be used in a Page Template only.");
            }

            StringBuilder resultBuilder = new StringBuilder();
            foreach (ComponentPresentation cp in page.ComponentPresentations)
            {
                string renderedCp = engine.RenderComponentPresentation(cp.Component.Id, cp.ComponentTemplate.Id);
                renderedCp = StripTcdlComponentPresentationTag(renderedCp);
                resultBuilder.AppendLine(renderedCp);
            }

            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Text, resultBuilder.ToString()));
        }
    }
}
