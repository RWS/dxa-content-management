using System.ServiceModel.Activation;
using Tridion.Logging;
using DXA.CM.Extensions.DXAResolver.Models.Interfaces;

namespace DXA.CM.Extensions.DXAResolver.Models
{
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public partial class Services : IServices
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