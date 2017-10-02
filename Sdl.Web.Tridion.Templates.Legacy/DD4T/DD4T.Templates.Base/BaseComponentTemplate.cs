using DD4T.ContentModel.Contracts.Serializing;
using DD4T.Serialization;
using DD4T.Templates.Base.Builder;
using DD4T.Templates.Base.Utils;
using System;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Templating;
using Dynamic = DD4T.ContentModel;

namespace DD4T.Templates.Base
{
    public enum ComponentPresentationRenderStyle { Component, ComponentPresentation }
    public abstract class BaseComponentTemplate : BaseTemplate
    {
       
        DateTime startTime = DateTime.Now;
        private ComponentPresentationRenderStyle _componentPresentationRenderStyle = ComponentPresentationRenderStyle.Component;
        public virtual ComponentPresentationRenderStyle ComponentPresentationRenderStyle 
        {
            get
            {
                Item renderStyle = Package.GetByName("render-style");
                if (renderStyle != null) // another template has set a render style, let's use that!
                {
                    ComponentPresentationRenderStyle cprs = (ComponentPresentationRenderStyle)Enum.Parse(typeof(ComponentPresentationRenderStyle), renderStyle.GetAsString());
                    return cprs;
                }
                return _componentPresentationRenderStyle;
            }
            protected set
            {
                _componentPresentationRenderStyle = value;
            }
        }

        public BaseComponentTemplate()
            : base(TemplatingLogger.GetLogger(typeof(BaseComponentTemplate)))
        {
           
        }

        public BaseComponentTemplate(TemplatingLogger log) : base(log) { }

        /// <summary>
        /// Abstract method to be implemented by a subclass. The method takes a DynamicDelivery component and can add information to it (e.g. by searching in folders / structure groups / linked components, etc
        /// </summary>
        /// <param name="component">DynamicDelivery component </param>
        protected abstract void TransformComponent(Dynamic.Component component);

        public override void Transform(Engine engine, Package package)
        {
            GeneralUtils.TimedLog("started Transform");
            this.Package = package;
            this.Engine = engine;

            object component;
            bool hasOutput = HasPackageValue(package, "Output");
            if (hasOutput)
            {
                String inputValue = package.GetValue("Output");
                if (ComponentPresentationRenderStyle == Base.ComponentPresentationRenderStyle.Component)
                {
                    component = (Dynamic.Component)SerializerService.Deserialize<Dynamic.Component>(inputValue);
                }
                else
                {
                    component = (Dynamic.ComponentPresentation)SerializerService.Deserialize<Dynamic.ComponentPresentation>(inputValue);
                }
            }
            else
            {
                if (ComponentPresentationRenderStyle == Base.ComponentPresentationRenderStyle.Component)
                {
                    component = GetDynamicComponent();
                }
                else
                {
                    component = GetDynamicComponentPresentation();
                }
            }

            try
            {
                TransformComponent(ComponentPresentationRenderStyle == Base.ComponentPresentationRenderStyle.Component ? (Dynamic.Component) component : ((Dynamic.ComponentPresentation)component).Component);
            }
            catch (StopChainException)
            {
                Log.Debug("caught stopchainexception, will not write current component back to the package");
                return;
            }

            string outputValue = ComponentPresentationRenderStyle == Base.ComponentPresentationRenderStyle.Component ? SerializerService.Serialize<Dynamic.Component>((Dynamic.Component)component) : SerializerService.Serialize<Dynamic.ComponentPresentation>((Dynamic.ComponentPresentation)component);
            if (hasOutput)
            {
                Item outputItem = package.GetByName("Output");
                outputItem.SetAsString(outputValue);
            }
            else
            {
                package.PushItem(Package.OutputName, package.CreateStringItem(SerializerService is XmlSerializerService ? ContentType.Xml : ContentType.Text, outputValue));
            }

            GeneralUtils.TimedLog("finished Transform");
        }

        private Dynamic.Component GetDynamicComponent()
        {
            Item item = Package.GetByName(Package.ComponentName);
            if (item == null)
            {
                Log.Error("no component found (is this a page template?)");
                return null;
            }

            Component tcmComponent = (Component)Engine.GetObject(item);
            Dynamic.Component component = Manager.BuildComponent(tcmComponent);
            EnsureExtraProperties(component,tcmComponent);
            return component;
        }

        private Dynamic.ComponentPresentation GetDynamicComponentPresentation()
        {

            Template template = Engine.PublishingContext.ResolvedItem.Template;
            if (! (template is ComponentTemplate))
            {
                Log.Error("no component template found (is this a page template?)");
                return null;
            }
            ComponentTemplate tcmComponentTemplate = (ComponentTemplate)template;
            Item item = Package.GetByName(Package.ComponentName);
            if (item == null)
            {
                Log.Error("no component found (is this a page template?)");
                return null;
            }
            Component tcmComponent = (Component)Engine.GetObject(item);

            Dynamic.Component component = Manager.BuildComponent(tcmComponent);
            EnsureExtraProperties(component,tcmComponent);
            Dynamic.ComponentTemplate componentTemplate = Manager.BuildComponentTemplate(tcmComponentTemplate);
            Dynamic.ComponentPresentation componentPresentation = new Dynamic.ComponentPresentation() { Component = component, ComponentTemplate = componentTemplate, IsDynamic = tcmComponentTemplate.IsRepositoryPublishable };

            return componentPresentation;
        }

        private void EnsureExtraProperties(Dynamic.Component component, Component tcmComponent)
        {
            // make sure that the Publication, Folder and OwningPublication are always available on the top level
            if (component.Publication == null)
            {
                component.Publication = Manager.BuildPublication(tcmComponent.ContextRepository);
            }
            if (component.OwningPublication == null)
            {
                component.OwningPublication = Manager.BuildPublication(tcmComponent.OwningRepository);
            }
            if (component.Folder == null)
            {
                component.Folder = Manager.BuildOrganizationalItem((Folder)tcmComponent.OrganizationalItem);
            }
        }

        protected Component GetTcmComponent()
        {
            Item item = Package.GetByName(Package.ComponentName);
            if (item == null)
            {
                Log.Error("no component found (is this a page template?)");
                return null;
            }

            return (Component)Engine.GetObject(item.GetAsSource().GetValue("ID"));
        }
    }
}