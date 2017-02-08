using System;
using System.Linq;
using DD4T.ContentModel;
using DD4T.Templates.Base.Builder;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Templating;
using Dd4tComponentPresentation = DD4T.ContentModel.ComponentPresentation;
using TcmComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;
using TcmKeyword = Tridion.ContentManager.ContentManagement.Keyword;

namespace Sdl.Web.Tridion.Templates.Legacy.DD4T
{
    internal class DxaBuildManager : BuildManager
    {
        private const string ContextExpressionSectionName = "ContextExpressions";
        private const string DxaExtensionDataSectionName = "DXA";

        private readonly TemplatingLogger _logger = TemplatingLogger.GetLogger(typeof(DxaBuildManager));

        public DxaBuildManager(Package package, Engine engine)
            : base(package, engine)
        {
            // Minimize the amount of unneeded data in the DD4T JSON:
            BuildProperties.OmitFolders = true;
            BuildProperties.OmitCategories = true;
            BuildProperties.OmitContextPublications = true;
            BuildProperties.OmitOwningPublications = true;
            BuildProperties.OmitValueLists = true;

            BuildProperties.ECLEnabled = true; // ECL is only used for ECL Stubs

            string expandLinkDepth = package.GetValue("expandLinkDepth");
            _logger.Debug(string.Format("expandLinkDepth: {0}", expandLinkDepth));
            if (!string.IsNullOrEmpty(expandLinkDepth))
            {
                BuildProperties.LinkLevels = Convert.ToInt32(expandLinkDepth);
            }
        }

        public override Dd4tComponentPresentation BuildComponentPresentation(TcmComponentPresentation tcmComponentPresentation, Engine engine,
            int linkLevels, bool resolveWidthAndHeight)
        {
            Dd4tComponentPresentation result = base.BuildComponentPresentation(tcmComponentPresentation, engine, linkLevels, resolveWidthAndHeight);
            AddCpExtensionData(result, tcmComponentPresentation);
            return result;
        }

        public override Field BuildField(ItemField tcmItemField, int currentLinkLevel)
        {
            Field dd4tField = base.BuildField(tcmItemField, currentLinkLevel);

            KeywordField tcmKeywordField = tcmItemField as KeywordField;
            if (tcmKeywordField != null)
            {
                int i = 0;
                foreach (Keyword dd4tKeyword in dd4tField.KeywordValues)
                {
                    TcmKeyword tcmKeyword = tcmKeywordField.Values[i++];
                    if (tcmKeyword.MetadataSchema != null)
                    {
                        dd4tKeyword.AddExtensionProperty(DxaExtensionDataSectionName, "MetadataSchemaId", tcmKeyword.MetadataSchema.Id);
                    }
                }
            }

            return dd4tField;
        }

        private void AddCpExtensionData(Dd4tComponentPresentation dd4tComponentPresentation, TcmComponentPresentation tcmComponentPresentation)
        {
            if (!tcmComponentPresentation.Conditions.Any())
            {
                return;
            }
            _logger.Debug("ComponentPresentation has Conditions");

            string[] includeContextExpressions = ContextExpressionUtils.GetContextExpressions(tcmComponentPresentation.Conditions.Where(c => !c.Negate).Select(c => c.TargetGroup));
            string[] excludeContextExpressions = ContextExpressionUtils.GetContextExpressions(tcmComponentPresentation.Conditions.Where(c => c.Negate).Select(c => c.TargetGroup));

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
