using DD4T.ContentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.ExternalContentLibrary.V2;

namespace DD4T.Templates.Base.Utils
{
    internal class EclProcessor : IDisposable
    {
        private readonly TemplatingLogger _log = TemplatingLogger.GetLogger(typeof(EclProcessor));
        private readonly Engine _engine;
        private readonly StructureGroup _binariesStructureGroup;
        private IEclSession _eclSession;

        //Empty list to prevent nullreference exection within the 'GetTemplateFragment(IList<ITemplateAttribute> attributes)' implementation inside the ECLConnector.
        private IList<ITemplateAttribute> _emptyTemplateAttributes = new List<ITemplateAttribute>();

        internal EclProcessor(Engine engine, Tridion.ContentManager.TcmUri binariesStructureGroupId)
        {
            _engine = engine;
            _binariesStructureGroup = (binariesStructureGroupId == null) ? null : (StructureGroup)engine.GetObject(binariesStructureGroupId);
            _eclSession = SessionFactory.CreateEclSession(engine.GetSession());
        }

        internal void ProcessEclStubComponent(Component eclStubComponent)
        {
            IContentLibraryContext eclContext;
            IContentLibraryMultimediaItem eclItem = GetEclItem(eclStubComponent.Id, out eclContext);

            // This may look a bit unusual, but we have to ensure that ECL Item members are accessed *before* the ECL Context is disposed.
            using (eclContext)
            {
                eclStubComponent.EclId = eclItem.Id.ToString();
                string directLinkToPublished = eclItem.GetDirectLinkToPublished(_emptyTemplateAttributes);
                eclStubComponent.Multimedia.Url = string.IsNullOrEmpty(directLinkToPublished) ? PublishBinaryContent(eclItem, eclStubComponent.Id) : directLinkToPublished;

                // Set additional ECL Item properties as ExtensionData on the ECL Stub Component.
                const string eclSectionName = "ECL";
                eclStubComponent.AddExtensionProperty(eclSectionName, "DisplayTypeId", eclItem.DisplayTypeId);
                eclStubComponent.AddExtensionProperty(eclSectionName, "MimeType", eclItem.MimeType);
                eclStubComponent.AddExtensionProperty(eclSectionName, "FileName", eclItem.Filename);
                eclStubComponent.AddExtensionProperty(eclSectionName, "TemplateFragment", eclItem.GetTemplateFragment(_emptyTemplateAttributes));

                IFieldSet eclExternalMetadataFieldSet = BuildExternalMetadataFieldSet(eclItem);
                if (eclExternalMetadataFieldSet != null)
                {
                    eclStubComponent.ExtensionData["ECL-ExternalMetadata"] = eclExternalMetadataFieldSet;
                }
            }
        }

        internal string ProcessEclXlink(XmlElement xlinkElement)
        {
            string eclStubComponentId = xlinkElement.GetAttribute("href", "http://www.w3.org/1999/xlink");

            IContentLibraryContext eclContext;
            IContentLibraryMultimediaItem eclItem = GetEclItem(eclStubComponentId, out eclContext);

            // This may look a bit unusual, but we have to ensure that ECL Item members are accessed *before* the ECL Context is disposed.
            using (eclContext)
            {
                // Set additional ECL Item properties as data attributes on the XLink element
                xlinkElement.SetAttribute("data-eclId", eclItem.Id.ToString());
                xlinkElement.SetAttribute("data-eclDisplayTypeId", eclItem.DisplayTypeId);
                if (!string.IsNullOrEmpty(eclItem.MimeType))
                {
                    xlinkElement.SetAttribute("data-eclMimeType", eclItem.MimeType);
                }
                if (!string.IsNullOrEmpty(eclItem.Filename))
                {
                    xlinkElement.SetAttribute("data-eclFileName", eclItem.Filename);
                }
                string eclTemplateFragment = eclItem.GetTemplateFragment(_emptyTemplateAttributes);
                if (!string.IsNullOrEmpty(eclTemplateFragment))
                {
                    // Note that the entire Template Fragment gets stuffed in an XHTML attribute.
                    // This may seem scary, but there is no limitation to the size of an XML attribute and the XLink element typically already has content.
                    xlinkElement.SetAttribute("data-eclTemplateFragment", eclTemplateFragment);
                }

                BuildExternalMetadataXmlAttributes(eclItem, xlinkElement);

                string directLinkToPublished = eclItem.GetDirectLinkToPublished(_emptyTemplateAttributes);
                return string.IsNullOrEmpty(directLinkToPublished) ? PublishBinaryContent(eclItem, eclStubComponentId) : directLinkToPublished;
            }
        }

        private IContentLibraryMultimediaItem GetEclItem(string eclStubComponentId, out IContentLibraryContext eclContext)
        {
            _log.Debug("Retrieving ECL item for ECL Stub Component: " + eclStubComponentId);
            IEclUri eclUri = _eclSession.TryGetEclUriFromTcmUri(eclStubComponentId);
            if (eclUri == null)
            {
                throw new Exception("Unable to get ECL URI for ECL Stub Component: " + eclStubComponentId);
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
                throw new Exception(string.Format("ECL item '{0}' not found (TCM URI: '{1}')", eclUri, eclStubComponentId));
            }

            _log.Debug(string.Format("Retrieved ECL item for ECL Stub Component '{0}': {1}", eclStubComponentId, eclUri));
            return eclItem;
        }

        private string PublishBinaryContent(IContentLibraryMultimediaItem eclItem, string eclStubComponentId)
        {
            IContentResult eclContent = eclItem.GetContent(null);
            string uniqueFilename = string.Format("{0}_{1}{2}",
                Path.GetFileNameWithoutExtension(eclItem.Filename), eclStubComponentId.Substring(4), Path.GetExtension(eclItem.Filename));

            Tridion.ContentManager.ContentManagement.Component eclStubComponent = (Tridion.ContentManager.ContentManagement.Component)_engine.GetObject(eclStubComponentId);
            Tridion.ContentManager.Publishing.Rendering.Binary binary = (_binariesStructureGroup == null) ?
                _engine.PublishingContext.RenderedItem.AddBinary(eclContent.Stream, uniqueFilename, string.Empty, eclStubComponent, eclContent.ContentType) :
                _engine.PublishingContext.RenderedItem.AddBinary(eclContent.Stream, uniqueFilename, _binariesStructureGroup, string.Empty, eclStubComponent, eclContent.ContentType);

            _log.Debug(string.Format("Added binary content of ECL Item '{0}' (Stub Component: '{1}', MimeType: '{2}') as '{3}' in '{4}'.",
                eclItem.Id, eclStubComponentId, eclContent.ContentType, binary.Url, (_binariesStructureGroup == null) ? "(default)" : _binariesStructureGroup.PublishPath));

            return binary.Url;
        }

        private void BuildExternalMetadataXmlAttributes(IContentLibraryItem eclItem, XmlElement xlinkElement)
        {
            string externalMetadataXml = eclItem.MetadataXml;
            if (string.IsNullOrEmpty(externalMetadataXml))
            {
                // No external metadata available; nothing to do.
                return;
            }

            ISchemaDefinition externalMetadataSchema = eclItem.MetadataXmlSchema;
            if (externalMetadataSchema == null)
            {
                _log.Warning(string.Format("ECL Item '{0}' has external metadata, but no schema defining it.", eclItem.Id));
                return;
            }

            try
            {
                XmlDocument externalMetadataDoc = new XmlDocument();
                externalMetadataDoc.LoadXml(externalMetadataXml);
                CreateExternalMetadataXmlAttribute(externalMetadataSchema.Fields, externalMetadataDoc.DocumentElement, xlinkElement); ;

                //_log.Debug(string.Format("ECL Item '{0}' has external metadata: {1}", eclItem.Id, string.Join(", ", result.Keys)));
            }
            catch (Exception ex)
            {
                _log.Error("An error occurred while parsing the external metadata for ECL Item " + eclItem.Id);
                _log.Error(ex.Message);
                return;
            }
        }

        private IFieldSet BuildExternalMetadataFieldSet(IContentLibraryItem eclItem)
        {
            string externalMetadataXml = eclItem.MetadataXml;
            if (string.IsNullOrEmpty(externalMetadataXml))
            {
                // No external metadata available; nothing to do.
                return null;
            }

            ISchemaDefinition externalMetadataSchema = eclItem.MetadataXmlSchema;
            if (externalMetadataSchema == null)
            {
                _log.Warning(string.Format("ECL Item '{0}' has external metadata, but no schema defining it.", eclItem.Id));
                return null;
            }

            try
            {
                XmlDocument externalMetadataDoc = new XmlDocument();
                externalMetadataDoc.LoadXml(externalMetadataXml);
                IFieldSet result = CreateExternalMetadataFieldSet(externalMetadataSchema.Fields, externalMetadataDoc.DocumentElement);

                _log.Debug(string.Format("ECL Item '{0}' has external metadata: {1}", eclItem.Id, string.Join(", ", result.Keys)));
                return result;
            }
            catch (Exception ex)
            {
                _log.Error("An error occurred while parsing the external metadata for ECL Item " + eclItem.Id);
                _log.Error(ex.Message);
                return null;
            }
        }

        private static FieldSet CreateExternalMetadataFieldSet(IEnumerable<IFieldDefinition> eclFieldDefinitions, XmlElement parentElement)
        {
            FieldSet fieldSet = new FieldSet();
            foreach (IFieldDefinition eclFieldDefinition in eclFieldDefinitions)
            {
                XmlNodeList fieldElements = parentElement.SelectNodes(string.Format("*[local-name()='{0}']", eclFieldDefinition.XmlElementName));
                if (fieldElements.Count == 0)
                {
                    // Don't generate a DD4T Field for ECL field without values.
                    continue;
                }

                Field field = new Field { Name = eclFieldDefinition.XmlElementName };
                foreach (XmlElement fieldElement in fieldElements)
                {
                    if (eclFieldDefinition is INumberFieldDefinition)
                    {
                        if (fieldElement.HasChildNodes)
                        {
                            field.NumericValues.Add(XmlConvert.ToDouble(fieldElement.InnerText));
                        }
                        field.FieldType = FieldType.Number;
                    }
                    else if (eclFieldDefinition is IDateFieldDefinition)
                    {
                        if (fieldElement.HasChildNodes)
                        {
                            field.DateTimeValues.Add(XmlConvert.ToDateTime(fieldElement.InnerText, XmlDateTimeSerializationMode.Unspecified));
                        }
                        field.FieldType = FieldType.Date;
                    }
                    else if (eclFieldDefinition is IFieldGroupDefinition)
                    {
                        if (field.EmbeddedValues == null)
                        {
                            field.EmbeddedValues = new List<FieldSet>();
                        }
                        IEnumerable<IFieldDefinition> embeddedFieldDefinitions = ((IFieldGroupDefinition)eclFieldDefinition).Fields;
                        field.EmbeddedValues.Add(CreateExternalMetadataFieldSet(embeddedFieldDefinitions, fieldElement));
                        field.FieldType = FieldType.Embedded;
                    }
                    else
                    {
                        field.Values.Add(fieldElement.InnerText);
                        if (eclFieldDefinition is IMultiLineTextFieldDefinition)
                        {
                            field.FieldType = FieldType.MultiLineText;
                        }
                        else if (eclFieldDefinition is IXhtmlFieldDefinition)
                        {
                            field.FieldType = FieldType.Xhtml;
                        }
                        else
                        {
                            field.FieldType = FieldType.Text;
                        }
                    }
                }

                fieldSet.Add(eclFieldDefinition.XmlElementName, field);
            }

            return fieldSet;
        }

        private static void CreateExternalMetadataXmlAttribute(IEnumerable<IFieldDefinition> eclFieldDefinitions, XmlElement parentElement, XmlElement xlinkElement)
        {
            foreach (IFieldDefinition eclFieldDefinition in eclFieldDefinitions)
            {
                XmlNodeList fieldElements = parentElement.SelectNodes(string.Format("*[local-name()='{0}']", eclFieldDefinition.XmlElementName));
                if (fieldElements.Count == 0)
                {
                    // Don't generate a DD4T Field for ECL field without values.
                    continue;
                }

                foreach (XmlElement fieldElement in fieldElements)
                {
                    if (fieldElement.HasChildNodes)
                    {
                        xlinkElement.SetAttribute(string.Format("data-{0}", eclFieldDefinition.XmlElementName), fieldElement.InnerText);
                    }
                }
            }
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