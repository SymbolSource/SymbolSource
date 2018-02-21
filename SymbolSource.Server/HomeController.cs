using SymbolSource.Contract;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace SymbolSource.Server
{
    public class HomeController : ApiController
    {
        //[Route("")]
        //[HttpGet]
        //public string Index()
        //{
        //    return "OK";
        //}

        [Route("favicon.ico")]
        [HttpGet]
        public Stream Icon()
        {
            return null;
        }
    }
}