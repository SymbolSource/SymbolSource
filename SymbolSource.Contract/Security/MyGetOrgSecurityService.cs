using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using Newtonsoft.Json;
using SymbolSource.Contract.Support;

namespace SymbolSource.Contract.Security
{
    public interface IMyGetOrgSecurityEndpoint
    {
        MyGetOrgResponse VerifyAccess(string host, string secret, string feedName, PackageName packageName, string apiKey, ICredentials credentials);
    }

    public class MyGetOrgSecurityEndpoint : IMyGetOrgSecurityEndpoint
    {
        private readonly ISupportService support;

        public MyGetOrgSecurityEndpoint(ISupportService support)
        {
            this.support = support;
        }

        public MyGetOrgResponse VerifyAccess(string host, string secret, string feedName, PackageName packageName, string apiKey, ICredentials credentials)
        {
            var url = string.Format("{0}/F/{1}/api/v2/symsrc/{2}/{3}", host, feedName, packageName.Id, packageName.Version);
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (apiKey != null)
                request.Headers["X-NuGet-ApiKey"] = apiKey.ToLower();

            if (credentials != null)
                request.Credentials = credentials;

            request.Headers["X-SymbolSource"] = secret;
            request.AllowAutoRedirect = false;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                    return JsonConvert.DeserializeObject<MyGetOrgResponse>(reader.ReadToEnd());
            }
            catch (WebException exception)
            {
                switch (((HttpWebResponse)exception.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Forbidden:
                        break;
                    default:
                        support.TrackException(exception, null);
                        break;
                }

                return new MyGetOrgResponse
                {
                    read = false,
                    write = false
                };
            }
        }
    }


    public class MyGetOrgResponse
    {
        public bool read { get; set; }
        public bool write { get; set; }
    }

    public class MyGetOrgSecurityService : ISecurityService
    {
        private readonly IMyGetOrgSecurityConfiguration configuration;
        private readonly IMyGetOrgSecurityEndpoint endpoint;

        public MyGetOrgSecurityService(IMyGetOrgSecurityConfiguration configuration, IMyGetOrgSecurityEndpoint endpoint)
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
                case AuthenticatedArea.Debugging:
                    return AuthenticationMode.Basic;
                case AuthenticatedArea.Querying:
                case AuthenticatedArea.QueryingOwn:
                    return AuthenticationMode.None;
                default:
                    throw new ArgumentOutOfRangeException("area");
            }
        }

        public SecurityIdentity Authenticate(IAuthenticationData data)
        {
            var nugetApiKeyData = data as NuGetApiKeyAuthenticationData;

            if (nugetApiKeyData != null)
                return NuGetApiKeyIdentity.Create(nugetApiKeyData.ApiKey, configuration.InstanceSalt);

            var basicData = data as BasicAuthenticationData;

            if (basicData != null)
                return NuGetApiKeyIdentity.Create(basicData.Password, configuration.InstanceSalt);

            throw new NotSupportedException();
        }

        public bool Authorize(FeedPrincipal principal, IEnumerable<PackageName> packageNames)
        {
            if (string.IsNullOrEmpty(principal.FeedName))
                return false;

            if (packageNames == null)
                packageNames = new PackageName[0];

            var apiKeyIdentity = principal.Identity as NuGetApiKeyIdentity;
            var basicIdentity = null as BasicAuthenticationData;

            foreach (var packageName in packageNames)
            {
                MyGetOrgResponse result;

                if (apiKeyIdentity != null)
                {
                    result = endpoint.VerifyAccess(configuration.Host, configuration.Secret, principal.FeedName, packageName, apiKeyIdentity.ApiKey, null);
                }
                else if (basicIdentity != null)
                {
                    var credential = new NetworkCredential(basicIdentity.Username, basicIdentity.Password);
                    result = endpoint.VerifyAccess(configuration.Host, configuration.Secret, principal.FeedName, packageName, null, credential);
                }
                else
                {
                    result = endpoint.VerifyAccess(configuration.Host, configuration.Secret, principal.FeedName, packageName, null, null);
                }

                switch (principal.AuthenticatedArea)
                {
                    case AuthenticatedArea.Pushing:
                    case AuthenticatedArea.Deleting:
                        {
                            if (result.write)
                                return true;
                            break;
                        }
                    case AuthenticatedArea.Querying:
                    case AuthenticatedArea.Debugging:
                        {
                            if (result.read)
                                return true;
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException("area", principal.AuthenticatedArea, null);
                }
            }

            return false;
        }

       
    }
}