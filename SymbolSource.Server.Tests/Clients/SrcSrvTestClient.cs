using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting;
using System.Threading.Tasks;

namespace SymbolSource.Server.Tests.Clients
{
    public class SrcSrvTestClient : TestClient
    {
        private readonly WebClient client;

        public SrcSrvTestClient(Uri serverUri, string feed)
            : base(serverUri, feed)
        {
            client = new WebClient();
        }

        public override void Dispose()
        {
            client.Dispose();
        }
        
        public override void Symbol()
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Path += "/aaa.pdb/bbb/aaa.pd_";
            client.DownloadData(uriBuilder.Uri);
        }
    }
}
