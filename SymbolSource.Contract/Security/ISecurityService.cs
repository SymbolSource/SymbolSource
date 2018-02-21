using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;

namespace SymbolSource.Contract.Security
{
    public enum AuthenticatedArea
    {
        None,
        Pushing,
        Deleting,
        //DeletingOwn,
        //DeletingAll,
        Querying,
        QueryingOwn,
        QueryingAll,
        Debugging,
        Retrying,
        RetryingOwn,
        RetryingAll,
    }

    public enum AuthenticationMode
    {
        None,
        NuGetApiKey,
        Basic,
    }

    public interface IAuthenticationData
    {
    }

    public class NuGetApiKeyAuthenticationData : IAuthenticationData
    {
        public string ApiKey { get; set; }
    }

    public class BasicAuthenticationData : IAuthenticationData
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public interface ISecurityService
    {
        AuthenticationMode GetAuthenticationMode(AuthenticatedArea area);
        SecurityIdentity Authenticate(IAuthenticationData data);
        bool Authorize(FeedPrincipal principal, IEnumerable<PackageName> packageNames);
    }

    public class FeedPrincipal : IPrincipal
    {
        public bool IsInRole(string role)
        {
            throw new NotSupportedException();
        }

        IIdentity IPrincipal.Identity
        {
            get { return Identity; }
        }

        public SecurityIdentity Identity { get; set; }
        public string FeedName { get; set; }
        public AuthenticatedArea AuthenticatedArea { get; set; }
        public PackageState PackageState { get; set; }

        public override string ToString()
        {
            var feedName = string.IsNullOrEmpty(FeedName) ? "<default>" : FeedName;
            return string.Format("{0} {1}/{2} as {3}", AuthenticatedArea, feedName, PackageState, Identity);
        }
    }

    public class SecurityIdentity : IIdentity
    {
        public static void Parse(ref string name, ref string handle)
        {
            var parts = name.Split('/');
            name = parts[0];

            if (parts.Length < 2)
                return;

            handle = parts[1];
        }

        private readonly AuthenticationMode mode;
        private readonly string name;
        private readonly string handle;

        public SecurityIdentity(AuthenticationMode mode, string name, string handle)
        {
            this.mode = mode;
            this.name = name;
            this.handle = handle;
        }

        public string Name
        {
            get { return name; }
        }

        public string Handle
        {
            get { return handle; }
        }

        public string AuthenticationType
        {
            get { return mode.ToString(); }
        }

        public bool IsAuthenticated
        {
            get { return !string.IsNullOrEmpty(name); }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(name))
                return "<anonymous>";

            if (!string.IsNullOrEmpty(handle))
                return string.Format("{0} ({1})", name, handle);

            return name;
        }
    }

    internal class NuGetApiKeyIdentity : SecurityIdentity
    {
        private readonly string apiKey;

        public NuGetApiKeyIdentity(string name, string handle, string apiKey)
            : base(AuthenticationMode.NuGetApiKey, name, handle)
        {
            this.apiKey = apiKey;
        }

        public string ApiKey
        {
            get { return apiKey; }
        }

        public static NuGetApiKeyIdentity Create(string apiKey, string salt)
        {
            string handle = null;
            Parse(ref apiKey, ref handle);
            apiKey = apiKey.ToUpper();
            var name = ComputeHash(apiKey, salt);
            return new NuGetApiKeyIdentity(name, handle, apiKey);
        }

        private static string ComputeHash(string apiKey, string salt)
        {
            var keyData = Encoding.UTF8.GetBytes(salt);

            using (var algorithm = new HMACSHA256(keyData))
            {
                var apiKeyData = Encoding.UTF8.GetBytes(apiKey);
                var apiKeyHash = algorithm.ComputeHash(apiKeyData);
                return apiKeyHash.ToInternetBase64();
            }
        }
    }
}