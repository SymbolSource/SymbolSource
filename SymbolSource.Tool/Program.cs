using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TweetSharp;

namespace SymbolSource.Tool
{
    class Program
    {
        private static void Main(string[] args)
        {
            var twitter = new TwitterService(args[0], args[1]);
            var request = twitter.GetRequestToken();
            var uri = twitter.GetAuthorizationUri(request);
            Console.WriteLine("Authorize Twitter at {0}", uri);
            Console.Write("PIN: ");
            var pin = Console.ReadLine();
            var access = twitter.GetAccessToken(request, pin);
            Console.WriteLine("Token: {0}", access.Token);
            Console.WriteLine("TokenSecret: {0}", access.TokenSecret);
            Console.ReadLine();
        }
    }
}
