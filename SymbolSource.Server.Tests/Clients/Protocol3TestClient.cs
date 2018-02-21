using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using SymbolSource.Contract;

namespace SymbolSource.Server.Tests.Clients
{
    public class Protocol3TestClient : TestClient
    {
        private readonly SourceRepository repository;

        public Protocol3TestClient(Uri serverUri, string feed)
            : base(serverUri, feed)
        {
            repository = new SourceRepository(
                new PackageSource(uri.ToString()), 
                Repository.Provider.GetCoreV3());
        }

        public override async Task<IList<PackageName>> Query()
        {
            var result = new List<PackageName>();
            var resource = await repository.GetResourceAsync<PackageSearchResource>();

            if (resource != null)
            {
                var page = 100;
                var skip = 0;

                do
                {
                    var packages = await resource.SearchAsync(
                        null,
                        new SearchFilter(true)
                        {
                            SupportedFrameworks = new string[0]
                        },
                        skip,
                        page,
                        null,
                        CancellationToken.None);

                    var packageNames = packages
                        .Select(p => new PackageName(p.Identity.Id, p.Identity.Version.ToString()))
                        .ToList();

                    if (packageNames.Count < page)
                        break;

                    result.AddRange(packageNames);
                    skip += page;
                }
                while (true);
            }

            return result;
        }

        public override void Symbol()
        {
            throw new NotImplementedException();
        }
    }
}
