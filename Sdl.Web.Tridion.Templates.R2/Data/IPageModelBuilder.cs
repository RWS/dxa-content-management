using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Interface for DXA R2 Page Data Model Builders
    /// </summary>
    /// <seealso cref="DataModelBuilderPipeline"/>
    public interface IPageModelDataBuilder
    {
        /// <summary>
        /// Builds a Page Data Model from a given CM Page object.
        /// </summary>
        /// <param name="pageModelData">The Page Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="page">The CM Page.</param>
        void BuildPageModel(ref PageModelData pageModelData, Page page);
    }
}
