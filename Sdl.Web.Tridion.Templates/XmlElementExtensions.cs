using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Sdl.Web.Tridion
{
    public static class XmlElementExtensions
    {

        public static IEnumerable<string> GetTextFieldValues(this XmlElement rootElement, string fieldName)
        {
            XmlNodeList fieldElements = rootElement?.SelectNodes(string.Format("*[local-name()='{0}']", fieldName));
            if (fieldElements == null || fieldElements.Count == 0)
            {
                return null;
            }
            return fieldElements.Cast<XmlElement>().Select(xmlElement => xmlElement.InnerText);
        }

        public static string GetTextFieldValue(this XmlElement rootElement, string fieldName)
        {
            IEnumerable<string> values = rootElement.GetTextFieldValues(fieldName);
            return values?.FirstOrDefault();
        }

        public static IEnumerable<XmlElement> GetEmbeddedFieldValues(this XmlElement rootElement, string fieldName)
        {
            XmlNodeList fieldElements = rootElement?.SelectNodes(string.Format("*[local-name()='{0}']", fieldName));
            if (fieldElements == null || fieldElements.Count == 0)
            {
                return null;
            }
            return fieldElements.Cast<XmlElement>();
        }

        public static void RemoveXlinkAttributes(this XmlElement xmlElement)
        {
            IEnumerable<XmlAttribute> attributesToRemove = xmlElement.Attributes.Cast<XmlAttribute>()
                .Where(a => a.NamespaceURI == "http://www.w3.org/1999/xlink" || a.LocalName == "xlink").ToArray();
            foreach (XmlAttribute attr in attributesToRemove)
            {
                xmlElement.Attributes.Remove(attr);
            }
        }

        public static XmlElement SelectSingleElement(this XmlElement xmlElement, string xpath, XmlNamespaceManager namespaceManager = null)
        {
            return (XmlElement) xmlElement.SelectSingleNode(xpath, namespaceManager);
        }

        public static IEnumerable<XmlElement> SelectElements(this XmlElement xmlElement, string xpath, XmlNamespaceManager namespaceManager = null)
        {
            return xmlElement.SelectNodes(xpath, namespaceManager).OfType<XmlElement>();
        }
    }
}
