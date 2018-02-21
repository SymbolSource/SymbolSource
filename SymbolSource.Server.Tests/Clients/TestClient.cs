using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SymbolSource.Contract;

namespace SymbolSource.Server.Tests.Clients
{
    public abstract class TestClient : IDisposable
    {
        protected readonly Uri uri;

        protected TestClient(Uri serverUri, string feed)
        {
            var uriBuilder = new UriBuilder(serverUri);

            if (Debugger.IsAttached)
                uriBuilder.Host += ".fiddler";

            if (!string.IsNullOrEmpty(feed))
                uriBuilder.Path = feed;

            uri = uriBuilder.Uri;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Generate(Stream stream, PackageName name)
        {
            throw new NotImplementedException();
        }

        public virtual void Push(Stream stream, string apiKey)
        {
            throw new NotImplementedException();
        }

        public virtual void Delete(string apiKey, PackageName name)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IList<PackageName>> Query()
        {
            throw new NotImplementedException();
        }

        public virtual void Symbol()
        {
            throw new NotImplementedException();
        }
    }
}
