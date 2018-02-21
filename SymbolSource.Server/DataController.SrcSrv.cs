using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using SymbolSource.Contract;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Server.Authentication;

namespace SymbolSource.Server
{
    public partial class DataController
    {
        [AuthenticatedArea(AuthenticatedArea.Debugging, "feedName")]
        [Route("index2.txt")]
        [Route("{feedName}/index2.txt")]
        [HttpGet]
        public IHttpActionResult Index()
        {
            return NotFound();
        }

        private async Task<IHttpActionResult> Storage(IStorageItem item)
        {
            if (item.CanGetUri)
            {
                var uri = await item.GetUri();

                if (uri != null)
                    return Redirect(uri);
            }
            else
            {
                var stream = await item.Get();

                if (stream != null)
                    return Ok(new StreamContent(stream));
            }

            return NotFound();
        }

        [AuthenticatedArea(AuthenticatedArea.Debugging, "feedName")]
        [Route("{imageName}.pdb/{symbolHash}/{imageName2}.{imageExtension}")]
        [Route("{feedName}/{imageName}.pdb/{symbolHash}/{imageName2}.{imageExtension}")]
        [HttpGet]
        //[CacheControl]
        public async Task<IHttpActionResult> Symbol(string imageName, string symbolHash, string imageName2, string imageExtension)
        {
            var principal = (FeedPrincipal)User;

            if (imageName2 == "file" && imageExtension == "ptr")
                return NotFound();

            if (imageName != imageName2)
                return BadRequest();

            if (imageExtension != "pd_")
                return Content(HttpStatusCode.NotFound, "Server supports only compressed files (pd_).");

            if (imageName.Length >= 2 && imageName.Substring(0, 2) == principal.FeedName)
                principal.FeedName = null;

            var symbolName = new SymbolName(imageName, symbolHash);
            var symbol = storage.GetFeed(principal.FeedName).GetSymbol(null, symbolName);

            if (User.Identity.IsAuthenticated)
            {
                if (!security.Authorize(principal, await symbol.PackageNames.List()))
                    return Unauthorized();
            }

            return await Storage(symbol);
        }

        [AuthenticatedArea(AuthenticatedArea.Debugging, "feedName")]
        [Route("src/{sourceFileName}/{sourceHash}/{sourceFileName2}")]
        [Route("{feedName}/src/{sourceFileName}/{sourceHash}/{sourceFileName2}")]
        //TODO: remove after next reprocessing of packages
        [Route("sources/{sourceFileName}/{sourceHash}/{sourceFileName2}")]
        [Route("{feedName}/sources/{sourceFileName}/{sourceHash}/{sourceFileName2}")]
        [HttpGet]
        public async Task<IHttpActionResult> Source(string sourceFileName, string sourceHash, string sourceFileName2)
        {
            var principal = (FeedPrincipal)User;

            if (sourceFileName != sourceFileName2)
                return BadRequest();

            var sourceName = new SourceName(sourceFileName, sourceHash);
            var source = storage.GetFeed(principal.FeedName).GetSource(null, sourceName);

            if (User.Identity.IsAuthenticated)
            {
                if (!security.Authorize(principal, await source.PackageNames.List()))
                    return Unauthorized();
            }

            return await Storage(source);
        }
    }
}