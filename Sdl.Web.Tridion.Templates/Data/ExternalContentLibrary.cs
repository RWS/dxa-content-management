using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion.ContentManager.ContentManagement;
using Tridion.ExternalContentLibrary.V2;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Wrapper class of ECL API access to prevent runtime errors on systems which don't have ECL installed.
    /// </summary>
    internal class ExternalContentLibrary : IDisposable
    {
        private static readonly IList<ITemplateAttribute> _emptyAttributes = new List<ITemplateAttribute>();
        private readonly DataModelBuilderPipeline _pipeline;
        private IEclSession _eclSession;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context Model Builder Pipeline.</param>
        internal ExternalContentLibrary(DataModelBuilderPipeline pipeline)
        {
            _pipeline = pipeline;
            _eclSession = SessionFactory.CreateEclSession(pipeline.Session);
        }

        internal XmlElement BuildEntityModel(EntityModelData entityModelData, Component eclStubComponent)
        {
            IContentLibraryContext eclContext;
            IContentLibraryMultimediaItem eclItem = GetEclItem(eclStubComponent.Id, out eclContext);

            // This may look a bit unusual, but we have to ensure that ECL Item members are accessed *before* the ECL Context is disposed.
            using (eclContext)
            {
                BinaryContent eclStubBinaryContent = eclStubComponent.BinaryContent;

                string directLinkToPublished = eclItem.GetDirectLinkToPublished(_emptyAttributes);

                entityModelData.BinaryContent = new BinaryContentData
                {
                    Url = string.IsNullOrEmpty(directLinkToPublished) ? PublishBinaryContent(eclItem, eclStubComponent) : directLinkToPublished,
                    MimeType = eclItem.MimeType ?? eclStubBinaryContent.MultimediaType.MimeType,
                    FileName = eclItem.Filename ?? eclStubBinaryContent.Filename,
                    FileSize = eclStubComponent.BinaryContent.Size
                };

                XmlElement externalMetadata = null;
                if (!string.IsNullOrEmpty(eclItem.MetadataXml))
                {
                    XmlDocument externalMetadataDoc = new XmlDocument();
                    externalMetadataDoc.LoadXml(eclItem.MetadataXml);
                    externalMetadata = externalMetadataDoc.DocumentElement;
                }

                entityModelData.ExternalContent = new ExternalContentData
                {
                    Id = eclItem.Id.ToString(),
                    DisplayTypeId = eclItem.DisplayTypeId,
                    TemplateFragment = eclItem.GetTemplateFragment(_emptyAttributes)
                    // Note: not setting Metadata here, but returning the external metadata as raw XML.
                };

                return externalMetadata;
            }
        }

        private IContentLibraryMultimediaItem GetEclItem(string eclStubComponentId, out IContentLibraryContext eclContext)
        {
            IEclUri eclUri = _eclSession.TryGetEclUriFromTcmUri(eclStubComponentId);
            if (eclUri == null)
            {
                throw new DxaException("Unable to get ECL URI for ECL Stub Component: " + eclStubComponentId);
            }

            eclContext = _eclSession.GetContentLibrary(eclUri);
            // This is done this way to not have an exception thrown through GetItem, as stated in the ECL API doc.
            // The reason to do this, is because if there is an exception, the ServiceChannel is going into the aborted state.
            // GetItems allows up to 20 (depending on config) connections. 
            IList<IContentLibraryItem> eclItems = eclContext.GetItems(new[] { eclUri });
            IContentLibraryMultimediaItem eclItem = (eclItems == null) ? null : eclItems.OfType<IContentLibraryMultimediaItem>().FirstOrDefault();
            if (eclItem == null)
            {
                eclContext.Dispose();
                throw new DxaException($"ECL item '{eclUri}' not found (TCM URI: '{eclStubComponentId}')");
            }

            return eclItem;
        }

        private string PublishBinaryContent(IContentLibraryMultimediaItem eclItem, Component eclStubComponent)
        {
            IContentResult eclContent = eclItem.GetContent(_emptyAttributes);
            string uniqueFilename =
                $"{Path.GetFileNameWithoutExtension(eclItem.Filename)}_{eclStubComponent.Id.ToString().Substring(4)}{Path.GetExtension(eclItem.Filename)}";

            return _pipeline.RenderedItem.AddBinary(eclContent.Stream, uniqueFilename, string.Empty, eclStubComponent, eclContent.ContentType).Url;
        }

        public void Dispose()
        {
            if (_eclSession != null)
            {
                _eclSession.Dispose();
                _eclSession = null;
            }
        }
    }
}
