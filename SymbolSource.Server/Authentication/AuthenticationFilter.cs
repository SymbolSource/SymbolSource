using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Autofac.Features.Indexed;
using Autofac.Integration.WebApi;
using SymbolSource.Contract;
using SymbolSource.Contract.Security;

namespace SymbolSource.Server.Authentication
{
    public class AuthenticatedAreaAttribute : Attribute
    {
        public AuthenticatedArea Area { get; private set; }
        public string Parameter { get; private set; }

        public AuthenticatedAreaAttribute(AuthenticatedArea area, string parameter)
        {
            Area = area;
            Parameter = parameter;
        }
    }

    public class AuthenticationFilter : IAutofacAuthenticationFilter
    {
        private readonly ISecurityService security;
        private readonly IIndex<AuthenticationMode, IAutofacAuthenticationFilter> filters;
        private readonly AuthenticatedAreaAttribute defaultAttribute;

        public AuthenticationFilter(
            ISecurityService security,
            IIndex<AuthenticationMode, IAutofacAuthenticationFilter> filters,
            AuthenticatedAreaAttribute defaultAttribute)
        {
            this.security = security;
            this.filters = filters;
            this.defaultAttribute = defaultAttribute;
        }

        private AuthenticatedAreaAttribute GetAttribute(HttpActionContext context)
        {
            var areaAttribute = context.ActionDescriptor
                .GetCustomAttributes<AuthenticatedAreaAttribute>()
                .SingleOrDefault();

            if (areaAttribute != null)
                return areaAttribute;

            areaAttribute = context.ActionDescriptor.ControllerDescriptor
                .GetCustomAttributes<AuthenticatedAreaAttribute>()
                .SingleOrDefault();

            if (areaAttribute != null)
                return areaAttribute;

            if (defaultAttribute != null)
                return defaultAttribute;

            throw new InvalidOperationException(string.Format(
                "Missing security attribute for {0}.{1}",
                context.ActionDescriptor.ControllerDescriptor.ControllerName,
                context.ActionDescriptor.ActionName));
        }

        private FeedPrincipal CreatePrincipal(HttpActionContext context)
        {
            var attribute = GetAttribute(context);

            var feedPrincipal = new FeedPrincipal
            {
                Identity = new SecurityIdentity(AuthenticationMode.None, null, null),
                AuthenticatedArea = attribute.Area,
                PackageState = PackageState.Succeded
            };

            ParseParameter(context.ControllerContext.RouteData, attribute.Parameter, feedPrincipal);

            return feedPrincipal;
        }

        private static void ParseParameter(IHttpRouteData routeData, string parameter, FeedPrincipal principal)
        {
            if (!routeData.Values.ContainsKey(parameter)) 
                return;
            
            var feedName = (string)routeData.Values[parameter];
            var authenticatedArea = principal.AuthenticatedArea;
            var packageState = principal.PackageState;
            ParseParameter(ref feedName, ref authenticatedArea, ref packageState);
            principal.FeedName = feedName;
            principal.AuthenticatedArea = authenticatedArea;
            principal.PackageState = packageState;
        }

        public static void ParseParameter(ref string feedName, ref AuthenticatedArea authenticatedArea, ref PackageState packageState)
        {
            if (feedName == null)
                return;

            var parts = feedName.Split(',');

            if (parts.Length < 2)
                return;

            feedName = parts[0];

            if (feedName == "")
                feedName = null;

            EnumExtensions.TryParse(authenticatedArea + parts[1], ref authenticatedArea);

            if (parts.Length < 3)
                return;

            if (!EnumExtensions.TryParse(parts[2], ref packageState))
                packageState = PackageState.None;

        }

        private AuthenticationMode GetMode(HttpActionContext context, FeedPrincipal feedPrincipal)
        {
            return security.GetAuthenticationMode(feedPrincipal.AuthenticatedArea);
        }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var principal = context.Principal as FeedPrincipal;

            if (principal == null)
                principal = CreatePrincipal(context.ActionContext);

            var mode = GetMode(context.ActionContext, principal);
            context.Principal = principal;
            await filters[mode].AuthenticateAsync(context, cancellationToken);
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            var principal = CreatePrincipal(context.ActionContext);
            var mode = GetMode(context.ActionContext, principal);
            await filters[mode].ChallengeAsync(context, cancellationToken);
        }
    }

    public class AuthenticationFailureResult : IHttpActionResult
    {
        public string ReasonPhrase { get; private set; }
        public HttpRequestMessage Request { get; private set; }

        public AuthenticationFailureResult(string reasonPhrase, HttpRequestMessage request)
        {
            ReasonPhrase = reasonPhrase;
            Request = request;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.RequestMessage = Request;
            response.ReasonPhrase = ReasonPhrase;
            return Task.FromResult(response);
        }
    }

    public class AuthenticationChallengeResult : IHttpActionResult
    {
        public IHttpActionResult InnerResult { get; private set; }
        public string Scheme { get; private set; }
        public string Parameter { get; private set; }

        public AuthenticationChallengeResult(IHttpActionResult innerResult, string scheme, string parameter)
        {
            InnerResult = innerResult;
            Scheme = scheme;
            Parameter = parameter;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = await InnerResult.ExecuteAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Headers.WwwAuthenticate.Clear();
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(Scheme, Parameter));
            }

            return response;
        }
    }
}