using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Autofac.Integration.WebApi;
using SymbolSource.Contract.Security;

namespace SymbolSource.Server.Authentication
{
    public class AuthenticationNuGetApiKeyFilter : IAutofacAuthenticationFilter
    {
        private readonly ISecurityService security;

        public AuthenticationNuGetApiKeyFilter(
            ISecurityService security)
        {
            this.security = security;
        }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var apiKey = GetApiKey(context);

            if (string.IsNullOrEmpty(apiKey))
            {
                context.ErrorResult = new AuthenticationFailureResult("Must authenticate", context.Request);
                return;
            }

            var data = new NuGetApiKeyAuthenticationData { ApiKey = apiKey };
            var identity = security.Authenticate(data);

            if (identity == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", context.Request);
                return;
            }

            ((FeedPrincipal)context.Principal).Identity = identity;
        }

        private static string GetApiKey(HttpAuthenticationContext context)
        {
            try
            {
                return context.Request.Headers.GetValues("X-NuGet-ApiKey").Single();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
        }
    }
}