using System.ServiceModel;
using System.ServiceModel.Web;

namespace DXA.CM.Extensions.DXAResolver.Models.Interfaces
{
	[ServiceContract(Namespace = "http://DXA.CM.Extensions.DXAResolver.Models", Name = "Services")]
	public interface IServices
	{
		[OperationContract]
		[WebInvoke(Method = "POST",
					RequestFormat = WebMessageFormat.Json,
					ResponseFormat = WebMessageFormat.Json)]
		string LoadConfiguration();

		[OperationContract]
		[WebInvoke(Method = "POST",
					RequestFormat = WebMessageFormat.Json,
					ResponseFormat = WebMessageFormat.Json)]
		string SaveConfiguration(string configurationXml);
	}
}