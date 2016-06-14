using System.Linq;
using DD4T.Templates.Base.Builder;
using Tridion.ContentManager.Templating;
using Dd4tComponentPresentation = DD4T.ContentModel.ComponentPresentation;
using TcmComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;

namespace Sdl.Web.Tridion.Templates.DD4T
{
    internal class DxaBuildManager : BuildManager
    {
        private const string ContextExpressionSectionName = "ContextExpressions";

        private readonly TemplatingLogger _logger = TemplatingLogger.GetLogger(typeof(DxaBuildManager));

        public DxaBuildManager(Package package, Engine engine)
            : base(package, engine)
        {
            // Minimize the amount of unneeded data in the DD4T JSON:
            BuildProperties.OmitFolders = true;
            BuildProperties.OmitCategories = true;
            BuildProperties.OmitCategories = true;
            BuildProperties.OmitContextPublications = true;
            BuildProperties.OmitOwningPublications = true;
        }

        public override Dd4tComponentPresentation BuildComponentPresentation(TcmComponentPresentation tcmComponentPresentation, Engine engine,
            int linkLevels, bool resolveWidthAndHeight)
        {
            Dd4tComponentPresentation result = base.BuildComponentPresentation(tcmComponentPresentation, engine, linkLevels, resolveWidthAndHeight);
            AddExtensionData(result, tcmComponentPresentation);
            return result;
        }

        private void AddExtensionData(Dd4tComponentPresentation dd4tComponentPresentation, TcmComponentPresentation tcmComponentPresentation)
        {
            if (!tcmComponentPresentation.Conditions.Any())
            {
                return;
            }
            _logger.Debug("ComponentPresentation has Conditions");

            string[] includeContextExpressions = ContextExpressionManager.GetContextExpressions(tcmComponentPresentation.Conditions.Where(c => !c.Negate).Select(c => c.TargetGroup));
            string[] excludeContextExpressions = ContextExpressionManager.GetContextExpressions(tcmComponentPresentation.Conditions.Where(c => c.Negate).Select(c => c.TargetGroup));

            if (includeContextExpressions.Any())
            {
                _logger.Debug("Adding Context Expression Conditions (Include): " + string.Join(", ", includeContextExpressions));
                dd4tComponentPresentation.AddExtensionProperty(ContextExpressionSectionName, "Include", includeContextExpressions);
            }

            if (excludeContextExpressions.Any())
            {
                _logger.Debug("Adding Context Expression Conditions (Exclude): " + string.Join(", ", excludeContextExpressions));
                dd4tComponentPresentation.AddExtensionProperty(ContextExpressionSectionName, "Exclude", excludeContextExpressions);
            }
        }

    }
}
