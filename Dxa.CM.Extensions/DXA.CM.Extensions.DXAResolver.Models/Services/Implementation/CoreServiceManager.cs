using System;
using System.Security.Claims;
using System.ServiceModel;
using System.Threading;
using System.Web;

using Tridion.ContentManager.CoreService.Client;
using Tridion.ContentManager.CoreService.Client.Security;
using Tridion.Logging;

namespace DXA.CM.Extensions.DXAResolver.Models
{
    internal class CoreServiceManager
    {
        public static string UserName
        {
            get
            {
                using (Tracer.GetTracer().StartTrace())
                {
                    string userName = String.Empty;

                    // Use the HttpContext if available (first choice)
                    if (HttpContext.Current != null && HttpContext.Current.User != null &&
                        HttpContext.Current.User.Identity != null)
                    {
                        userName = HttpContext.Current.User.Identity.Name;
                    }
                    // Use the WCF security context if available (second choice)
                    else if (ServiceSecurityContext.Current != null)
                    {
                        userName = ServiceSecurityContext.Current.WindowsIdentity.Name;
                    }

                    // If neither HTTPContext or WCFSecurityContext could be used, revert back to the Thread's principal
                    if (String.IsNullOrEmpty(userName))
                    {
                        userName = Thread.CurrentPrincipal.Identity.Name;
                    }

                    return userName;
                }
            }
        }

        public static SessionAwareCoreServiceClient GetCoreServiceClient()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return CreateCoreService(Constants.CLIENT_ENDPOINT_NAME_81);
            }
        }

        private static SessionAwareCoreServiceClient CreateCoreService(string endpointName)
        {
            using (Tracer.GetTracer().StartTrace(endpointName))
            {
                SessionAwareCoreServiceClient client = new SessionAwareCoreServiceClient(endpointName);
                var clientCredentials = client.ClientCredentials as ClaimsClientCredentials;

                if (clientCredentials != null)
                {
                    SetClaimsCredential(clientCredentials, client.Endpoint.Address.Uri);
                }
                else
                {
                    // Default ClientCredentials. Use the current Windows Identity as client credentials and specify the user name in a Core Service Impersonate call.
                    client.Impersonate(UserName);
                }

                return client;
            }
        }

        private static void SetClaimsCredential(ClaimsClientCredentials clientCredentials, Uri audienceUri)
        {
            using (Tracer.GetTracer().StartTrace(clientCredentials))
            {
                // Our own ClaimsClientCredentials (SAML support) has been configured.
                if (IsSamlSupported())
                {
                    // Our own LDAP or SSO authentication has been used. Use the ClaimSet it provided as client credentials.
                    var user = HttpContext.Current.User as ClaimsPrincipal;
                    if (user == null)
                    {
                        var tridionUser = HttpContext.Current.User as Tridion.Security.ClaimsPrincipal;
                        clientCredentials.Claims = tridionUser.Claims;
                    }
                    else
                    {
                        clientCredentials.Claims = user.Claims;
                    }
                }
                else
                {
                    // Other authentication scheme has been used. Provide the user name as client credentials.
                    clientCredentials.UserName.UserName = UserName;
                }

                clientCredentials.AudienceUris = new[] { audienceUri };
            }
        }

        private static bool IsSamlSupported()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                var user = HttpContext.Current.User as ClaimsPrincipal;
                var trdidionUser = HttpContext.Current.User as Tridion.Security.ClaimsPrincipal;
               
                return user != null || trdidionUser!= null;
            }
        }
    }
}
