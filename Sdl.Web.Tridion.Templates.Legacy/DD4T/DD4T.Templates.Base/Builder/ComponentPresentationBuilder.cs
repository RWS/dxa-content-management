using System;
using System.Collections.Generic;
using System.Text;
using Dynamic = DD4T.ContentModel;
using TCM = Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using System.Xml.Serialization;
using System.IO;
using DD4T.Templates.Base.Utils;
using DD4T.ContentModel.Contracts.Serializing;
using DD4T.Serialization;

namespace DD4T.Templates.Base.Builder
{
    public class ComponentPresentationBuilder
    {
        private static TemplatingLogger log = TemplatingLogger.GetLogger(typeof(ComponentPresentationBuilder));

        public static Dynamic.ComponentPresentation BuildComponentPresentation(TCM.ComponentPresentation tcmComponentPresentation, Engine engine, BuildManager manager)
        {
            TemplatingLogger logger = TemplatingLogger.GetLogger(typeof(ComponentPresentationBuilder));
            Dynamic.ComponentPresentation cp = new Dynamic.ComponentPresentation();

            logger.Debug(string.Format(">BuildCP {0} ({1})", tcmComponentPresentation.ComponentTemplate.Title, tcmComponentPresentation.ComponentTemplate.IsRepositoryPublishable));
            if (tcmComponentPresentation.ComponentTemplate.IsRepositoryPublishable)
            {
                // call render but ignore the output - render ensures componentlinking will be setup as normal.
                // don't bother with page flags because the end result is dynamically published so it needs to run with DCP settings
                engine.RenderComponentPresentation(tcmComponentPresentation.Component.Id, tcmComponentPresentation.ComponentTemplate.Id);

                // ignore the rendered CP, because it is already available in the broker
                // instead, we will render a very simple version without any links
                cp.Component = manager.BuildComponent(tcmComponentPresentation.Component, 0); // linkLevel = 0 means: only summarize the component
                cp.IsDynamic = true;
            }
            else
            {
                // render the component presentation using its own CT
                // but first, set a parameter in the context so that the CT will know it is beng called
                // from a DynamicDelivery page template
                if (engine.PublishingContext.RenderContext != null && !engine.PublishingContext.RenderContext.ContextVariables.Contains(BasePageTemplate.VariableNameCalledFromDynamicDelivery))
                {
                    engine.PublishingContext.RenderContext.ContextVariables.Add(BasePageTemplate.VariableNameCalledFromDynamicDelivery, BasePageTemplate.VariableValueCalledFromDynamicDelivery);
                }

                string renderedContent = engine.RenderComponentPresentation(tcmComponentPresentation.Component.Id, tcmComponentPresentation.ComponentTemplate.Id);
                renderedContent = TridionUtils.StripTcdlTags(renderedContent);

                // rendered content could contain si4t search data. if that's the case, the value of renderedCotnent is not valid DD4T data.
                // lets remove the si4t search data if that's the case. 
                string dd4tData = Si4tUtils.RemoveSearchData(renderedContent);
     
                try
                {
                    // we cannot be sure the component template uses the same serializer service as the page template
                    // so we will call a factory which can detect the correct service based on the content
                    ISerializerService serializerService = SerializerServiceFactory.FindSerializerServiceForContent(dd4tData);
                    cp = serializerService.Deserialize<Dynamic.ComponentPresentation>(dd4tData);

                    // inital renderedContent could contain si4t search data. we need to preserve the search data. 
                    // lets retrieve the si4t search data if that's the case and added to the renderedContent property
                    cp.RenderedContent = Si4tUtils.RetrieveSearchData(renderedContent);
                }
                catch (Exception e)
                {
                    log.Error("exception while deserializing into CP", e);
                    // the component presentation could not be deserialized, this probably not a Dynamic Delivery template
                    // just store the output as 'RenderedContent' on the CP
                    cp.RenderedContent = renderedContent;
                    // because the CT was not a DD4T CT, we will generate the DD4T XML code here
                    cp.Component = manager.BuildComponent(tcmComponentPresentation.Component);
                }
                cp.IsDynamic = false;
            }
            cp.ComponentTemplate = manager.BuildComponentTemplate(tcmComponentPresentation.ComponentTemplate);
            return cp;
        }
    }
}
