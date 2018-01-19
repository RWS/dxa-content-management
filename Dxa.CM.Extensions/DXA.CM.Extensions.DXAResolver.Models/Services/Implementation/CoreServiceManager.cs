using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.ServiceModel;
using System.Threading;
using System.Web;
using System.Web.Caching;

using System.Xml.Linq;
using Tridion.ContentManager.CoreService.Client;
using Tridion.ContentManager.CoreService.Client.Security;
using Tridion.Logging;
using Tridion.Web.CMUtils;

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

        internal ICoreServiceWrapper _coreServiceClient = null;
        internal AccessTokenData _userData = null;
        internal ICoreServiceWrapperFactory CoreServiceFactory;

        public const string CacheKey = "DXA.CM.Extensions.DXAResolver-CoreClientManager";

        /// <summary>
        /// Gets or creates an instance of <see cref="CoreClientManager"/> that is stored in the current http context <seealso cref="HttpContext.Current"/>.
        /// </summary>
        /// <returns>An instance of <see cref="CoreClientManager"/></returns>
        /// <remarks>
        /// The lifecycle of the instance is managed through <see cref="Tridion.Web.UI.Models.TCM54.TcmAuthorizationModule"/> http module.
        /// It creates the instance on authorization (<seealso cref="HttpApplication.AuthorizeRequest"/>) 
        /// and dispose it after ASP.NET responds to a request (<seealso cref="HttpApplication.EndRequest"/>).
        /// </remarks>
        public static CoreServiceManager GetInstance()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return GetInstance(HttpContext.Current);
            }
        }

        public static CoreServiceManager GetInstance(HttpContext httpContext)
        {
            using (Tracer.GetTracer().StartTrace(httpContext))
            {
                if (httpContext == null)
                {
                    throw new ArgumentNullException("httpContext");
                }

                if (!(httpContext.Items[CacheKey] is CoreServiceManager instance))
                {
                    instance = new CoreServiceManager();
                    httpContext.Items[CacheKey] = instance;
                }

                return instance;
            }
        }

        public void Dispose()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                try
                {
                    if (_coreServiceClient == null)
                    {
                        return;
                    }

                    if (_coreServiceClient.State == CommunicationState.Faulted)
                    {
                        _coreServiceClient.Abort();
                    }
                    else
                    {
                        _coreServiceClient.Close();
                    }

                    _coreServiceClient = null;
                }
                finally
                {
                }
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
                        else if (ServiceSecurityContext.Current != null && ServiceSecurityContext.Current.WindowsIdentity != null)
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

        public AccessTokenData UserData
        {
            get
            {
                using (Tracer.GetTracer().StartTrace())
                {
                    if (_userData == null)
                    {
                        _userData = CoreServiceClient.GetCurrentUser();
                    }

                    return _userData;
                }
            }
        }

        public string UserId
        {
            get
            {
                using (Tracer.GetTracer().StartTrace())
                {
                    return UserData.Id;
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

                    var accessTokenData = GetNonExpiredAccessTokenFromCache(UserName);

                    if (accessTokenData != null)
                    {
                        Logger.Write(
                            String.Format(
                                "Using cached access token for user '{0}' to do an impersonation with token.",
                                UserName
                            ),
                            this.GetType().Name,
                            LoggingCategory.Logging
                        );

                        try
                        {
                            _coreServiceClient.ImpersonateWithToken(accessTokenData);
                        }
                        catch (Exception exception)
                        {
                            Logger.Write(exception, this.GetType().Name, LoggingCategory.Logging);

                            // Cleanup cached access token as we need to get it again due to some failure.
                            accessTokenData = null;

                            RecreateCoreService();
                        }
                    }

                    if (accessTokenData == null)
                    {
                        accessTokenData = GetAccessTokenData();
                    }

                    StoreAccessTokenInCache(UserName, accessTokenData);
                    StoreAccessTokenLocally(accessTokenData);

                    return _coreServiceClient;
                }
            }
        }

        private bool CoreServiceClientIsFaulted()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                return _coreServiceClient.State == CommunicationState.Faulted;
            }
        }
        public ICoreServiceWrapper CreateCoreService(string endpointName)
        {
            using (Tracer.GetTracer().StartTrace(endpointName))
            {
                _coreServiceClient = new SessionAwareCoreServiceClient(endpointName) as ICoreServiceWrapper;
            }

            return _coreServiceClient;
        }

        private void DestroyCoreService()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                _coreServiceClient.Abort();
                _coreServiceClient.Dispose();
                _coreServiceClient = null;
            }
        }

        private void RecreateCoreService()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                DestroyCoreService();
                _coreServiceClient = CreateCoreService(Constants.CLIENT_ENDPOINT_NAME_81);
            }
        }

        private AccessTokenData GetNonExpiredAccessTokenFromCache(string user)
        {
            using (Tracer.GetTracer().StartTrace(user))
            {
                Logger.Write(String.Format("Trying to retrieve access token for user '{0}'", user), this.GetType().Name,
                    LoggingCategory.Logging);

                var accessTokenData = GetAccessTokenFromCache(user);

                if (accessTokenData == null || HasAccessTokenExpired(accessTokenData))
                {
                    return null;
                }

                Logger.Write(
                    String.Format("Access token for user '{0}' was succesfully retrieved from cache.", UserName),
                    this.GetType().Name,
                    LoggingCategory.Logging);

                return accessTokenData;
            }
        }

        private static AccessTokenData GetAccessTokenFromCache(string user)
        {
            using (Tracer.GetTracer().StartTrace(user))
            {
                var cacheKey = GetAccessTokenCacheKey(user);
                var accessTokenData = HttpContext.Current.Cache.Get(cacheKey) as AccessTokenData;

                if (accessTokenData == null)
                {
                    Logger.Write(String.Format("Access token for user '{0}' couldn't be found in cache.", user),
                        "GetAccessTokenFromCache", LoggingCategory.Logging);
                }

                return accessTokenData;
            }
        }

        private static string GetAccessTokenCacheKey(string user)
        {
            using (Tracer.GetTracer().StartTrace(user))
            {
                return "SdlTridion\\WebUI\\AccessTokens\\" + user;
            }
        }

        private static bool HasAccessTokenExpired(AccessTokenData accessToken)
        {
            using (Tracer.GetTracer().StartTrace(accessToken))
            {
                return accessToken.ExpiresAt < DateTime.UtcNow;
            }
        }

        private AccessTokenData GetAccessTokenData()
        {
            using (Tracer.GetTracer().StartTrace())
            {
                Logger.Write(
                    String.Format("_coreServiceClient.ClientCredentials type is {0}",
                        _coreServiceClient.ClientCredentials == null
                            ? "None"
                            : _coreServiceClient.ClientCredentials.GetType().ToString()),
                    this.GetType().Name, LoggingCategory.Logging);
                var clientCredentials = _coreServiceClient.ClientCredentials as ClaimsClientCredentials;

                return clientCredentials != null
                    ? GetAccessTokenDataFromClientCrentials(clientCredentials)
                    // Default ClientCredentials. Use the current Windows Identity as client credentials and specify the user name in a Core Service Impersonate call.
                    : _coreServiceClient.Impersonate(UserName);
            }
        }

        private static void StoreAccessTokenInCache(string user, AccessTokenData accessToken)
        {
            using (Tracer.GetTracer().StartTrace(user, accessToken))
            {
                Logger.Write(String.Format("Storing access token for user '{0}' in cache.", user),
                    "StoreAccessTokenInCache",
                    LoggingCategory.Logging);

                var cacheKey = GetAccessTokenCacheKey(user);

                HttpContext.Current.Cache.Insert(cacheKey, accessToken, null, accessToken.ExpiresAt,
                    Cache.NoSlidingExpiration);
            }
        }

        private void StoreAccessTokenLocally(AccessTokenData accessTokenData)
        {
            using (Tracer.GetTracer().StartTrace(accessTokenData))
            {
                //TODO AN: Get rid of this side effect, take _userData from cache instead
                _userData = accessTokenData;
            }
        }

        private AccessTokenData GetAccessTokenDataFromClientCrentials(ClaimsClientCredentials clientCredentials)
        {
            using (Tracer.GetTracer().StartTrace(clientCredentials))
            {
                // Our own ClaimsClientCredentials (SAML support) has been configured.
                if (IsSamlSupported())
                {
                    // Our own LDAP or SSO authentication has been used. Use the ClaimSet it provided as client credentials.
                    var user = (ClaimsPrincipal) HttpContext.Current.User;
                    clientCredentials.Claims = user.Claims;
                }
                else
                {
                    // Other authentication scheme has been used. Provide the user name as client credentials.
                    clientCredentials.UserName.UserName = UserName;
                }

                clientCredentials.AudienceUris = new[] {_coreServiceClient.Endpoint.Address.Uri};

                return _coreServiceClient.GetCurrentUser();
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
