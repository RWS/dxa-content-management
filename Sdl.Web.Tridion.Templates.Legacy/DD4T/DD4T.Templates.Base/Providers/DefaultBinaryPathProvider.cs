using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tridion.ContentManager.Templating;

namespace DD4T.Templates.Base.Providers
{
    /// <summary>
    /// Default implementation of the IBinaryPathProvider for the DD4T framework.
    /// This class does not contain any logic of its own, instead it extends BaseBinaryPathProvider, to allow easy customization.
    /// </summary>
    public class DefaultBinaryPathProvider : BaseBinaryPathProvider
    {
        public DefaultBinaryPathProvider(Engine engine, Package package) : base(engine, package)
        {
        }
        /// <summary>
        /// Magic value which represents the situation where the binary must be published using the default SDL Web logic
        /// </summary>
        public static readonly string USE_DEFAULT_BINARY_PATH = "9hh1jj2uuKKKsjasdddd";
    }
}
