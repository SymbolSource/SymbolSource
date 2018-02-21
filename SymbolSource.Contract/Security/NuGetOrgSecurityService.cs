using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;

namespace SymbolSource.Contract.Security
{
    public interface INuGetOrgSecurityEndpoint
    {
        HttpStatusCode VerifyKey(string apiKey, string packageId);
    }

    public class NuGetOrgSecurityEndpoint : INuGetOrgSecurityEndpoint
    {
        public HttpStatusCode VerifyKey(string apiKey, string packageId)
        {
            var uri = string.Format("{0}/api/v2/verifykey/{1}", "https://www.nuget.org", packageId);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers["X-NuGet-ApiKey"] = apiKey.ToLower();
            request.AllowAutoRedirect = false;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode;
                }
            }
            catch (WebException exception)
            {
                return ((HttpWebResponse)exception.Response).StatusCode;
            }
        }
    }

    public class NuGetOrgSecurityService : ISecurityService
    {
        private readonly IInstanceConfiguration configuration;
        private readonly INuGetOrgSecurityEndpoint endpoint;

        public NuGetOrgSecurityService(IInstanceConfiguration configuration, INuGetOrgSecurityEndpoint endpoint)
        {
            this.configuration = configuration;
            this.endpoint = endpoint;
        }

        public AuthenticationMode GetAuthenticationMode(AuthenticatedArea area)
        {
            switch (area)
            {
                case AuthenticatedArea.Pushing:
                case AuthenticatedArea.Retrying:
                case AuthenticatedArea.RetryingOwn:
                case AuthenticatedArea.RetryingAll:
                case AuthenticatedArea.Deleting:
                    return AuthenticationMode.NuGetApiKey;
                case AuthenticatedArea.Querying:
                case AuthenticatedArea.QueryingAll:
                    return AuthenticationMode.None;
                case AuthenticatedArea.QueryingOwn:
                    return AuthenticationMode.Basic;
                case AuthenticatedArea.Debugging:
                    return AuthenticationMode.None;
                default:
                    throw new ArgumentOutOfRangeException("area", area, null);
            }
        }

        public SecurityIdentity Authenticate(IAuthenticationData data)
        {
            var nugetApiKeyData = data as NuGetApiKeyAuthenticationData;

            if (nugetApiKeyData != null)
                return NuGetApiKeyIdentity.Create(nugetApiKeyData.ApiKey, configuration.InstanceSalt);

            var basicData = data as BasicAuthenticationData;

            if (basicData != null)
            {
                var apiKey = string.Format("{0}/{1}", basicData.Password, basicData.Username);
                return NuGetApiKeyIdentity.Create(apiKey, configuration.InstanceSalt);
            }

            throw new NotSupportedException();
        }

        public bool Authorize(FeedPrincipal principal, IEnumerable<PackageName> packageNames)
        {
            if (!string.IsNullOrEmpty(principal.FeedName))
                return false;

            if (packageNames == null)
                packageNames = new PackageName[0];

            switch (principal.AuthenticatedArea)
            {
                case AuthenticatedArea.Pushing:
                case AuthenticatedArea.Deleting:
                case AuthenticatedArea.Retrying:
                case AuthenticatedArea.RetryingAll:
                case AuthenticatedArea.RetryingOwn:
                    {
                        var apiKeyIdentity = (NuGetApiKeyIdentity)principal.Identity;

                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach (var packageName in packageNames)
                            if (VerifyKey(apiKeyIdentity.ApiKey, packageName.Id))
                                return true;

                        return false;
                    }
                case AuthenticatedArea.Querying:
                case AuthenticatedArea.QueryingAll:
                case AuthenticatedArea.QueryingOwn:
                case AuthenticatedArea.Debugging:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("area", principal.AuthenticatedArea, null);
            }
        }

        public bool VerifyKey(string apiKey, string packageId)
        {
            var response = endpoint.VerifyKey(apiKey, packageId);

            switch (response)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NotFound:
                    return true;

                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Found:
                    return false;

                default:
                    throw new Exception(response.ToString());
            }
        }
    }
}