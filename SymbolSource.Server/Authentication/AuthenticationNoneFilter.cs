using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Autofac.Integration.WebApi;

namespace SymbolSource.Server.Authentication
{
    public class AuthenticationNoneFilter : IAutofacAuthenticationFilter
    {
        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
        }
    }
}