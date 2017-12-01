using System.ServiceModel.Activation;
using Tridion.Logging;
using Tridion.Web.UI.Core.Services;
using DXA.CM.Extensions.CustomResolver.Models.Interfaces;

namespace DXA.CM.Extensions.CustomResolver.Models
{
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public partial class Services : BaseService, IServices
	{
		public string LoadConfiguration()
		{
			using (Tracer.GetTracer().StartTrace())
			{
				return LoadConfigurationImpl();
			}
		}

		public string SaveConfiguration(string configurationXml)
		{
			using (Tracer.GetTracer().StartTrace(configurationXml))
			{
				return SaveConfigurationImpl(configurationXml);
			}
		}
	}
}