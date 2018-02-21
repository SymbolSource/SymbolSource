using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using Autofac.Integration.WebApi;
using SymbolSource.Contract.Security;

namespace SymbolSource.Server.Authentication
{
    public class AuthenticationBasicFilter : IAutofacAuthenticationFilter
    {
        private readonly ISecurityService security;

        public AuthenticationBasicFilter(ISecurityService security)
        {
            this.security = security;
        }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (context.Request.Headers.Authorization == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Must authenticate", context.Request);
                return;
            }

            var data = Decode(context.Request.Headers.Authorization.Parameter);

            if (data == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Malformed credentials", context.Request);
                return;
            }

            var identity = security.Authenticate(data);
           
            if (identity == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", context.Request);
                return;
            }

            ((FeedPrincipal)context.Principal).Identity = identity;
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new AuthenticationBasicChallengeResult(context.Result, "test");
        }

        public static BasicAuthenticationData Decode(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return null;
            }

            // The currently approved HTTP 1.1 specification says characters here are ISO-8859-1.
            // However, the current draft updated specification for HTTP 1.1 indicates this encoding is infrequently
            // used in practice and defines behavior only for ASCII.
            var encoding = Encoding.ASCII;
            // Make a writable copy of the encoding to enable setting a decoder fallback.
            encoding = (Encoding)encoding.Clone();
            // Fail on invalid bytes rather than silently replacing and continuing.
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            
            string decodedCredentials;

            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(decodedCredentials))
                return null;

            var colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
                return null;

            return new BasicAuthenticationData
            {
                Username = decodedCredentials.Substring(0, colonIndex),
                Password = decodedCredentials.Substring(colonIndex + 1)
            };
        }
    }

    public class AuthenticationBasicChallengeResult : AuthenticationChallengeResult
    {
        public AuthenticationBasicChallengeResult(IHttpActionResult innerResult, string realm)
            : base(innerResult, "Basic", "realm=\"" + realm + "\"")
        {
        }
    }
}