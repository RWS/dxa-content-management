using System;
using System.Linq;
using System.Net;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.TopologyManager.Client;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Wrapper class for access to Topology Manager (to prevent assembly binding errors when run on SDL Tridion 2013 SP1, which doesn't have Tridion.TopologyManager.Client).
    /// </summary>
    internal static class TopologyManager
    {
        private static readonly TemplatingLogger _logger = TemplatingLogger.GetLogger(typeof(TopologyManager));
        private static TopologyManagerClient _topologyManagerClient;
        private static CmEnvironmentData _cmEnvironment;

        internal static string GetCmWebsiteUrl()
        {
            if (_cmEnvironment == null)
            {
                _cmEnvironment = TopologyManagerClient.CmEnvironments.Where(env => env.Id == TopologyManagerClient.ContentManagerEnvironmentId).FirstOrDefault();
                if (_cmEnvironment == null)
                {
                    throw new Exception("Unable to obtain CM Environment Data from Topology Manager. CM Environment ID: " + TopologyManagerClient.ContentManagerEnvironmentId);
                }
            }

            return _cmEnvironment.WebsiteRootUrl;
        }

        internal static string GetSearchQueryUrl(Publication publication, string environmentPurpose)
        {
            string publicationId = publication.Id.ToString();
            MappingData mapping = TopologyManagerClient.Mappings.Expand("CdEnvironment")
                .Where(m => m.PublicationId == publicationId && m.EnvironmentPurpose == environmentPurpose).FirstOrDefault();
            if (mapping == null || mapping.CdEnvironment == null)
            {
                return null;
            }

            string dxaSearchQueryUrl =  mapping.CdEnvironment.ExtensionProperties
                .Where(ep => ep.Name == "DXA.Search.QueryURL")
                .Select(ep => ep.Value)
                .FirstOrDefault();

            return dxaSearchQueryUrl;
        }

        private static TopologyManagerClient TopologyManagerClient
        {
            get
            {
                if (_topologyManagerClient == null)
                {
                    _topologyManagerClient = new TopologyManagerClient
                    {
                        Credentials = CredentialCache.DefaultNetworkCredentials
                    };
                }
                return _topologyManagerClient;
            }
        }
    }
}
