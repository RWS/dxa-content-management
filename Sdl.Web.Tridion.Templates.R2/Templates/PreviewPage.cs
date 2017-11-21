using System;
using System.IO;
using System.Net;
using Sdl.Web.Tridion.Templates.Common;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.R2.Templates
{   
    // Not currently implemented -- requires model-service + webapp changes

    /// <summary>
    /// Generates a DXA R2 Data Model based on the current Page
    /// </summary>
    /*[TcmTemplateTitle("Preview Page R2")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Templates.R2.Resources.PreviewPageParameters.xsd")]
    public class PreviewPage : TemplateR2Base
    {
        /// <summary>
        /// Performs the Transform.
        /// </summary>
        public override void Transform(Engine engine, Package package)
        {
            // do NOT execute this logic when we are actually publishing! (similair for fast track publishing)
            if (engine.RenderMode == RenderMode.Publish || (engine.PublishingContext.PublicationTarget != null && !global::Tridion.ContentManager.TcmUri.IsNullOrUriNull(engine.PublishingContext.PublicationTarget.Id)))
            {
                return;
            }

            Item outputItem = package.GetByName(Package.OutputName);
            string inputValue = package.GetValue(Package.OutputName);

            if (string.IsNullOrEmpty(inputValue))
            {
                Logger.Warning("Could not find 'Output' in the package, nothing to preview");
                return;
            }

            string stagingUrl;
            package.TryGetParameter("stagingUrl", out stagingUrl, Logger);
            string outputValue = HttpPost(stagingUrl, inputValue);

            // replace the Output item in the package
            package.Remove(outputItem);
            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Html, outputValue));
        }

        private string HttpPost(string url, string postData)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postData.Length;

                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(postData);
                }

                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd().Trim();
                }

            }
            catch (Exception e)
            {
                Logger.Error("Problem performing http post", e);
                return $"<h2>There was an error while generating the preview.</h2><h3>{e}</h3>";
            }
        }
    }*/
}
