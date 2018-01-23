using System;
using System.ServiceModel;
using System.Threading;
using System.Web;

using Tridion.ContentManager.CoreService.Client;
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
        internal CoreServiceManager()
        {}

        private string _userName;
        private static CoreServiceManager _instance;

        internal ISessionAwareCoreService _coreServiceClient = null;
        internal AccessTokenData _userData = null;

        /// <summary>
        /// Gets or creates an instance of <see cref="CoreServiceManager"/> that is stored in the current http context <seealso cref="HttpContext.Current"/>.
        /// </summary>
        /// <returns>An instance of <see cref="CoreServiceManager"/></returns>
        public static CoreServiceManager GetInstance()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                if (_instance == null)
                {
                    _instance = new CoreServiceManager();
                }

                return _instance;
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

        public ISessionAwareCoreService CoreServiceClient
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
                    _coreServiceClient.Impersonate(UserName);

                    return _coreServiceClient;
                }
            }
        }

        public ISessionAwareCoreService CreateCoreService(string endpointName)
        {
            using (Tracer.GetTracer().StartTrace(endpointName))
            {
                return new SessionAwareCoreServiceClient(endpointName);
            }
        }

        private bool CoreServiceClientIsFaulted()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return ((ICommunicationObject) _coreServiceClient).State == CommunicationState.Faulted;
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
                ((IDisposable) _coreServiceClient).Dispose();
                _coreServiceClient = null;
            }
        }
    }
}
