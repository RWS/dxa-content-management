using Sdl.Web.DataModel;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Interface for DXA R2 Keyword Data Model Builders
    /// </summary>
    /// <seealso cref="DataModelBuilderPipeline"/>
    public interface IKeywordModelDataBuilder
    {
        /// <summary>
        /// Builds a Keyword Data Model from a given CM Keyword object.
        /// </summary>
        /// <param name="keywordModelData">The Keyword Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="keyword">The CM Page.</param>
        /// <param name="expandLinkDepth">The level of Component/Keyword links to expand.</param>
        /// <remarks>
        /// The <paramref name="expandLinkDepth"/> parameter starts at <see cref="DataModelBuilderSettings.ExpandLinkDepth"/>, 
        /// but is decremented for expanded Keyword/Component links (recursively).
        /// </remarks>
        void BuildKeywordModel(ref KeywordModelData keywordModelData, Keyword keyword, int expandLinkDepth);
    }
}
