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
        /// <summary>
        /// Private constructor to force everyone to use the static <see cref="GetInstance()"/> method.
        /// </summary>
        /// <remarks>
        /// This constructor should NOT be removed.
        /// </remarks>
        private CoreServiceManager()
        { }

        private string _userName;
        private static Lazy<CoreServiceManager> _instance = new Lazy<CoreServiceManager>(() => new CoreServiceManager());

        internal SessionAwareCoreServiceClient _coreServiceClient = null;

        /// <summary>
        /// Gets or creates an instance of <see cref="CoreServiceManager"/> that is stored in the current http context <seealso cref="HttpContext.Current"/>.
        /// </summary>
        /// <returns>An instance of <see cref="CoreServiceManager"/></returns>
        public static CoreServiceManager GetInstance()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return _instance.Value;
            }
        }

        public string UserName
        {
            get
            {
                using (Tracer.GetTracer().StartTrace())
                {
                    if (_userName == null)
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

                        _userName = userName;
                    }

                    return _userName;
                }
            }
        }

        public SessionAwareCoreServiceClient CoreServiceClient
        {
            get
            {
                using (Tracer.GetTracer().StartTrace())
                {
                    if (_coreServiceClient != null && CoreServiceClientIsFaulted())
                    {
                        DestroyCoreService();
                    }

                    if (_coreServiceClient != null)
                    {
                        return _coreServiceClient;
                    }

                    _coreServiceClient = CreateCoreService(Constants.CLIENT_ENDPOINT_NAME_81);

                    return _coreServiceClient;
                }
            }
        }

        private bool CoreServiceClientIsFaulted()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return ((ICommunicationObject)_coreServiceClient).State == CommunicationState.Faulted;
            }
        }

        public SessionAwareCoreServiceClient CreateCoreService(string endpointName)
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

        private void DestroyCoreService()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                if (_coreServiceClient == null)
                {
                    return;
                }

                var client = _coreServiceClient as ICommunicationObject;
                if (client != null)
                {
                    if (client.State == CommunicationState.Faulted)
                    {
                        client.Abort();
                    }
                    else
                    {
                        client.Close();
                    }
                }
                ((IDisposable)_coreServiceClient).Dispose();
                _coreServiceClient = null;
            }
        }

        private void SetClaimsCredential(ClaimsClientCredentials clientCredentials, Uri audienceUri)
        {
            using (Tracer.GetTracer().StartTrace(clientCredentials))
            {
                // Our own ClaimsClientCredentials (SAML support) has been configured.
                if (IsSamlSupported())
                {
                    // Our own LDAP or SSO authentication has been used. Use the ClaimSet it provided as client credentials.
                    var user = (ClaimsPrincipal)HttpContext.Current.User;
                    clientCredentials.Claims = user.Claims;
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
                return user != null;
            }
        }
    }
}
