using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NuGet;
using PackageName = SymbolSource.Contract.PackageName;

namespace SymbolSource.Server.Tests.Clients
{
    public class Protocol2TestClient : TestClient
    {
        private static ICredentialCache cache = (ICredentialCache)
                typeof(ICredentialCache).Assembly
                .GetType("NuGet.CredentialStore")
                .GetProperty("Instance")
                .GetValue(null);

        private readonly PackageServer packageServer;
        private readonly DataServicePackageRepository packageRepository;

        public Protocol2TestClient(Uri serverUri, string feed)
            : base(serverUri, feed)
        {
            packageServer = new PackageServer(uri.ToString(), "");
            packageRepository = new DataServicePackageRepository(uri);
        }

        public override void Dispose()
        {
        }

        public ICredentials Credentials
        {
            get { return cache.GetCredentials(uri); }
            set { cache.Add(uri, value); }
        }

        public override void Generate(Stream stream, PackageName name)
        {
            var builder = new PackageBuilder();
            builder.Id = name.Id;
            builder.Description = name.Id;
            builder.Version = new SemanticVersion(name.Version);
            builder.Authors.Add("test");
            builder.Files.Add(CreateFile());
            builder.Save(stream);
        }

        public override void Push(Stream stream, string apiKey)
        {
            var size = stream.Length;
            var package = new ZipPackage(stream);
            packageServer.PushPackage(apiKey, package, size, 1000, false);
        }

        private static PhysicalPackageAssemblyReference CreateFile()
        {
            var buffer = Encoding.UTF8.GetBytes("test");
            var file = new PhysicalPackageAssemblyReference(() => new MemoryStream(buffer));
            file.SourcePath = "test";
            file.TargetPath = "test";
            return file;
        }

        public override void Delete(string apiKey, PackageName name)
        {
            packageServer.DeletePackage(apiKey, name.Id, name.Version);
        }

        public override Task<IList<PackageName>> Query()
        {
            //TODO: packageRepository.FindPackages("", null, true, true)?
            var packageNames = packageRepository.GetPackages()
                .ToList()
                .Select(p => new PackageName(p.Id, p.Version.ToString()))
                .ToList();

            return Task.FromResult<IList<PackageName>>(packageNames);
        }
    }
}
