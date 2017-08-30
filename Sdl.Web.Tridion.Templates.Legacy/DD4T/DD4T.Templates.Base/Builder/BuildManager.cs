using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCM = Tridion.ContentManager.ContentManagement;
using Dynamic = DD4T.ContentModel;
using TComm = Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using DD4T.ContentModel.Contracts.Serializing;
using DD4T.Templates.Base.Utils;
using DD4T.ContentModel;

namespace DD4T.Templates.Base.Builder
{
    /// <summary>
    /// Class serves as reference point to all builders, allowing subclasses of the BuildManager to override
    /// specific points of implementation. In a way, this class provides a poor man's dependency injection.
    /// </summary>
    public class BuildManager
    {
        // I think this should go! (qs)
        //public BuildManager()
        //{
        //    BuildProperties = new BuildProperties(null);
        //}
        public BuildManager (Package package, Engine engine)
        {
            BuildProperties = new BuildProperties(package);
            BinaryPublisher = new BinaryPublisher(package, engine);
        }
        protected BinaryPublisher BinaryPublisher
        {
            get; set;
        }

        public BuildProperties BuildProperties { get; set; }
        public ISerializerService SerializerService { get; set; }

        public virtual Dynamic.Page BuildPage(TComm.Page tcmPage, Engine engine)
        {
            return PageBuilder.BuildPage(tcmPage, engine, this);
        }

        public virtual List<Dynamic.Category> BuildCategories(TComm.Page page)
        {
            return CategoriesBuilder.BuildCategories(page,this);
        }

        public virtual List<Dynamic.Category> BuildCategories(TCM.Component component)
        {
            return CategoriesBuilder.BuildCategories(component, this);
        }

        public virtual Dynamic.Component BuildComponent(TCM.Component tcmComponent)
        {
            return ComponentBuilder.BuildComponent(tcmComponent, this);
		}

        public virtual Dynamic.Component BuildComponent(TCM.Component tcmComponent, int currentLinkLevel)
        {
            return ComponentBuilder.BuildComponent(tcmComponent, currentLinkLevel, this);
        }

        public virtual Dynamic.ComponentPresentation BuildComponentPresentation(TComm.ComponentPresentation tcmComponentPresentation, Engine engine, int linkLevels, bool resolveWidthAndHeight)
        {
            return ComponentPresentationBuilder.BuildComponentPresentation(tcmComponentPresentation, engine, this);
        }

        public virtual Dynamic.ComponentTemplate BuildComponentTemplate(TComm.ComponentTemplate tcmComponentTemplate)
        {
            return ComponentTemplateBuilder.BuildComponentTemplate(tcmComponentTemplate, this);
        }

        public virtual Dynamic.Field BuildField(TCM.Fields.ItemField tcmItemField, int currentLinkLevel)
        {
            return FieldBuilder.BuildField(tcmItemField, currentLinkLevel, this);
        }

        public virtual Dynamic.FieldSet BuildFields(TCM.Fields.ItemFields tcmItemFields)
        {
            return FieldsBuilder.BuildFields(tcmItemFields, this.BuildProperties.LinkLevels, this);
        }

        public virtual Dynamic.FieldSet BuildFields(TCM.Fields.ItemFields tcmItemFields, int currentLinkLevel)
        {
            return FieldsBuilder.BuildFields(tcmItemFields, currentLinkLevel, this);
        }


        public Dynamic.Keyword BuildKeyword(TCM.Keyword keyword)
        {
            return KeywordBuilder.BuildKeyword(keyword, 2, this);
        }

        public Dynamic.Keyword BuildKeyword(TCM.Keyword keyword, int linkLevels)
        {
            return KeywordBuilder.BuildKeyword(keyword, linkLevels, this);
        }

        public virtual Dynamic.OrganizationalItem BuildOrganizationalItem(TComm.StructureGroup tcmStructureGroup)
        {
            return OrganizationalItemBuilder.BuildOrganizationalItem(tcmStructureGroup);
		}

        public virtual Dynamic.OrganizationalItem BuildOrganizationalItem(TCM.Folder tcmFolder)
        {
            return OrganizationalItemBuilder.BuildOrganizationalItem(tcmFolder);
        }

        public virtual Dynamic.PageTemplate BuildPageTemplate(TComm.PageTemplate tcmPageTemplate)
        {
            return PageTemplateBuilder.BuildPageTemplate(tcmPageTemplate, this);
        }

        public virtual Dynamic.Publication BuildPublication(TCM.Repository tcmPublication)
        {
            return PublicationBuilder.BuildPublication(tcmPublication);
        }

        public virtual Dynamic.Schema BuildSchema(TCM.Schema tcmSchema)
        {
            return SchemaBuilder.BuildSchema(tcmSchema, this);
        }
        public virtual void AddXpathToFields(Dynamic.FieldSet fieldSet, string baseXpath)
        {
            FieldsBuilder.AddXpathToFields(fieldSet, baseXpath);
        }

        public virtual void PublishMultimediaComponent(Component component)
        {
            BinaryPublisher.PublishMultimediaComponent(component, BuildProperties);
        }

        public virtual string PublishBinariesInRichTextField(string v)
        {
            return BinaryPublisher.PublishBinariesInRichTextField(v, BuildProperties);
        }
    }
}
