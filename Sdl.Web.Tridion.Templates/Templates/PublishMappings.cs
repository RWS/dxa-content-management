using System.Linq;
using Sdl.Web.Tridion.Templates.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Sdl.Web.DataModel.Configuration;
using Tridion;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Publishes schema and region mapping information in JSON format
    /// </summary>
    [TcmTemplateTitle("Publish Mappings")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.PublishMappingsParameters.xsd")]
    public class PublishMappings : TemplateBase
    {
        private const string InvalidCharactersPattern = @"[^A-Za-z0-9.]+";
        private const string VocabulariesAppDataId = "http://www.sdl.com/tridion/SemanticMapping/vocabularies";
        private const string TypeOfAppDataId = "http://www.sdl.com/tridion/SemanticMapping/typeof";
        private const string CoreVocabularyPrefix = "tri";
        private const string CoreVocabulary = "http://www.sdl.com/web/schemas/core";

        private static readonly XmlNamespaceManager _namespaceManager = new XmlNamespaceManager(new NameTable());
        private static readonly IDictionary<string, string> _namespaces = new Dictionary<string, string>
        {
            {Constants.XsdPrefix, Constants.XsdNamespace},
            {Constants.TcmPrefix, Constants.TcmNamespace},
            {Constants.XlinkPrefix, Constants.XlinkNamespace},
            {Constants.XhtmlPrefix, Constants.XhtmlNamespace},
            {"tcmi","http://www.tridion.com/ContentManager/5.0/Instance"},
            {"mapping", "http://www.sdl.com/tridion/SemanticMapping"}
        };

        private static readonly IDictionary<Type, FieldType> _fieldTypes = new Dictionary<Type, FieldType>
        {
            {typeof (SingleLineTextFieldDefinition), FieldType.Text},
            {typeof (MultiLineTextFieldDefinition), FieldType.MultiLineText},
            {typeof (XhtmlFieldDefinition), FieldType.Xhtml},
            {typeof (NumberFieldDefinition), FieldType.Number},
            {typeof (DateFieldDefinition), FieldType.Date},
            {typeof (MultimediaLinkFieldDefinition), FieldType.MultiMediaLink},
            {typeof (ComponentLinkFieldDefinition), FieldType.ComponentLink},
            {typeof (ExternalLinkFieldDefinition), FieldType.ExternalLink},
            {typeof (EmbeddedSchemaFieldDefinition), FieldType.Embedded},
            {typeof (KeywordFieldDefinition), FieldType.Keyword}
        };

        private bool _retrofitMode;
        private Schema _currentSchema;

        public PublishMappings()
        {
            foreach (KeyValuePair<string, string> ns in _namespaces)
            {
                _namespaceManager.AddNamespace(ns.Key, ns.Value);
            }
        }

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            package.TryGetParameter("retrofitMode", out _retrofitMode, Logger);

            // The input Component is used to relate all the generated Binaries to (so they get unpublished if the Component is unpublished).
            Component inputComponent = GetComponent();
            StructureGroup mappingsStructureGroup = GetSystemStructureGroup("mappings");

            List<Binary> binaries = new List<Binary>
            {
                PublishSemanticVocabularies(mappingsStructureGroup, inputComponent),
                PublishSemanticSchemas(mappingsStructureGroup, inputComponent),
                PublishXpmRegionConfiguration(mappingsStructureGroup, inputComponent),
                PublishPageIncludes(mappingsStructureGroup, inputComponent)
            };

            AddBootstrapJsonBinary(binaries, inputComponent, mappingsStructureGroup, "mapping");

            OutputSummary("Publish Mappings", binaries.Select(b => b?.Url));
        }

        private Binary PublishSemanticVocabularies(StructureGroup structureGroup, Component relatedComponent)
        {
            ApplicationData vocabularyAppData = Session.SystemManager.LoadGlobalApplicationData(VocabulariesAppDataId);
            if (vocabularyAppData == null)
            {
                throw new DxaException("No Vocabularies are defined in Global Application Data: " + VocabulariesAppDataId);
            }

            XElement vocabulariesXml = XElement.Parse(Encoding.Unicode.GetString(vocabularyAppData.Data));

            List<VocabularyData> vocabularies = vocabulariesXml.Elements()
                .Select(vocabElement => new VocabularyData { Prefix = vocabElement.Attribute("prefix").Value, Vocab = vocabElement.Attribute("name").Value })
                .ToList();

            if (_retrofitMode)
            {
                Logger.Info("Retrofit mode is enabled; generating semantic vocabularies for each (non-embedded) Schema.");
                RepositoryItemsFilter schemasFilter = new RepositoryItemsFilter(Session)
                {
                    Recursive = true,
                    ItemTypes = new[] { ItemType.Schema },
                    SchemaPurposes = new[] { SchemaPurpose.Component, SchemaPurpose.Multimedia, SchemaPurpose.Metadata, SchemaPurpose.Region }
                };
                IEnumerable<Schema> schemas = Publication.GetItems(schemasFilter).Cast<Schema>();
                vocabularies.AddRange(schemas.Select(schema => new VocabularyData { Prefix = GetDefaultVocabularyPrefix(schema), Vocab = schema.NamespaceUri }));

            }

            if (!vocabularies.Any(vocab => vocab.Prefix == CoreVocabularyPrefix))
            {
                VocabularyData coreVocabulary = new VocabularyData { Prefix = CoreVocabularyPrefix, Vocab = CoreVocabulary };
                vocabularies.Add(coreVocabulary);
            }

            return AddJsonBinary(vocabularies, relatedComponent, structureGroup, "vocabularies");
        }

        private Binary PublishSemanticSchemas(StructureGroup structureGroup, Component relatedComponent)
        {
            RepositoryItemsFilter schemasFilter = new RepositoryItemsFilter(Session)
            {
                Recursive = true,
                ItemTypes = new[] { ItemType.Schema },
                SchemaPurposes = new[] { SchemaPurpose.Component, SchemaPurpose.Multimedia, SchemaPurpose.Metadata, SchemaPurpose.Region }
            };

            IEnumerable<Schema> schemas = Publication.GetItems(schemasFilter).Cast<Schema>();
            SemanticSchemaData[] semanticSchemas = schemas.Select(GetSemanticSchema).ToArray();

            return AddJsonBinary(semanticSchemas, relatedComponent, structureGroup, "schemas", variantId: "semantic-schemas");
        }

        private Dictionary<string, XpmRegionData> CollectNativeRegions()
        {
            Dictionary<string, XpmRegionData> nativeRegions = new Dictionary<string, XpmRegionData>();

            RepositoryItemsFilter filter = new RepositoryItemsFilter(Session)
            {
                SchemaPurposes = new[] { SchemaPurpose.Region },
                ItemTypes = new[] { ItemType.Schema },
                Recursive = true
            };

            IEnumerable<Schema> regionSchemas = Publication.GetItems(filter).Cast<Schema>();

            foreach (Schema schema in regionSchemas)
            {
                dynamic regionDefinition = schema.RegionDefinition;

                string regionSchemaId = schema.Id.ItemId.ToString();
                if (nativeRegions.All(nr => nr.Key != regionSchemaId))
                {
                    XpmRegionData nativeRegion = new XpmRegionData
                    {
                        Region = regionSchemaId,
                        ComponentTypes = GetComponentTypeConstraints(regionDefinition)
                    };
                    nativeRegions.Add(regionSchemaId, nativeRegion);
                }
                else
                {
                    Logger.Debug($"Region {regionSchemaId} has already been added. Skipping.");
                }
            }
            return nativeRegions;
        }

        private List<XpmComponentTypeData> GetComponentTypeConstraints(dynamic regionDefinition)
        {
            List<XpmComponentTypeData> result = new List<XpmComponentTypeData>();
            dynamic constraints = regionDefinition.ComponentPresentationConstraints;
            foreach (var constraint in constraints)
            {
                if (constraint.GetType().ToString() == "Tridion.ContentManager.CommunicationManagement.Regions.TypeConstraint")
                {
                    string schemaId = constraint.BasedOnSchema != null ? constraint.BasedOnSchema.Id.ToString() : "*";
                    string templateId = constraint.BasedOnComponentTemplate != null ? constraint.BasedOnComponentTemplate.Id.ToString() : "*";
                    result.Add(new XpmComponentTypeData()
                    {
                        Schema = schemaId,
                        Template = templateId
                    });
                }
            }
            if (result.Count == 0)
            {
                result.Add(new XpmComponentTypeData()
                {
                    Schema = "*",
                    Template = "*"
                });
            }
            return result;
        }

        private Binary PublishXpmRegionConfiguration(StructureGroup structureGroup, Component relatedComponent)
        {
            IDictionary<string, XpmRegionData> xpmRegions = new Dictionary<string, XpmRegionData>();

            foreach (ComponentTemplate ct in Publication.GetComponentTemplates())
            {
                string regionName = GetRegionName(ct);

                XpmRegionData xpmRegion;
                if (!xpmRegions.TryGetValue(regionName, out xpmRegion))
                {
                    xpmRegion = new XpmRegionData { Region = regionName, ComponentTypes = new List<XpmComponentTypeData>() };
                    xpmRegions.Add(regionName, xpmRegion);
                }

                string templateId = ct.Id.GetVersionlessUri().ToString();

                IEnumerable<XpmComponentTypeData> allowedComponentTypes = ct.RelatedSchemas.Select(
                    schema => new XpmComponentTypeData
                    {
                        Schema = schema.Id.GetVersionlessUri().ToString(),
                        Template = templateId
                    }
                );

                    xpmRegion.ComponentTypes.AddRange(allowedComponentTypes);
                }

            Dictionary<string, XpmRegionData> nativeRegions = CollectNativeRegions();
            foreach (KeyValuePair<string, XpmRegionData> nativeRegion in nativeRegions)
            {
                if (!xpmRegions.ContainsKey(nativeRegion.Key))
                {
                    xpmRegions.Add(nativeRegion);
                }
            }

            return AddJsonBinary(xpmRegions.Values, relatedComponent, structureGroup, "regions");
        }

        private Binary PublishPageIncludes(StructureGroup structureGroup, Component relatedComponent)
        {
            IDictionary<string, string[]> pageIncludes = new Dictionary<string, string[]>();

            RepositoryItemsFilter pageTemplatesFilter = new RepositoryItemsFilter(Session)
            {
                ItemTypes = new[] { ItemType.PageTemplate },
                Recursive = true
            };

            IEnumerable<PageTemplate> pageTemplates = Publication.GetItems(pageTemplatesFilter).Cast<PageTemplate>();
            foreach (PageTemplate pt in pageTemplates.Where(pt => pt.MetadataSchema != null && pt.Metadata != null))
            {
                ItemFields ptMetadataFields = new ItemFields(pt.Metadata, pt.MetadataSchema);
                string[] includes = ptMetadataFields.GetTextValues("includes").ToArray();
                pageIncludes.Add(pt.Id.ItemId.ToString(), includes);
            }

            return AddJsonBinary(pageIncludes, relatedComponent, structureGroup, "includes");
        }

        private SemanticSchemaData GetSemanticSchema(Schema schema)
        {
            try
            {
                SemanticTypeData[] semanticTypes = GetSemanticTypes(schema).ToArray();

                SemanticSchemaData semanticSchema = new SemanticSchemaData
                {
                    Id = schema.Id.ItemId,
                    RootElement = GetSemanticRootElementName(schema),
                    Semantics = semanticTypes
                };

                _currentSchema = schema;

                SchemaFields schemaFields = new SchemaFields(schema, expandEmbeddedFields: true);
                List<SemanticSchemaFieldData> semanticSchemaFields = new List<SemanticSchemaFieldData>();
                semanticSchemaFields.AddRange(GetSemanticSchemaFields(schemaFields.Fields, semanticTypes, schema, "/" + schema.RootElementName));
                semanticSchemaFields.AddRange(GetSemanticSchemaFields(schemaFields.MetadataFields, semanticTypes, schema, "/Metadata"));
                semanticSchema.Fields = semanticSchemaFields.ToArray();

                return semanticSchema;
            }
            catch (Exception ex)
            {
                throw new DxaException(
                    string.Format("An error occurred while generating the semantic schema for Schema '{0}' ({1}).", schema.Title, schema.Id),
                    ex
                );
            }
        }

        private static string GetSemanticRootElementName(Schema schema)
        {
            string result = schema.RootElementName;
            if (string.IsNullOrEmpty(result))
            {
                // Multimedia/metadata schemas don't have a root element name, so we use its title without any invalid characters
                result = Regex.Replace(schema.Title.Trim(), InvalidCharactersPattern, string.Empty);
            }
            return result;
        }

        private string GetDefaultVocabularyPrefix(Schema schema)
        {
            return _retrofitMode && !string.IsNullOrEmpty(schema.NamespaceUri) ? string.Format("s{0}", schema.Id.ItemId) : CoreVocabularyPrefix;
        }

        private IEnumerable<SemanticTypeData> GetSemanticTypes(Schema schema)
        {
            List<SemanticTypeData> semanticTypes = new List<SemanticTypeData>
            {
                new SemanticTypeData { Prefix = GetDefaultVocabularyPrefix(schema), Entity = GetSemanticRootElementName(schema) }
            };

            ApplicationData typeOfAppData = schema.LoadApplicationData(TypeOfAppDataId);
            if (typeOfAppData == null)
            {
                return semanticTypes;
            }

            string typeOfString = XElement.Parse(Encoding.Unicode.GetString(typeOfAppData.Data)).Value;

            foreach (string typeOf in typeOfString.Split(','))
            {
                string[] typeOfParts = typeOf.Split(':');
                if (typeOfParts.Length == 2)
                {
                    semanticTypes.Add(new SemanticTypeData { Prefix = typeOfParts[0], Entity = typeOfParts[1] });
                }
                else
                {
                    Logger.Warning(
                        string.Format("Invalid format for semantic typeOf application data for Schema '{0}' ({1}): '{2}'.  Format must be <prefix>:<entity>.",
                            schema.Title, schema.Id, typeOf)
                    );
                }
            }

            return semanticTypes;
        }
        private IEnumerable<SemanticSchemaFieldData> GetSemanticSchemaFields(
            IEnumerable<ItemFieldDefinition> schemaFields,
            SemanticTypeData[] semanticTypes,
            Schema schema,
            string contextPath
        )
        {
            List<SemanticSchemaFieldData> semanticSchemaFields = new List<SemanticSchemaFieldData>();
            if (schemaFields == null) return semanticSchemaFields;
            foreach (ItemFieldDefinition schemaField in schemaFields)
            {
                EmbeddedSchemaFieldDefinition embeddedSchemaField = schemaField as EmbeddedSchemaFieldDefinition;
                SemanticSchemaFieldData semanticSchemaField = (embeddedSchemaField == null) ? new SemanticSchemaFieldData() : new EmbeddedSemanticSchemaFieldData();
                semanticSchemaField.Name = schemaField.Name;
                semanticSchemaField.Path = $"{contextPath}/{schemaField.Name}";
                semanticSchemaField.IsMultiValue = schemaField.MaxOccurs != 1;
                semanticSchemaField.Semantics = GetSemanticProperties(schemaField, semanticTypes, schema).ToArray();
                semanticSchemaField.FieldType = GetFieldType(schemaField);
                if (embeddedSchemaField == null)
                {
                    semanticSchemaField.Fields = new SemanticSchemaFieldData[0];
                }
                else
                {
                    Schema embeddedSchema = embeddedSchemaField.EmbeddedSchema;
                    EmbeddedSemanticSchemaFieldData fieldData = (EmbeddedSemanticSchemaFieldData)semanticSchemaField;
                    fieldData.Id = embeddedSchema.Id.ItemId;
                    fieldData.Title = embeddedSchema.Title;
                    fieldData.RootElementName = embeddedSchema.RootElementName;
                    semanticSchemaField.Fields = GetSemanticSchemaFields(
                        embeddedSchemaField.EmbeddedFields,
                        semanticTypes.Concat(GetSemanticTypes(embeddedSchema)).ToArray(),
                        embeddedSchema,
                        semanticSchemaField.Path
                    ).ToArray();
                }

                semanticSchemaFields.Add(semanticSchemaField);
            }

            return semanticSchemaFields;
        }

        private static FieldType GetFieldType(ItemFieldDefinition field) => _fieldTypes[field.GetType()];

        private IEnumerable<SemanticPropertyData> GetSemanticProperties(ItemFieldDefinition schemaField, IEnumerable<SemanticTypeData> semanticTypes, Schema schema)
        {
            XmlElement typeOfExtensionElement = null;
            XmlElement propertyExtensionElement = null;
            XmlElement extensionElement = schemaField.ExtensionXml;
            if (extensionElement != null)
            {
                typeOfExtensionElement = (XmlElement)extensionElement.SelectSingleNode("mapping:typeof", _namespaceManager);
                propertyExtensionElement = (XmlElement)extensionElement.SelectSingleNode("mapping:property", _namespaceManager);
            }

            // Create a set of distinct semantic types for this field.
            List<SemanticTypeData> fieldSemanticTypes = new List<SemanticTypeData>(semanticTypes);
            if (typeOfExtensionElement != null)
            {
                foreach (string typeOf in typeOfExtensionElement.InnerText.Split(','))
                {
                    string[] typeOfParts = typeOf.Split(':');
                    if (typeOfParts.Length != 2)
                    {
                        throw new DxaException(string.Format("Invalid format for semantic typeOf extension data of field '{0}' in Schema '{1}' ({2}): '{3}'.  Format must be <prefix>:<entity>.",
                            schemaField.Name, schema.Title, schema.Id, typeOf));
                    }
                    fieldSemanticTypes.Add(new SemanticTypeData { Prefix = typeOfParts[0], Entity = typeOfParts[1] });
                }
            }
            fieldSemanticTypes = fieldSemanticTypes.Distinct().ToList();

            // Create a list of semantic property names for this field
            string semanticProperties = string.Format("{0}:{1}", GetDefaultVocabularyPrefix(schema), schemaField.Name);
            if (_retrofitMode && string.IsNullOrEmpty(schema.NamespaceUri))
            {
                semanticProperties += string.Format(",{0}:{1}", GetDefaultVocabularyPrefix(_currentSchema), schemaField.Name);
            }
            if (propertyExtensionElement != null)
            {
                semanticProperties += "," + propertyExtensionElement.InnerText;
            }

            // Resolve the property name prefixes to semantic types
            List<SemanticPropertyData> result = new List<SemanticPropertyData>();
            foreach (string property in semanticProperties.Split(','))
            {
                string[] propertyParts = property.Split(':');
                if (propertyParts.Length != 2)
                {
                    throw new DxaException(string.Format("Invalid format for semantic property of field '{0}' in Schema '{1}' ({2}): '{3}'.  Format must be <prefix>:<property>.",
                        schemaField.Name, schema.Title, schema.Id, property));
                }

                string prefix = propertyParts[0];
                string propertyName = propertyParts[1];

                SemanticTypeData[] propertySemanticTypes = fieldSemanticTypes.Where(s => s.Prefix == prefix).ToArray();
                if (propertySemanticTypes.Length == 0)
                {
                    Logger.Warning(string.Format("Semantic property of field '{0}' in Schema '{1}' ({2}) references an undeclared prefix '{3}'. Semantic types: {4}",
                        schemaField.Name, schema.Title, schema.Id, property, string.Join(", ", fieldSemanticTypes.Select(s => s.ToString()))));
                    continue;
                }

                result.AddRange(propertySemanticTypes.Select(s => new SemanticPropertyData { Prefix = s.Prefix, Entity = s.Entity, Property = propertyName }));
            }

            return result;
        }
    }
}
