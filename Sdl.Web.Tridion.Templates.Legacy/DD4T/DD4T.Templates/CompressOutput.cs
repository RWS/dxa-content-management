using DD4T.Serialization;
using System;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace DD4T.Templates.XML
{
    [Obsolete("Enable compression by setting the template parameter CompressionEnabled to 'yes'")]
    [TcmTemplateTitle("Compress Output")]
    public class CompressOutput : ITemplate
    {
        protected TemplatingLogger log = TemplatingLogger.GetLogger(typeof(CompressOutput));
        protected Package package;
        protected Engine engine;

        public void Transform(Engine engine, Package package)
        {
            this.package = package;
            this.engine = engine;

            Item outputItem = package.GetByName(Package.OutputName);
            string inputValue = package.GetValue(Package.OutputName);

            if (string.IsNullOrEmpty(inputValue))
            {
                log.Warning("Could not find 'Output' in the package, nothing to transform");
                return;
            }

            string outputValue = Compressor.Compress(inputValue);
            // replace the Output item in the package
            outputItem.SetAsString(outputValue);
        }
    }
}
