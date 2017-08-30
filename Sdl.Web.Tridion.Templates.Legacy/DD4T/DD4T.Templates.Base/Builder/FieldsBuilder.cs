using System;
using DD4T.ContentModel;
using DD4T.ContentModel.Exceptions;
using Dynamic = DD4T.ContentModel;
using TCM = Tridion.ContentManager.ContentManagement;
using DD4T.Templates.Base.Utils;
using Tridion.ContentManager.Templating;

namespace DD4T.Templates.Base.Builder
{
    public class FieldsBuilder
    {
        private static TemplatingLogger log = TemplatingLogger.GetLogger(typeof(FieldsBuilder));
        public static Dynamic.FieldSet BuildFields(TCM.Fields.ItemFields tcmItemFields, int currentLinkLevel, BuildManager manager)
        {
            Dynamic.FieldSet fields = new FieldSet();
            AddFields(fields, tcmItemFields, currentLinkLevel, manager);
            return fields;
        }

        public static void AddFields(Dynamic.FieldSet fields, TCM.Fields.ItemFields tcmItemFields, BuildManager manager)
        {
            AddFields(fields, tcmItemFields, manager.BuildProperties.LinkLevels, manager);
        }

        public static void AddFields(Dynamic.FieldSet fields, TCM.Fields.ItemFields tcmItemFields, int currentLinkLevel, BuildManager manager)
        {
            foreach (TCM.Fields.ItemField tcmItemField in tcmItemFields)
            {
                try
                {
                    if (fields.ContainsKey(tcmItemField.Name))
                    {
                        log.Debug("field exists already, with " + fields[tcmItemField.Name].Values.Count + " values");
                        if (manager.BuildProperties.MergeAction.Equals(Dynamic.MergeAction.Skip) || (manager.BuildProperties.MergeAction.Equals(Dynamic.MergeAction.MergeMultiValueSkipSingleValue) && tcmItemField.Definition.MaxOccurs == 1))
                        {
                            log.Debug(string.Format("skipping field (merge action {0}, maxoccurs {1}", manager.BuildProperties.MergeAction.ToString(), tcmItemField.Definition.MaxOccurs));
                            continue;
                        }
                        Dynamic.Field f = manager.BuildField(tcmItemField, currentLinkLevel);
                        if (manager.BuildProperties.MergeAction.Equals(Dynamic.MergeAction.Replace) || (manager.BuildProperties.MergeAction.Equals(Dynamic.MergeAction.MergeMultiValueReplaceSingleValue) && tcmItemField.Definition.MaxOccurs == 1))
                        {
                            log.Debug(string.Format("replacing field (merge action {0}, maxoccurs {1}", manager.BuildProperties.MergeAction.ToString(), tcmItemField.Definition.MaxOccurs));
                            fields.Remove(f.Name);
                            fields.Add(f.Name, f);
                        }
                        else
                        {
                            IField existingField = fields[f.Name];
                            switch (existingField.FieldType)
                            {

                                case FieldType.ComponentLink:
                                case FieldType.MultiMediaLink:
                                    foreach (Component linkedComponent in f.LinkedComponentValues)
                                    {
                                        bool valueExists = false;
                                        foreach (Component existingLinkedComponent in existingField.LinkedComponentValues)
                                        {
                                            if (linkedComponent.Id.Equals(existingLinkedComponent.Id))
                                            {
                                                // this value already exists
                                                valueExists = true;
                                                break;
                                            }
                                        }
                                        if (!valueExists)
                                        {
                                            existingField.LinkedComponentValues.Add(linkedComponent);
                                        }
                                    }
                                    break;
                                case FieldType.Date:
                                    foreach (DateTime dateTime in f.DateTimeValues)
                                    {
                                        bool valueExists = false;
                                        foreach (DateTime existingDateTime in existingField.DateTimeValues)
                                        {
                                            if (dateTime.Equals(existingDateTime))
                                            {
                                                // this value already exists
                                                valueExists = true;
                                                break;
                                            }
                                        }
                                        if (!valueExists)
                                        {
                                            existingField.DateTimeValues.Add(dateTime);
                                        }
                                    }
                                    break;
                                case FieldType.Number:
                                    foreach (int nr in f.NumericValues)
                                    {
                                        bool valueExists = false;
                                        foreach (int existingNr in existingField.NumericValues)
                                        {
                                            if (nr == existingNr)
                                            {
                                                // this value already exists
                                                valueExists = true;
                                                break;
                                            }
                                        }
                                        if (!valueExists)
                                        {
                                            existingField.NumericValues.Add(nr);
                                        }
                                    }
                                    break;
                                default:
                                    foreach (string val in f.Values)
                                    {
                                        bool valueExists = false;
                                        foreach (string existingVal in existingField.Values)
                                        {
                                            if (val.Equals(existingVal))
                                            {
                                                // this value already exists
                                                valueExists = true;
                                                break;
                                            }
                                        }
                                        if (!valueExists)
                                        {
                                            existingField.Values.Add(val);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Dynamic.Field f = manager.BuildField(tcmItemField, currentLinkLevel);
                        fields.Add(f.Name, f);
                    }
                }
                catch (FieldHasNoValueException)
                {
                    // fail silently, field is not added to the list
                }
                catch (FieldTypeNotDefinedException)
                {
                    // fail silently, field is not added to the list
                }
            }
        }

        public static void AddXpathToFields(Dynamic.FieldSet fieldSet, string baseXpath)
        {
            // add XPath properties to all fields

            if (fieldSet == null)
            {
                log.Error("fieldSet == null");
                return;
            }
            if (fieldSet.Values == null)
            {
                log.Error("fieldSet.Values == null");
                return;
            }
            try
            {
                foreach (Field f in fieldSet.Values)
                {
                    f.XPath = string.Format("{0}/custom:{1}", baseXpath, f.Name);
                    int i = 1;
                    if (f.EmbeddedValues != null)
                    {
                        foreach (FieldSet subFields in f.EmbeddedValues)
                        {
                            AddXpathToFields(subFields, string.Format("{0}/custom:{1}[{2}]", baseXpath, f.Name, i++));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Exception adding xpath to fields", e);
            }
        }
    }
}