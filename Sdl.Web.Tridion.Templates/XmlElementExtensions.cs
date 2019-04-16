using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Tridion;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Extension methods for class <see cref="XmlElement"/>.
    /// </summary>
    public static class XmlElementExtensions
    {
        private static readonly XmlNamespaceManager _defaultNamespaceManager = new XmlNamespaceManager(new NameTable());

        /// <summary>
        /// Class constructor
        /// </summary>
        static XmlElementExtensions()
        {
            _defaultNamespaceManager.AddNamespace("xlink", Constants.XlinkNamespace);
            _defaultNamespaceManager.AddNamespace("xhtml", Constants.XhtmlNamespace);
        }

        /// <summary>
        /// Gets string values from a CM text field with a given name under the current XML element.
        /// </summary>
        /// <param name="rootElement">The current XML element.</param>
        /// <param name="fieldName">The CM field (XML) name.</param>
        /// <returns>The string values or <c>null</c> if the field does not exist.</returns>
        public static IEnumerable<string> GetTextFieldValues(this XmlElement rootElement, string fieldName)
        {
            XmlNodeList fieldElements = rootElement?.SelectNodes(GetXPathFromFieldName(fieldName));
            if ((fieldElements == null) || (fieldElements.Count == 0))
            {
                return null;
            }
            return fieldElements.Cast<XmlElement>().Select(xmlElement => xmlElement.InnerText);
        }

        /// <summary>
        /// Gets the string value from a CM text field with a given name under the current XML element.
        /// </summary>
        /// <param name="rootElement">The current XML element.</param>
        /// <param name="fieldName">The CM field (XML) name.</param>
        /// <returns>The string value or <c>null</c> if the field does not exist or has no value.</returns>
        public static string GetTextFieldValue(this XmlElement rootElement, string fieldName)
        {
            IEnumerable<string> values = rootElement.GetTextFieldValues(fieldName);
            return values?.FirstOrDefault();
        }

        /// <summary>
        /// Gets values from a CM embedded schema field with a given name under the current XML element.
        /// </summary>
        /// <param name="rootElement">The current XML element.</param>
        /// <param name="fieldName">The CM field (XML) name.</param>
        /// <returns>The XML element values or <c>null</c> if the field does not exist.</returns>
        public static IEnumerable<XmlElement> GetEmbeddedFieldValues(this XmlElement rootElement, string fieldName)
        {
            XmlNodeList fieldElements = rootElement?.SelectNodes(GetXPathFromFieldName(fieldName));
            if ((fieldElements == null) || (fieldElements.Count == 0))
            {
                return null;
            }
            return fieldElements.Cast<XmlElement>();
        }

        /// <summary>
        /// Removes all XLink-related attribute from the current XML element.
        /// </summary>
        /// <param name="xmlElement">The current XML element</param>
        public static void RemoveXlinkAttributes(this XmlElement xmlElement)
        {
            IEnumerable<XmlAttribute> attributesToRemove = xmlElement.Attributes.Cast<XmlAttribute>()
                .Where(a => (a.NamespaceURI == Constants.XlinkNamespace) || (a.LocalName == "xlink")).ToArray();
            foreach (XmlAttribute attr in attributesToRemove)
            {
                xmlElement.Attributes.Remove(attr);
            }
        }

        /// <summary>
        /// Select a single XML element under the current XML element based on a given XPath.
        /// </summary>
        /// <param name="xmlElement">The current XML element.</param>
        /// <param name="xpath">The XPath which must resolve to an XML element.</param>
        /// <param name="namespaceManager">Optional XML namespace manager. If not specified or <c>null</c>, a default namespace manager will be used.</param>
        /// <returns>The XML element or <c>null</c>.</returns>
        public static XmlElement SelectSingleElement(this XmlElement xmlElement, string xpath, XmlNamespaceManager namespaceManager = null)
        {
            if (namespaceManager == null)
            {
                namespaceManager = _defaultNamespaceManager;
            }
            return (XmlElement)xmlElement.SelectSingleNode(xpath, namespaceManager);
        }

        /// <summary>
        /// Select XML elements under the current XML element based on a given XPath.
        /// </summary>
        /// <param name="xmlElement">The current XML element.</param>
        /// <param name="xpath">The XPath which must resolve to XML elements.</param>
        /// <param name="namespaceManager">Optional XML namespace manager. If not specified or <c>null</c>, a default namespace manager will be used.</param>
        /// <returns>The XML elements.</returns>
        public static IEnumerable<XmlElement> SelectElements(this XmlElement xmlElement, string xpath, XmlNamespaceManager namespaceManager = null)
        {
            if (namespaceManager == null)
            {
                namespaceManager = _defaultNamespaceManager;
            }
            return xmlElement.SelectNodes(xpath, namespaceManager).OfType<XmlElement>();
        }

        /// <summary>
        /// Gets the path to the current XML element.
        /// </summary>
        /// <param name="xmlElement">The current XML element.</param>
        /// <returns>The path to the current XML element formed by the local names of ancestor XML elements starting at the document element.</returns>
        public static string GetPath(this XmlElement xmlElement)
        {
            List<string> pathSegments = new List<string>();
            XmlElement currentElement = xmlElement;
            while (currentElement != null)
            {
                pathSegments.Insert(0, currentElement.LocalName);
                currentElement = currentElement.ParentNode as XmlElement;
            }
            return "/" + string.Join("/", pathSegments);
        }

        private static string GetXPathFromFieldName(string fieldname)
        {
            string[] bits = fieldname.Split('/');
            return "//" + string.Join("/", bits.Select(f => $"*[local-name()='{f}']"));
        }
    }
}
