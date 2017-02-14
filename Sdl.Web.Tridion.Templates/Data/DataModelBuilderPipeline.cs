using System;
using System.Collections.Generic;
using Sdl.Web.DataModel;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using ComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Represents the pipeline of Page/Entity Data Model Builders.
    /// </summary>
    public class DataModelBuilderPipeline
    {
        private readonly IList<IPageModelDataBuilder> _pageModelBuilders = new List<IPageModelDataBuilder>();
        private readonly IList<IEntityModelDataBuilder> _entityModelBuilders = new List<IEntityModelDataBuilder>();

        /// <summary>
        /// Gets the current CM Session.
        /// </summary>
        public Session Session { get; }

        /// <summary>
        /// Gets the context <see cref="RenderedItem"/> instance which is used to add Binaries and child Rendered Items.
        /// </summary>
        public RenderedItem RenderedItem { get; }

        /// <summary>
        /// Gets the current Model Builder settings.
        /// </summary>
        public DataModelBuilderSettings Settings { get; }

        /// <summary>
        /// Gets the logger used by the Model Builder pipeline.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="renderedItem">A context <see cref="RenderedItem"/> instance which is used to add Binaries and child Rendered Items.</param>
        /// <param name="settings">The Model Builder Settings to use.</param>
        /// <param name="modelBuilderTypeNames">The (qualified) type names of Model Builders to use.</param>
        /// <param name="logger">Optional logger to use. If not specified or <c>null</c>, the Data Model Builder Pipeline creates its own logger.</param>
        public DataModelBuilderPipeline(
            RenderedItem renderedItem,
            DataModelBuilderSettings settings,
            IEnumerable<string> modelBuilderTypeNames,
            ILogger logger = null
            )
        {
            Session = renderedItem.ResolvedItem.Item.Session;
            RenderedItem = renderedItem;
            Settings = settings;
            Logger = logger ?? new TemplatingLoggerAdapter(TemplatingLogger.GetLogger(GetType()));

            foreach (string modelBuilderTypeName in modelBuilderTypeNames)
            {
                string qualifiedTypeName = modelBuilderTypeName.Contains(".") ? modelBuilderTypeName : $"Sdl.Web.Tridion.Data.{modelBuilderTypeName}";
                Type modelBuilderType = Type.GetType(qualifiedTypeName, throwOnError: true);
                object modelBuilder = Activator.CreateInstance(modelBuilderType, new object[] { this });
                IPageModelDataBuilder pageModelBuilder = modelBuilder as IPageModelDataBuilder;
                IEntityModelDataBuilder entityModelBuilder = modelBuilder as IEntityModelDataBuilder;
                if ((pageModelBuilder == null) && (entityModelBuilder == null))
                {
                    Logger.Warning($"Configured Model Builder type '{modelBuilderType.FullName}' does not implement IPageModelDataBuilder nor IEntityModelDataBuilder; skipping.");
                    continue;
                }
                if (pageModelBuilder != null)
                {
                    Logger.Debug($"Using Page Model Builder type '{modelBuilderType.FullName}'.");
                    _pageModelBuilders.Add(pageModelBuilder);
                }
                if (entityModelBuilder != null)
                {
                    Logger.Debug($"Using Entity Model Builder type '{modelBuilderType.FullName}'.");
                    _entityModelBuilders.Add(entityModelBuilder);
                }
            }
        }

        /// <summary>
        /// Creates a Page Data Model from a given CM Page object.
        /// </summary>
        /// <param name="page">The CM Page.</param>
        public PageModelData CreatePageModel(Page page)
        {
            PageModelData pageModelData = null;
            foreach (IPageModelDataBuilder pageModelBuilder in _pageModelBuilders)
            {
                pageModelBuilder.BuildPageModel(ref pageModelData, page);
            }
            return pageModelData;
        }

        /// <summary>
        /// Creates an Entity Data Model from a given CM Component Presentation object.
        /// </summary>
        /// <param name="cp">The CM Component Presentation.</param>
        public EntityModelData CreateEntityModel(ComponentPresentation cp)
        {
            EntityModelData entityModelData = null;
            foreach (IEntityModelDataBuilder entityModelBuilder in _entityModelBuilders)
            {
                entityModelBuilder.BuildEntityModel(ref entityModelData, cp);
            }
            return entityModelData;
        }

        /// <summary>
        /// Creates an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        public EntityModelData CreateEntityModel(Component component, ComponentTemplate ct)
        {
            EntityModelData entityModelData = null;
            foreach (IEntityModelDataBuilder entityModelBuilder in _entityModelBuilders)
            {
                entityModelBuilder.BuildEntityModel(ref entityModelData, component, ct);
            }
            return entityModelData;
        }
    }
}
