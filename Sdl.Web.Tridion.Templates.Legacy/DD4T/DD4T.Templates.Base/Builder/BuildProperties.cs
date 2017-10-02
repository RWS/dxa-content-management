using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tridion.ContentManager.Templating;
using DD4T.Serialization;

namespace DD4T.Templates.Base.Builder
{
    public enum MergeAction { Replace, Skip, Merge, MergeMultiValueReplaceSingleValue, MergeMultiValueSkipSingleValue }
    public class BuildProperties
    {

        public static SerializationFormat DefaultSerializationFormat = SerializationFormat.JSON;
        public static bool DefaultCompressionEnabled = false;
        public static MergeAction DefaultMergeAction = MergeAction.Skip;
        public static int DefaultLinkLevels = 1;
        public static bool DefaultResolveWidthAndHeight = false;
        public static bool DefaultPublishEmptyFields = false;
        public static bool DefaultFollowLinksPerField = false;
        public static bool DefaultOmitContextPublications = false;
        public static bool DefaultOmitOwningPublications = false;
        public static bool DefaultOmitFolders = false;
        public static bool DefaultOmitCategories = false;
        public static bool DefaultOmitValueLists = false;        
        public static bool DefaultECLEnabled = false;

        public SerializationFormat SerializationFormat { get; set; }
        public bool CompressionEnabled { get; set; }
        public MergeAction MergeAction { get; set; }
        public int LinkLevels { get; set; }
        public bool ResolveWidthAndHeight { get; set; }
        public string PublishBinariesTargetStructureGroup { get; set; }
        public bool PublishEmptyFields { get; set; }
        public bool FollowLinksPerField { get; set; }
        public bool OmitContextPublications { get; set; }
        public bool OmitOwningPublications { get; set; }
        public bool OmitFolders { get; set; }
        public bool OmitCategories { get; set; }
        public bool OmitValueLists { get; set; }
        public bool ECLEnabled { get; set; }


        public BuildProperties(Package package)
        {
            if (package == null)
                return;
            if (HasPackageValue(package, "MergeAction"))
            {
                MergeAction = (MergeAction) Enum.Parse(typeof(MergeAction), package.GetValue("MergeAction"));
            }
            else
            {
                MergeAction = DefaultMergeAction;
            }
            if (HasPackageValue(package, "LinkLevels"))
            {
                LinkLevels = Convert.ToInt32(package.GetValue("LinkLevels"));
            }
            else
            {
                LinkLevels = DefaultLinkLevels;
            }
            if (HasPackageValue(package, "ResolveWidthAndHeight"))
            {
                ResolveWidthAndHeight = package.GetValue("ResolveWidthAndHeight").ToLower().Equals("yes");
            }
            else
            {
                ResolveWidthAndHeight = DefaultResolveWidthAndHeight;
            }
            if (HasPackageValue(package, "PublishEmptyFields"))
            {
                PublishEmptyFields = package.GetValue("PublishEmptyFields").ToLower().Equals("yes");
            }
            else
            {
                PublishEmptyFields = DefaultPublishEmptyFields;
            }
            if (HasPackageValue(package, "sg_PublishBinariesTargetStructureGroup"))
            {
                PublishBinariesTargetStructureGroup = package.GetValue("sg_PublishBinariesTargetStructureGroup");
            }
            if (HasPackageValue(package, "FollowLinksPerField"))
            {
                FollowLinksPerField = package.GetValue("FollowLinksPerField").ToLower().Equals("yes");
            }
            else
            {
                FollowLinksPerField = DefaultFollowLinksPerField;
            }
            if (HasPackageValue(package, "SerializationFormat"))
            {
                SerializationFormat = (SerializationFormat)Enum.Parse(typeof(SerializationFormat), package.GetValue("SerializationFormat").ToUpper());
            }
            else
            {
                SerializationFormat = DefaultSerializationFormat;
            }
            if (HasPackageValue(package, "OmitCategories"))
            {
                OmitCategories = package.GetValue("OmitCategories").ToLower().Equals("yes");
            }
            else
            {
                OmitCategories = DefaultOmitCategories;
            }
            if (HasPackageValue(package, "OmitValueLists"))
            {
                OmitValueLists = package.GetValue("OmitValueLists").ToLower().Equals("yes");
            }
            else
            {
                OmitValueLists = DefaultOmitValueLists;
            } 
            if (HasPackageValue(package, "OmitContextPublications"))
            {
                OmitContextPublications = package.GetValue("OmitContextPublications").ToLower().Equals("yes");
            }
            else
            {
                OmitContextPublications = DefaultOmitContextPublications;
            }
            if (HasPackageValue(package, "OmitOwningPublications"))
            {
                OmitOwningPublications = package.GetValue("OmitOwningPublications").ToLower().Equals("yes");
            }
            else
            {
                OmitOwningPublications = DefaultOmitOwningPublications;
            }
            if (HasPackageValue(package, "OmitFolders"))
            {
                OmitFolders = package.GetValue("OmitFolders").ToLower().Equals("yes");
            }
            else
            {
                OmitFolders = DefaultOmitFolders;
            }
            if (HasPackageValue(package, "CompressionEnabled"))
            {
                CompressionEnabled = package.GetValue("CompressionEnabled").ToLower().Equals("yes");
            }
            else
            {
                CompressionEnabled = DefaultCompressionEnabled;
            }
            if (HasPackageValue(package, "ECLEnabled"))
            {
                ECLEnabled = package.GetValue("ECLEnabled").ToLower().Equals("yes");
            }
            else
            {
                ECLEnabled = DefaultECLEnabled;
            }
        }

        private bool HasPackageValue(Package package, string key)
        {
            foreach (KeyValuePair<string, Item> kvp in package.GetEntries())
            {
                if (kvp.Key.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }
    }
}