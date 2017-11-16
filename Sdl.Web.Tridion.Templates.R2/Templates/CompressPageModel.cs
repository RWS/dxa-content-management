using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Templates.R2.Data;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.R2.Templates
{
    /*
    [TcmTemplateTitle("Generate Compressed DXA R2 Page Model")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Templates.R2.Resources.GenerateCompressedPageModelParameters.xsd")]
    public class CompressPageModel : TemplateR2Base
    {
        public static string Compress(string data)
        {
            using (MemoryStream i = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                using (MemoryStream o = new MemoryStream())
                {
                    using (Stream s = new GZipStream(o, CompressionMode.Compress))
                    {
                        i.CopyTo(s);
                    }
                    return Convert.ToBase64String(o.ToArray());
                }
            }
        }

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);
            Page page = GetPage();
            try
            {
                // Create our data model pipeline.
                DataModelBuilderPipeline modelBuilderPipeline = CreatePipeline();
                // Create page model
                PageModelData pageModel = modelBuilderPipeline.CreatePageModel(page);
                // Serialize our page model
                string pageModelJson = JsonSerialize(pageModel, IsPreview, DataModelBinder.SerializerSettings);

                OutputText(Compress(pageModelJson));
            }
            catch (Exception ex)
            {
                throw new DxaException($"An error occurred while rendering {page.FormatIdentifier()}", ex);
            }
        }
    }*/
}
