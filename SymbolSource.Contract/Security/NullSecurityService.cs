using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace SymbolSource.Contract.Security
{
    public class NullSecurityService : ISecurityService
    {
        private readonly INullSecurityConfiguration configuration;

        public NullSecurityService(INullSecurityConfiguration configuration)
        {
            this.configuration = configuration;
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
                    return AuthenticationMode.None;
                case AuthenticatedArea.QueryingOwn:
                    return AuthenticationMode.Basic;
                case AuthenticatedArea.QueryingAll:
                    return AuthenticationMode.None;
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
                return NuGetApiKeyIdentity.Create(basicData.Password, configuration.InstanceSalt);

            throw new NotSupportedException();
        }

        public bool Authorize(FeedPrincipal principal, IEnumerable<PackageName> packageName)
        {
            if (!configuration.AllowNamedFeeds && !string.IsNullOrEmpty(principal.FeedName))
                return false;

            if (!principal.Identity.IsAuthenticated)
                return true;

            var apiKeyIdentity = (NuGetApiKeyIdentity)principal.Identity;
            return configuration.PushApiKeys.Contains(apiKeyIdentity.ApiKey, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}